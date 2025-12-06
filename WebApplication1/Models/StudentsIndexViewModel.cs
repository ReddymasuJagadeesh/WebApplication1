using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication1.Models
{
    public class StudentsIndexViewModel
    {
        public IEnumerable<Students> Students { get; set; } = Enumerable.Empty<Students>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }

        // UI helpers
        public int[] AllowedPageSizes { get; set; } = new[] { 2, 3, 5, 10 };
        public string? Query { get; set; }

        public int TotalPages => PageSize == 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);

        public int StartItem => TotalItems == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int EndItem => TotalItems == 0 ? 0 : Math.Min(Page * PageSize, TotalItems);
        public int RemainingItems => Math.Max(0, TotalItems - EndItem);
    }
}