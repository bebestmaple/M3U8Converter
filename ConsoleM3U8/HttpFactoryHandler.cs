using System.Text;
using Newtonsoft.Json;

namespace ConsoleM3U8
{
    public sealed class HttpFactoryHandler
    {
        internal const string _HttpClientName = "HttpFactoryHandler";

        private readonly IHttpClientFactory _HttpClientFactory;


        public HttpFactoryHandler(IHttpClientFactory httpClientFactory)
        {
            _HttpClientFactory = httpClientFactory;
        }


        #region Common
        /// <summary>
        /// Get HttpClient
        /// </summary>
        /// <returns></returns>
        public HttpClient GetHttpClient()
        {
            return _HttpClientFactory.CreateClient(_HttpClientName);
        }
        #endregion

        /// <summary>
        /// Get Byte Array From Web
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public Task<byte[]> GetContentToBytesAsync(string url)
        {
            var client = GetHttpClient();
            return client.GetByteArrayAsync(url);
        }

        public async Task<(bool IsSuccess, string Url)> UploadFileAsync(string filePath,
                                                                       string uploadUrl,
                                                                       string? token = null,
                                                                       Encoding? encode = null,
                                                                       string mediaType = "application/json",
                                                                       string? oriUrl = null,
                                                                       string? replaceUrl = null,
                                                                       CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"{nameof(filePath)} is empty。", nameof(filePath));
            }

            if (string.IsNullOrWhiteSpace(uploadUrl))
            {
                throw new ArgumentException($"{nameof(uploadUrl)} is empty", nameof(uploadUrl));
            }
            try
            {
                encode ??= Encoding.UTF8;
                using var httpClient = GetHttpClient();
                httpClient.Timeout = TimeSpan.FromHours(1);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }
                var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                var bodyStr = JsonConvert.SerializeObject(new
                {
                    Data = fileBytes,
                    FileName = Path.GetFileName(filePath),
                    SpliceType = 1
                });
                using var content = new StringContent(bodyStr, encode, mediaType);

                var requestBodyBytes = await content.ReadAsByteArrayAsync(cancellationToken);
                var requestBodySizeInBytes = requestBodyBytes.Length;
                Console.WriteLine($"Request body size: {FileHelper.FormatFileSize(requestBodySizeInBytes)}");

                using var response = await httpClient.PostAsync(uploadUrl, content, cancellationToken);

                response.EnsureSuccessStatusCode();
                var uploadResult = JsonConvert.DeserializeObject<UploadResult<string>>(await response.Content.ReadAsStringAsync());
                if(uploadResult is not null and {IsSuccess : true} && !string.IsNullOrWhiteSpace(uploadResult.Data))
                {
                    if(!string.IsNullOrWhiteSpace(oriUrl) && !string.IsNullOrWhiteSpace(replaceUrl))
                    {
                        return (true,uploadResult.Data.Replace(oriUrl,replaceUrl));
                    }
                    return (true,uploadResult.Data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return (false, string.Empty);


        }
    }
}
