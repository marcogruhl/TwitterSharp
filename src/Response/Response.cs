namespace TwitterSharp.Response
{
    public class Response<T>
    {
        public T Data { get; set; }
        public RateLimit RateLimit { init; get; }
    }
}
