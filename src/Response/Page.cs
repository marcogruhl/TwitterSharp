using System;
using System.Threading.Tasks;

namespace TwitterSharp.Response
{
    public class Page<T> : Response<T>
    {
        public Func<Task<Page<T>>> NextAsync { init; get; }
        public Func<Task<Page<T>>> PreviousAsync { init; get; }

        public Page(T data) : base(data)
        {
        }

        public Page(T[] data) : base(data)
        {
        }
    }
}
