using FFMpegCore;

namespace ConsoleM3U8
{
    public static class VideoHelper
    {
        public static async Task<bool> SplitToTsFilesAsync(string videoPath, string tempDirectoryPath,string m3u8Path)
        {
            var videoDuration = FFProbe.Analyse(videoPath).Duration;

            string videoTsPath = Path.Combine(tempDirectoryPath, "video.ts");
            var isConvetToTsSuccess = await FFMpegArguments
               .FromFileInput(videoPath)
               .OutputToFile(videoTsPath, true, opt => opt
                   //.WithCustomArgument("-c copy -vbsf h264_mp4toannexb -absf aac_adtstoasc")
                   .WithCustomArgument("-c:v libx264 -preset medium -c:a aac -vbsf h264_mp4toannexb -absf aac_adtstoasc")
                   //.WithCustomArgument("-c:v libx264 -crf 23 -preset medium -c:a aac -vbsf h264_mp4toannexb -absf aac_adtstoasc")
               )
               .NotifyOnProgress(percent => Console.Write($"Convert To TS：{percent}% "), videoDuration)
               .ProcessAsynchronously();

            if (!isConvetToTsSuccess)
            {
                Console.WriteLine($"Convert To TS Failed");
                return false;
            }
            var splitTsFilesPath = Path.Combine(tempDirectoryPath,"%04d.ts");

            var isSplitToTs =
                await FFMpegArguments
                .FromFileInput(videoTsPath)
                .OutputToFile(splitTsFilesPath, true, opt => opt
                    .WithCustomArgument($"-c copy -hls_list_size 0 -hls_allow_cache 1 -hls_time 1 -hls_flags split_by_time -f segment -segment_list \"{m3u8Path}\" ")
                )
                .NotifyOnProgress(percent => Console.Write($"Split TS：{percent}% "), videoDuration)
                .ProcessAsynchronously();

            File.Delete(videoTsPath);

            if (!isSplitToTs)
            {
                Console.WriteLine($"Split TS Failed");
                return false;
            }

            return true;
        }
    }
}