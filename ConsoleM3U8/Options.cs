using CommandLine;

namespace ConsoleM3U8
{
    public sealed class Options
    {
        [Option('f', "File", Required = true, HelpText = "Set video file path need to convert")]
        public string? VideoPath { get; set; }

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
        public string? OriginalUrl{get;set;}

        [Option('r', "ReplaceUrl", Required = false, HelpText = "Set Replace Uploaded Url")]
        public string? ReplaceUploadedUrl{get;set;}

        [Option('t', "UploadAuthToken", Required = false, HelpText = "Set upload auth token")]
        public string? UploadAuthToken { get; set; }
    }
}