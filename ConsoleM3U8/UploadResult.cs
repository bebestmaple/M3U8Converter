namespace ConsoleM3U8
{
    public sealed class UploadResult<T>
    {
        public bool IsSuccess { get; set; }

        public T? Data { get; set; }
    }
}