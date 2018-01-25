namespace Torronto.BLL.Models
{
    public class PaginationParams
    {
        private int _pageSize;

        public int Page { get; set; }
        public int DefaultPageSize { get; set; }
        public int PageSize
        {
            get { return _pageSize > 0 ? _pageSize : DefaultPageSize; }
            set
            {
                if (value <= 100)
                {
                    _pageSize = value;
                }
            }
        }

        public bool NoCount { get; set; }
        public int SkipCount
        {
            get { return Page > 1 ? PageSize * (Page - 1) : 0; }
        }

        public string Order { get; set; }

        public PaginationParams()
        {
            Page = 1;
            DefaultPageSize = 100;
        }
    }
}