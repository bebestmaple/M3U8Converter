using System.Text;

namespace ConsoleM3U8
{
    public class GitHubActionHelper
    {
        /// <summary>
        /// Masking a value in a log
        /// </summary>
        /// <param name="value">value need to mask in log</param>
        /// <exception cref="ArgumentException">value is null or whitespace</exception>
        public static void MaskValueInLog(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{nameof(value)} must not be null or empty", nameof(value));
            }

            Console.WriteLine($"::add-mask::{value}");
        }

        public static void StartWriteGroupLog(string groupTitle)
        {
            if (string.IsNullOrWhiteSpace(groupTitle))
            {
                throw new ArgumentException($"{nameof(groupTitle)} must not be null or empty", nameof(groupTitle));
            }

            Console.WriteLine($"::group::{groupTitle}");
        }

        public static void EndWriteGroupLog()
        {
            Console.WriteLine($"::endgroup::");
        }

        public static void WriteToGitHubOutput(string key, string val)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException($"{nameof(key)} must not be null or empty", nameof(key));
            }

            if (string.IsNullOrWhiteSpace(val))
            {
                throw new ArgumentException($"{nameof(val)} must not be null or empty", nameof(val));
            }

            var gitHubOutputFile = GetGitHubOutputFilePath();
            if (!string.IsNullOrWhiteSpace(gitHubOutputFile))
            {
                using StreamWriter textWriter = new(gitHubOutputFile, true, Encoding.UTF8);
                textWriter.WriteLine($"{key}={val}");
            }
        }

        public static string? GetGitHubOutputFilePath()
        {
            return Environment.GetEnvironmentVariable("GITHUB_OUTPUT"); ;
        }
    }
}