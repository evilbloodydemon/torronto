using System.Collections;
using System.Collections.Generic;
using Torronto.DAL.Models;

namespace Torronto.BLL.Models
{
    public class Pagination<T> : IEnumerable
    {
        private readonly List<T> _elements = new List<T>();

        public int TotalItems { get; set; }
        public int PageSize { get; set; }

        public Pagination()
        {
        }

        public Pagination(List<T> input)
        {
            _elements = input;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T this[int index]
        {
            get { return _elements[index]; }
        }

        public int Count
        {
            get { return _elements.Count; }
        }
    }

    public class MoviePagination : Pagination<MovieItem>
    {
        public IEnumerable<Person> Actors { get; set; }

        public MoviePagination(List<MovieItem> input)
            : base(input)
        {
        }
    }
}