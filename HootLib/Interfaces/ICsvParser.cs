using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HootLib.Interfaces
{
    public interface ICsvParser : IDisposable
    {
        public T ParseLine<T>();
        public Task<T> ParseLineAsync<T>();
        public IEnumerable<T> ParseAllLines<T>();
        public Task<List<T>> ParseAllLinesAsync<T>();
    }
}