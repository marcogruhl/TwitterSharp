using System.Collections.Generic;

namespace TwitterSharp.Response
{
    public class Response<T> : List<T>
    {
        public Response(T data)
        {
            Add(data);
        }

        public Response(T[] data)
        {
            AddRange(data);
        }

        public RateLimit RateLimit { init; get; }
    }
}
