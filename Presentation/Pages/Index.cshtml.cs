using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using NewsArticleModel = Assignment2.DataAccess.Models.NewsArticle;

namespace Assignment2.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly INewsArticleService _newsArticleService;
        private readonly ICategoryService _categoryService;
        private readonly ITagService _tagService;

        public IndexModel(
            ILogger<IndexModel> logger,
            INewsArticleService newsArticleService,
            ICategoryService categoryService,
            ITagService tagService)
        {
            _logger = logger;
            _newsArticleService = newsArticleService;
            _categoryService = categoryService;
            _tagService = tagService;
        }

        public List<NewsArticleModel> Articles { get; set; } = new();
        public IList<Category> Categories { get; set; } = new List<Category>();
        public IList<Tag> Tags { get; set; } = new List<Tag>();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }   // <-- filter theo category

        [BindProperty(SupportsGet = true)]
        public int? TagId { get; set; }        // <-- filter theo tag

        public int TotalPages { get; set; }
        private const int PageSize = 6;

        public void OnGet()
        {
            // lookup cho dropdown / hiển thị
            Categories = _categoryService.GetAll().ToList();
            Tags = _tagService.GetAll().ToList();

            // Chỉ lấy bài đã publish
            var query = _newsArticleService.GetAll(status: true);

            // Search
            if (!string.IsNullOrWhiteSpace(Search))
            {
                query = query.Where(a =>
                    (a.NewsTitle ?? "").Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                    (a.Headline ?? "").Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                    (a.NewsContent ?? "").Contains(Search, StringComparison.OrdinalIgnoreCase));
            }

            // Filter theo Category
            if (CategoryId.HasValue && CategoryId.Value > 0)
            {
                query = query.Where(a => a.CategoryId == CategoryId.Value);
            }

            // Filter theo Tag (Any tag matches)
            if (TagId.HasValue && TagId.Value > 0)
            {
                query = query.Where(a => a.Tags.Any(t => t.TagId == TagId.Value));
            }

            var total = query.Count();
            TotalPages = (int)Math.Ceiling((double)total / PageSize);

            Articles = query
                .OrderByDescending(a => a.CreatedDate) // gợi ý: sắp xếp mới nhất
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }
    }
}
