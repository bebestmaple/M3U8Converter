namespace ConsoleM3U8
{
    public static class FileHelper
	{
		public static async Task MergeTSFilesAsync(string tsFileDirPath)
		{
			var tempDirectoryInfo = new DirectoryInfo(tsFileDirPath);
			var tsFileInfos = tempDirectoryInfo.GetFiles("*.ts");
			int fastStart = 3;
			for (int i = 0; i < tsFileInfos.Length; i++)
			{
				string lastPath = Path.Combine(tsFileDirPath, $"{i - 1}.ts");
				string currentPath = Path.Combine(tsFileDirPath, $"{i}.ts");

				if (!File.Exists(lastPath))
				{
					continue;
				}

				var lastFileInfo = new FileInfo(lastPath);
				var currentFileInfo = new FileInfo(currentPath);

				if (lastFileInfo.Length + currentFileInfo.Length <= (fastStart > 0 ? 2 * Consts._1MB : Consts._1MB))
				{
					var tempTsFilePath = Path.Combine(tsFileDirPath, "~.ts");
					Console.BackgroundColor = ConsoleColor.DarkGreen;
					Console.ForegroundColor = ConsoleColor.White;
					Console.Write("[MERGE]");
					Console.ResetColor();
					Console.WriteLine($" {i}.ts <- {i - 1}.ts");
					await JoinFilesAsync(new List<string>{lastPath, currentPath}, tempTsFilePath);

					File.Delete(lastPath);
					File.Delete(currentPath);
					File.Move(tempTsFilePath, currentPath);
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