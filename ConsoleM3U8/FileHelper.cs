namespace ConsoleM3U8
{
	public static class FileHelper
	{
		public static async Task MergeTSFilesAsync(string tsFileDirPath)
		{
			var tempDirectoryInfo = new DirectoryInfo(tsFileDirPath);
			var tsFileInfos = tempDirectoryInfo.GetFiles("*.ts").OrderBy(x => x.Name).ToList();
			int fastStart = 3;
			for (int i = 0; i < tsFileInfos.Count; i++)
			{
				var lastFileInfo = tsFileInfos[i - 1];
				var currentFileInfo = tsFileInfos[i];

				if (lastFileInfo.Length + currentFileInfo.Length <= (fastStart > 0 ? Consts._2MB : Consts._1MB))
				{
					var tempTsFilePath = Path.Combine(tsFileDirPath, "~.ts");
					Console.BackgroundColor = ConsoleColor.DarkGreen;
					Console.ForegroundColor = ConsoleColor.White;
					Console.Write("[MERGE]");
					Console.ResetColor();
					Console.WriteLine($" {i}.ts <- {i - 1}.ts");
					await JoinFilesAsync(new List<string> { lastFileInfo.FullName, currentFileInfo.FullName }, tempTsFilePath);
					lastFileInfo.Delete();
					currentFileInfo.Delete();
					File.Move(tempTsFilePath, currentFileInfo.FullName);
				}
				else
				{
					Console.BackgroundColor = ConsoleColor.DarkCyan;
					Console.ForegroundColor = ConsoleColor.White;
					Console.Write("[SKIP]");
					Console.ResetColor();
					if (fastStart > 0)
					{
						fastStart--;
						Console.WriteLine($" {i}.ts ({Math.Round(lastFileInfo.Length / Consts._1MB, 2)} MB) + {Math.Round(currentFileInfo.Length / Consts._1MB, 2)} MB)");
					}
					else
					{
						Console.WriteLine($" {i}.ts ({Math.Round(currentFileInfo.Length / Consts._1MB, 2)} MB)");
					}
				}
			}
		}

		public static async Task MergeTSFiles2Async(string tsFileDirPath)
		{
			var tempDirectoryInfo = new DirectoryInfo(tsFileDirPath);
			var tsFileInfos = tempDirectoryInfo.GetFiles("*.ts").OrderBy(x => x.Name).ToList();
			var tsFileCount = tsFileInfos.Count;
			int fastStart = 3;
			int lastCount = 0;
			var sizes = new decimal[] { Consts._1MB * 4, Consts._1MB * 3, Consts._2MB, Consts._1MB };

			for (int i = 0; i < tsFileCount - 1; i++)
			{
				var currentFileInfo = tsFileInfos[i];
				var nextFileInfo = tsFileInfos[i + 1];

				if (currentFileInfo.Length + nextFileInfo.Length <= sizes[fastStart])
				{
					var tempTsFilePath = Path.Combine(tsFileDirPath, "~.ts");
					await JoinFilesAsync(new List<string> { currentFileInfo.FullName, nextFileInfo.FullName }, tempTsFilePath);
					currentFileInfo.Delete();
					nextFileInfo.Delete();
					File.Move(tempTsFilePath, nextFileInfo.FullName);
				}
				else
				{
					Console.BackgroundColor = ConsoleColor.DarkGreen;
					Console.ForegroundColor = ConsoleColor.White;
					Console.Write("[MERGE]");


					if (fastStart > 0)
					{
						fastStart--;
						Console.WriteLine($" {lastCount}-{i - 1} -> {i}.ts ({Math.Round(currentFileInfo.Length / Consts._1MB, 2)} MB FastStart)");
					}
					else
					{
						Console.WriteLine($" {lastCount}-{i - 1} -> {i}.ts ({Math.Round(currentFileInfo.Length / Consts._1MB, 2)} MB)");
					}

					lastCount = i + 1;
				}

				if (i == tsFileCount - 2)
				{
					Console.BackgroundColor = ConsoleColor.DarkCyan;
					Console.ForegroundColor = ConsoleColor.White;
					Console.Write("[MERGE]");
					Console.WriteLine($" {lastCount}-{i} -> {i + 1}.ts ({Math.Round(nextFileInfo.Length / Consts._1MB, 2)} MB)");
				}
			}
		}

		public static async Task JoinFilesAsync(IEnumerable<string> inputFiles, string outputFile)
		{
			using var output = File.Create(outputFile);

			foreach (var inputFile in inputFiles)
			{
				using var input = File.OpenRead(inputFile);

				await input.CopyToAsync(output);

			}

		}
		public static async Task<M3U8> ParseM3U8FromFileAsync(string m3u8FilePath)
		{
			var m3u8 = new M3U8
			{
				Meta = new List<string>(),
				Infos = new List<M3U8Info>(),
			};
			var m3u8SourceArr = await File.ReadAllLinesAsync(m3u8FilePath);

			for (int i = 0; i < m3u8SourceArr.Length; i++)
			{
				if (m3u8SourceArr[i] == "#EXTM3U" || m3u8SourceArr[i] == "#EXT-X-ENDLIST")
				{
					continue;
				}
				if (m3u8SourceArr[i].StartsWith("#EXTINF:"))
				{
					var infoArr = m3u8SourceArr[i].Replace("#EXTINF:", "").Split(",");
					var duration = Convert.ToDecimal(infoArr[0]);
					var oriFileName = infoArr.Length > 1 ? infoArr[1] : string.Empty;
					var encryptFileName = infoArr.Length > 2 ? infoArr[2] : string.Empty;
					m3u8.Infos.Add(new M3U8Info
					{
						Duration = duration,
						OriFileName = oriFileName,
						EncryptFileName = encryptFileName,
						File = m3u8SourceArr[++i]
					});
				}
				else
				{
					m3u8.Meta.Add(m3u8SourceArr[i]);
				}
			}

			return m3u8;
		}

		public static async Task WriteM3u8ToFileAsync(M3U8 m3u8, string m3u8FilePath)
		{
			var m3u8ContentList = new List<string> { "#EXTM3U" };
			m3u8ContentList.AddRange(m3u8.Meta!);
			foreach (var info in m3u8.Infos!)
			{
				var infoList = new List<string>
					{
						info.Duration.ToString(),
						info.OriFileName??string.Empty,
						info.EncryptFileName??string.Empty
					}.Where(x => !string.IsNullOrEmpty(x));
				m3u8ContentList.Add($"#EXTINF:{string.Join(",", infoList)}");
				m3u8ContentList.Add(info.File!);
			}
			m3u8ContentList.Add("#EXT-X-ENDLIST");

			await File.WriteAllLinesAsync(m3u8FilePath, m3u8ContentList);
		}
	}
}