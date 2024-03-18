// See https://aka.ms/new-console-template for more information
using System.Collections.Concurrent;
using CommandLine;
using ConsoleM3U8;
using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

// 创建依赖注入容器
var serviceProvider = new ServiceCollection()
			.AddHttpFactoryHandler()
			.BuildServiceProvider();

// 从容器中获取服务实例
var httpFactoryHandler = serviceProvider.GetRequiredService<HttpFactoryHandler>();

await CommandLine.Parser.Default.ParseArguments<Options>(args)
	.WithParsedAsync(async o =>
	{
		var cli = new RedisClient(o.RedisConnectionString!)
		{
			Serialize = obj => JsonConvert.SerializeObject(obj),
			Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type)
		};

		var remoteWait2ConvertPath = o.RemoteWaitConvertFolderPath!;
		var remoteResultPath = o.RemoteResultFolderPath!;

		if (!Directory.Exists(remoteResultPath))
		{
			Console.WriteLine("Create remote result directory");
			Directory.CreateDirectory(remoteResultPath);
		}

		Console.WriteLine("Get remote file need to convert");
		var remoteWaitConvertDirInfo = new DirectoryInfo(remoteWait2ConvertPath);

		var remoteWaitConvertFileInfos = remoteWaitConvertDirInfo.GetFiles();
		if (remoteWaitConvertFileInfos == null || remoteWaitConvertFileInfos.Length <= 0)
		{
			Console.WriteLine("No file to convert");
			Environment.Exit(1);
		}

		// Check the list key in redis
		var listKey = "Wait";
		if (!await cli.ExistsAsync(listKey))
		{
			var remoteResultDirInfo = new DirectoryInfo(remoteResultPath);
			var remoteResultFileInfos = remoteResultDirInfo.GetFiles();
			var remoteResultFileNames = remoteResultFileInfos
				.Select(x => Path.GetFileNameWithoutExtension(x.Name))
				.ToArray();
			var remoteWaitConvertFilePathQuery = remoteWaitConvertFileInfos
			.Where(x => !remoteResultFileNames.Contains(Path.GetFileNameWithoutExtension(x.Name)));
			remoteWaitConvertFilePathQuery = o.FileCompareType switch
			{
				2 => remoteWaitConvertFilePathQuery.OrderByDescending(x => x.Name, new NaturalFileNameSortComparer()),
				3 => remoteWaitConvertFilePathQuery.OrderBy(x => x.Length),
				4 => remoteWaitConvertFilePathQuery.OrderByDescending(x => x.Length),
				_ => remoteWaitConvertFilePathQuery.OrderBy(x => x.Name, new NaturalFileNameSortComparer()),
			};
			var remoteWaitConvertFileNameList = remoteWaitConvertFilePathQuery.Select(x => x.Name).ToList();
			if (remoteWaitConvertFileNameList.Any())
			{
				foreach (var fileName in remoteWaitConvertFileNameList)
				{
					await cli.RPushAsync(listKey, fileName);
				}
			}
			else
			{
				Console.WriteLine("Nothing set to redis");
				Environment.Exit(1);
			}
		}


		var waitConvertFileName = await cli.BLPopAsync(listKey, 5);

		if (string.IsNullOrEmpty(waitConvertFileName))
		{
			Console.WriteLine("Nothing in redis list");
			Environment.Exit(1);
		}

		var remoteWaitConvertFileInfo = remoteWaitConvertFileInfos.FirstOrDefault(x => x.Name == waitConvertFileName);
		if (remoteWaitConvertFileInfo == null)
		{
			Console.WriteLine($"no file named {waitConvertFileName}");
			Environment.Exit(1);
		}

		Console.WriteLine("[SUCCESS] Get remote file need to convert");

		var convertingCacheKey = "Converting";
		var isSetKeySuccess = await cli.HSetNxAsync(convertingCacheKey, Path.GetFileNameWithoutExtension(remoteWaitConvertFileInfo.Name), "1");

		Console.WriteLine($"Copy remote file to local:{waitConvertFileName}[{FileHelper.FormatFileSize(remoteWaitConvertFileInfo.Length)}]");
		var localFolderPath = o.LocalFolderPath!;
		ProcessHelper.Excute("rclone", $"copy \"{remoteWaitConvertFileInfo.FullName}\" \"{localFolderPath}\"");
		var videoPath = Path.Combine(localFolderPath, waitConvertFileName);
		if (!File.Exists(videoPath))
		{
			Console.WriteLine("[Error] Copy remote file to local Failed");
			await cli.HDelAsync(convertingCacheKey, Path.GetFileNameWithoutExtension(waitConvertFileName));
			Environment.Exit(1);
		}
		Console.WriteLine("[SUCCESS] Copy remote file to local");

		var tempDirectoryPath = Path.Combine(localFolderPath, Path.GetFileNameWithoutExtension(videoPath).Trim());
		if (!Directory.Exists(tempDirectoryPath))
		{
			Directory.CreateDirectory(tempDirectoryPath);
		}
		var tempDirectoryInfo = new DirectoryInfo(tempDirectoryPath);
		var m3u8Path = Path.Combine(tempDirectoryPath, "video.m3u8");

		Console.WriteLine("Splitting video file");
		var isSplitToTsFileSuccess = await VideoHelper.SplitToTsFilesAsync(videoPath, tempDirectoryPath, m3u8Path);

		if (!isSplitToTsFileSuccess)
		{
			Environment.Exit(1);
		}
		File.Delete(videoPath);

		Console.WriteLine("[SUCCESS] Split video file");

		var tsFileInfos = tempDirectoryInfo.GetFiles("*.ts").OrderBy(x => x.Name).ToList();

		Console.WriteLine("Checking TS file size");
		// Check ts file size
		foreach (var tsFileInfo in tsFileInfos)
		{
			var fileLength = tsFileInfo.Length;
			if (fileLength > Consts._10MB)
			{
				Console.WriteLine($"[ERROR] File size limit exceeded: {tsFileInfo.Name} ({FileHelper.FormatFileSize(fileLength)})");
				Environment.Exit(1);
			}
		}

		Console.WriteLine("[SUCCESS] Check TS file size");

		// Parse M3U8 file from TS file
		Console.WriteLine("Parsing M3U8 file...");
		var m3u8 = await FileHelper.ParseM3U8FromFileAsync(m3u8Path);

		Console.WriteLine("[SUCCESS] Parse M3U8 file");

		// Merge TS file
		GitHubActionHelper.StartWriteGroupLog("Merging TS files");
		Console.WriteLine("Merging TS files...");
		await FileHelper.MergeTSFiles2Async(tempDirectoryPath);
		Console.WriteLine("[SUCCESS] Merge TS file");
		GitHubActionHelper.EndWriteGroupLog();

		// Write M3U8
		Console.WriteLine("Writing M3U8...");
		var mergedInfoList = new List<M3U8Info>();
		int tsLast = 0;
		for (int i = 0; i < tsFileInfos.Count; i++)
		{
			var tsFilePath = Path.Combine(tempDirectoryPath, $"{i:d4}.ts");
			if (!File.Exists(tsFilePath))
			{
				continue;
			}

			decimal mergedDuration = 0M;
			for (int j = tsLast; j <= i; j++)
			{
				mergedDuration += m3u8.Infos![j].Duration;
			}
			mergedInfoList.Add(new M3U8Info
			{
				Duration = mergedDuration,
				File = m3u8.Infos![i].File,
				OriFileName = m3u8.Infos![i].File,
			});
			tsLast = i + 1;
		}
		m3u8.Infos = mergedInfoList;

		await FileHelper.WriteM3u8ToFileAsync(m3u8, m3u8Path);

		// Encrypt TS
		GitHubActionHelper.StartWriteGroupLog("Encrypt & Upload");
		Console.WriteLine("Encrypting TS files...");
		string keyFilePath = Path.Combine(tempDirectoryPath, "KEY");
		string ivFilePath = Path.Combine(tempDirectoryPath, "IV");
		byte[]? encryptKey = null;
		byte[]? encryptIV = null;
		string encryptKeyUrl = string.Empty, encryptIvUrl = string.Empty;
		var isNeedUploadKeyFile = true;
		if (File.Exists(keyFilePath) && File.Exists(ivFilePath))
		{
			Console.WriteLine("Load Key & IV in local file");
			encryptKey = await File.ReadAllBytesAsync(keyFilePath);
			encryptIV = await File.ReadAllBytesAsync(ivFilePath);
		}
		else if (!string.IsNullOrWhiteSpace(o.Key) && !string.IsNullOrWhiteSpace(o.IV))
		{
			Console.WriteLine("Load Key & IV from string");
			encryptKey = Convert.FromBase64String(o.Key);
			encryptIV = Convert.FromBase64String(o.IV);
		}
		else if (!string.IsNullOrWhiteSpace(o.KeyUrl) && !string.IsNullOrWhiteSpace(o.IvUrl))
		{
			Console.WriteLine("Load Key & IV from web");
			encryptKeyUrl = o.KeyUrl;
			encryptIvUrl = o.IvUrl;
			encryptKey = await httpFactoryHandler.GetContentToBytesAsync(o.KeyUrl);
			encryptIV = await httpFactoryHandler.GetContentToBytesAsync(o.IvUrl);
			isNeedUploadKeyFile = false;
		}
		else
		{
			Console.WriteLine("create Key & IV");
			encryptKey = EncryptHelper.GetRandomBytes(16);
			encryptIV = EncryptHelper.GetRandomBytes(16);
		}

		await File.WriteAllBytesAsync(keyFilePath, encryptKey);
		await File.WriteAllBytesAsync(ivFilePath, encryptIV);

		if (isNeedUploadKeyFile)
		{
			Console.WriteLine("Uploading KEY & IV files");
			(var isUploadKeyFileSuccess, encryptKeyUrl) = await httpFactoryHandler.UploadFileAsync(keyFilePath, o.UploadUrl!, o.UploadAuthToken, oriUrl: o.OriginalUrl, replaceUrl: o.ReplaceUploadedUrl);
			if (!isUploadKeyFileSuccess)
			{
				Console.WriteLine("Upload KEY file Failed");
				Environment.Exit(1);
			}
			(var isUploadIvFileSuccess, encryptIvUrl) = await httpFactoryHandler.UploadFileAsync(ivFilePath, o.UploadUrl!, o.UploadAuthToken, oriUrl: o.OriginalUrl, replaceUrl: o.ReplaceUploadedUrl);
			if (!isUploadIvFileSuccess)
			{
				Console.WriteLine("Upload IV file Failed");
				Environment.Exit(1);
			}
		}

		string onlineM3u8FilePath = Path.Combine(tempDirectoryPath, $"{tempDirectoryInfo.Name}.m3u8");
		if (File.Exists(onlineM3u8FilePath))
		{
			m3u8 = await FileHelper.ParseM3U8FromFileAsync(onlineM3u8FilePath);
		}
		if (m3u8.Meta!.All(x => !x.StartsWith("#EXT-X-KEY:METHOD=AES-128")))
		{
			m3u8.Meta!.Add($"#EXT-X-KEY:METHOD=AES-128,URI=\"{encryptKeyUrl}\",IV=0x{EncryptHelper.ByteArrayToHexString(encryptIV)}");
			await FileHelper.WriteM3u8ToFileAsync(m3u8, onlineM3u8FilePath);
		}

		var m3u8InfoList = new ConcurrentBag<M3U8Info>(m3u8.Infos!);
		var tsFileList = Directory.EnumerateFiles(tempDirectoryPath, "*.ts").ToList();
		var total = tsFileList.Count;
		var processedCount = 0;

		await Parallel.ForEachAsync(tsFileList, new ParallelOptions
		{
			MaxDegreeOfParallelism = Environment.ProcessorCount + 2
		}, async (tsFilePath, cancellationToken) =>
		{
			string fileName = Path.GetFileName(tsFilePath);
			string encryptedFilePath = $"{tsFilePath}.jpg";
			var encryptedFileName = Path.GetFileName(encryptedFilePath);

			// file uploaded
			if (m3u8InfoList.Any(x => x.OriFileName == fileName && x.File!.StartsWith("http", StringComparison.OrdinalIgnoreCase)))
			{
				return;
			}

			Console.WriteLine("Encrypting File: " + fileName);
			if (!File.Exists(encryptedFilePath))
			{
				await EncryptHelper.EncryptFileAsync(tsFilePath, encryptKey, encryptIV, encryptedFilePath);
			}
			Console.WriteLine($"Uploading File[File size: {FileHelper.FormatFileSize(new FileInfo(encryptedFilePath).Length)}]: {fileName}");

			var encryptedFileUrl = encryptedFileName;

			var retryTime = 0;
			while (retryTime < 50)
			{
				try
				{
					var (IsSuccess, Url) = await httpFactoryHandler.UploadFileAsync(encryptedFilePath, o.UploadUrl!, o.UploadAuthToken, oriUrl: o.OriginalUrl, replaceUrl: o.ReplaceUploadedUrl, cancellationToken: cancellationToken);


					if (!IsSuccess || string.IsNullOrWhiteSpace(Url))
					{
						Console.WriteLine($"[WARNING] {fileName} Failed to upload");
					}
					else
					{
						Interlocked.Increment(ref processedCount);
						encryptedFileUrl = Url;
						//Console.WriteLine($"[UPLOAD]{encryptedFileName} {encryptedFileUrl}");
						Console.WriteLine($"[UPLOAD][{processedCount}/{total}]{encryptedFileName}");
						File.Delete(encryptedFilePath);
						break;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
				retryTime++;
			}

			foreach (var info in m3u8InfoList)
			{
				if (info.File == fileName)
				{
					info.File = encryptedFileUrl;
					info.OriFileName = fileName;
					info.EncryptFileName = encryptedFileName;
				}
			}
		});

		GitHubActionHelper.EndWriteGroupLog();

		m3u8.Infos = m3u8InfoList.OrderBy(x => x.OriFileName).ToList();

		await FileHelper.WriteM3u8ToFileAsync(m3u8, onlineM3u8FilePath);

		GitHubActionHelper.WriteToGitHubOutput("RESULT_PATH", onlineM3u8FilePath);
		//Console.WriteLine($"::set-output name=RESULT_PATH::{onlineM3u8FilePath}");
		//Console.WriteLine($"\"RESULT_PATH={onlineM3u8FilePath}\" >> $GITHUB_OUTPUT");

		await cli.HDelAsync(convertingCacheKey, Path.GetFileNameWithoutExtension(waitConvertFileName));
	});