# M3U8Converter
This is a .NET 7.0 console application that converts video files to m3u8 format using FFmpeg. The application accepts the following command-line options:

## Command-Line Options:
`--redis`: Required. Redis connection string.

`--remote-convert-folder`: Required. Remote folder path containing files to convert.

`--remote-result-folder`: Required. Remote folder path to store the result.

`--file-compare-type: Optional. File comparison type. Default is 1. Options:

1: File name ascending

2: File name descending

3: File size ascending

4: File size descending

`--local-folder`: Required. Local folder path.

`--binPath`: Optional. Path to the FFmpeg binary folder.

`--key`: Optional. encryption key.

`--iv`: Optional. encryption IV.

`--keyUrl`: Optional. URL for the encryption key.

`--ivUrl`: Optional. URL for the encryption IV.

`--UploadUrl`: Optional. URL to upload the converted files.

`--OriginalUrl`: Optional. Original uploaded URL.

`--ReplaceUrl`: Optional. URL to replace the uploaded URL.

`--UploadAuthToken`: Optional. Authentication token for upload.

## Example Usage:
```bash
M3U8Converter --redis "localhost:6379" --remote-convert-folder "/remote/folder" --remote-result-folder "/remote/result" --local-folder "/local/folder" --binPath "/path/to/ffmpeg" --UploadUrl "http://upload.url"
```

这是一个基于.NET 7.0的控制台应用程序，使用FFmpeg将视频文件转换为m3u8格式。该应用程序接受以下命令行选项：

命令行选项：
`--redis`: 必填。Redis连接字符串。

`--remote-convert-folder`: 必填。包含需要转换的文件的远程文件夹路径。

`--remote-result-folder`: 必填。存放转换结果的远程文件夹路径。

`--file-compare-type: 可选。文件比较方式。默认为1。选项：

1：文件名升序

2：文件名降序

3：文件大小升序

4：文件大小降序

`--local-folder`: 必填。本地文件夹路径。

`--binPath`: 可选。FFmpeg二进制文件夹路径。

`--key`: 可选。加密密钥。

`--iv`: 可选。加密初始化向量。

`--keyUrl`: 可选。加密密钥的URL。

`--ivUrl`: 可选。加密初始化向量的URL。

`--UploadUrl`: 可选。转换后文件上传的URL。

`--OriginalUrl`: 可选。原始上传URL。

`--ReplaceUrl`: 可选。替换上传的URL。

`--UploadAuthToken`: 可选。上传认证令牌。

## 示例使用：
```bash
ConsoleM3U8 --redis "localhost:6379" --remote-convert-folder "/remote/folder" --remote-result-folder "/remote/result" --local-folder "/local/folder" --binPath "/path/to/ffmpeg" --UploadUrl "http://upload.url"
```
