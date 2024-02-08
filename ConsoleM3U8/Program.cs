// See https://aka.ms/new-console-template for more information
using CommandLine;
using ConsoleM3U8;
using Microsoft.Extensions.DependencyInjection;

// 创建依赖注入容器
var serviceProvider = new ServiceCollection()
			.AddHttpFactoryHandler()
			.BuildServiceProvider();

// 从容器中获取服务实例
var httpFactoryHandler = serviceProvider.GetRequiredService<HttpFactoryHandler>();

await CommandLine.Parser.Default.ParseArguments<Options>(args)
	.WithParsedAsync(async o =>
	{
		var videoPath = o.VideoPath;
		var tempDirectoryPath = Path.Combine(Path.GetDirectoryName(videoPath)!, Path.GetFileNameWithoutExtension(videoPath)!.Trim());
		if (!Directory.Exists(tempDirectoryPath))
		{
			Directory.CreateDirectory(tempDirectoryPath);
		}
		var tempDirectoryInfo = new DirectoryInfo(tempDirectoryPath);
		var m3u8Path = Path.Combine(tempDirectoryPath, "video.m3u8");

		var isSplitToTsFileSuccess = await VideoHelper.SplitToTsFilesAsync(videoPath!, tempDirectoryPath, m3u8Path);

		if (!isSplitToTsFileSuccess)
		{
			return;
		}

		var tsFileInfos = tempDirectoryInfo.GetFiles("*.ts");
		// 检查TS文件大小是否超过限制
		foreach (var tsFileInfo in tsFileInfos)
		{
			var fileLength = tsFileInfo.Length;
			if (fileLength > 10 * Consts._1MB)
			{
				Console.WriteLine($"[ERROR] File size limit exceeded: {tsFileInfo.Name} ({Math.Round(fileLength / Consts._1MB, 2)} MB)");
				return;
			}
		}

		//解析M3U8
		Console.WriteLine("Parsing M3U8...");
		var m3u8 = await FileHelper.ParseM3U8FromFileAsync(m3u8Path);

		// 合并TS文件
		Console.WriteLine("Merging TS files...");
		await FileHelper.MergeTSFilesAsync(tempDirectoryPath);

		// 写入M3U8文件
		Console.WriteLine("Writing M3U8...");
		var mergedInfoList = new List<M3U8Info>();
		int tsLast = 0;
		for (int i = 0; i < tsFileInfos.Length; i++)
		{
			var tsFilePath = tsFileInfos[i].FullName;
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
				File = m3u8.Infos![i].File
			});
			tsLast = i + 1;
		}
		m3u8.Infos = mergedInfoList;

		string mergedM3u8Path = Path.Combine(tempDirectoryPath, "video.m3u8");
		await FileHelper.WriteM3u8ToFileAsync(m3u8, mergedM3u8Path);

		// 加密TS文件
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
			var uploadKeyFileRet = await httpFactoryHandler.UploadFileAsync(keyFilePath, o.UploadUrl!, o.UploadAuthToken, oriUrl: o.OriginalUrl, replaceUrl: o.ReplaceUploadedUrl);
			if (!uploadKeyFileRet.IsSuccess)
			{
				Console.WriteLine("Upload KEY file Failed");
				return;
			}
			encryptKeyUrl = uploadKeyFileRet.Url;
			var uploadIvFileRet = await httpFactoryHandler.UploadFileAsync(ivFilePath, o.UploadUrl!, o.UploadAuthToken, oriUrl: o.OriginalUrl, replaceUrl: o.ReplaceUploadedUrl);
			if (!uploadKeyFileRet.IsSuccess)
			{
				Console.WriteLine("Upload IV file Failed");
				return;
			}
			encryptIvUrl = uploadIvFileRet.Url;
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

		foreach (string tsFilePath in Directory.EnumerateFiles(tempDirectoryPath, "*.ts"))
		{
			string fileName = Path.GetFileName(tsFilePath);
			string encryptedFilePath = tsFilePath + ".jpg";
			var encryptedFileName = Path.GetFileName(encryptedFilePath);

			// 该文件已上传
			if (m3u8.Infos!.Any(x => x.OriFileName == fileName && x.File!.StartsWith("http", StringComparison.OrdinalIgnoreCase)))
			{
				continue;
			}

			Console.WriteLine("Encrypting File: " + fileName);
			if (!File.Exists(encryptedFilePath))
			{
				await EncryptHelper.EncryptFileAsync(tsFilePath, encryptKey, encryptIV, encryptedFilePath);
			}
			Console.WriteLine($"Uploading File[File size: {new FileInfo(encryptedFilePath).Length / Consts._1MB:F2} MB]: {fileName}");

			var encryptedFileUrl = encryptedFileName;

			var retryTime = 0;
			while (retryTime < 3)
			{
				try
				{
					var uploadFileRet = await httpFactoryHandler.UploadFileAsync(encryptedFilePath, o.UploadUrl!, o.UploadAuthToken, oriUrl: o.OriginalUrl, replaceUrl: o.ReplaceUploadedUrl);


					if (!uploadFileRet.IsSuccess || string.IsNullOrWhiteSpace(uploadFileRet.Url))
					{
						Console.ForegroundColor = ConsoleColor.White;
						Console.BackgroundColor = ConsoleColor.DarkYellow;
						Console.Write("[WARNING] ");
						Console.ResetColor();
						Console.WriteLine(fileName + " Failed to upload");
					}
					else
					{
						encryptedFileUrl = uploadFileRet.Url;
						Console.ForegroundColor = ConsoleColor.White;
						Console.BackgroundColor = ConsoleColor.DarkGreen;
						Console.Write("[UPLOAD] ");
						Console.ResetColor();
						Console.WriteLine(encryptedFileName + " " + encryptedFileUrl);
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

			foreach (var info in m3u8.Infos!)
			{
				if (info.File == fileName)
				{
					info.File = encryptedFileUrl;
					info.OriFileName = fileName;
					info.EncryptFileName = encryptedFileName;
				}
			}
			await FileHelper.WriteM3u8ToFileAsync(m3u8, onlineM3u8FilePath);
		}
	});









