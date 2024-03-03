using CommandLine;

namespace ConsoleM3U8
{
    public sealed class Options
    {

        [Option("remote-convert-folder", Required = true, HelpText = "Set remote path need to convert")]
        public string? RemoteWaitConvertFolderPath { get; set; }

        [Option("remote-result-folder", Required = true, HelpText = "Set remote result path")]
        public string? RemoteResultFolderPath { get; set; }

        [Option("file-compare-type", Default = 1, Min = 1, Max = 4, HelpText = "1(default):file name asc 2: file name desc 3:file size asc 4: file size desc")]
        public int FileCompareType { get; set; } = 1;

        [Option("local-folder", Required = true, HelpText = "Set local path")]
        public string? LocalFolderPath { get; set; }

        [Option('b', "binPath", Required = false, HelpText = "Set the FFMpeg Binary Folder Path")]
        public string? FFMpegBinaryFolderPath { get; set; }

        [Option('k', "key", Required = false, HelpText = "Set the base64 KEY to encrypt")]
        public string? Key { get; set; }

        [Option('v', "iv", Required = false, HelpText = "Set the base64 IV to encrypt")]
        public string? IV { get; set; }

        [Option("keyUrl", Required = false, HelpText = "Set the KEY Url to encrypt")]
        public string? KeyUrl { get; set; }

        [Option("ivUrl", Required = false, HelpText = "Set the  IV Url to encrypt")]
        public string? IvUrl { get; set; }

        [Option('u', "UploadUrl", Required = false, HelpText = "Set upload Url")]
        public string? UploadUrl { get; set; }

        [Option('o', "OriginalUrl", Required = false, HelpText = "Set Original Uploaded Url")]
        public string? OriginalUrl { get; set; }

        [Option('r', "ReplaceUrl", Required = false, HelpText = "Set Replace Uploaded Url")]
        public string? ReplaceUploadedUrl { get; set; }

        [Option('t', "UploadAuthToken", Required = false, HelpText = "Set upload auth token")]
        public string? UploadAuthToken { get; set; }
    }
}