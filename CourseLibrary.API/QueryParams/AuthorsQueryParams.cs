namespace CourseLibrary.API.QueryParams
{
    public class AuthorsQueryParams
    {
        const int maxPageSize = 20;

        // Filtering
        public string? Category { get; set; }
        
        // Searching
        public string? SearchQuery { get; set; }

        // Paging
        public int PageNumber { get; set; } = 1;
        private int _pageSize;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > maxPageSize) ? maxPageSize: value;
        }

        // Sorting
        public string OrderBy { get; set; } = "Name";

        // Projection / Shaping
        public string? Fields { get; set; }
    }
}
