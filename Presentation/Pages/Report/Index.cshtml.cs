using Assignment2.BusinessLogic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Assignment2.DataAccess.Models;
using OfficeOpenXml;

namespace Presentation.Pages.Report
{
    public class IndexModel : PageModel
    {
        private readonly INewsArticleService _newsArticleService;

        public IndexModel(INewsArticleService newsArticleService)
        {
            _newsArticleService = newsArticleService;
        }

        public List<Assignment2.DataAccess.Models.NewsArticle> Articles { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Start { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? End { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? GroupBy { get; set; }

        public int ActiveCount { get; set; }

        public int InactiveCount { get; set; }

        private bool IsAdmin() => HttpContext.Session.GetInt32("Role") is int r && r != 1 && r != 2;

        public IActionResult OnGet()
        {
            if (!IsAdmin()) return Unauthorized();

            DateTime? startDate = null;
            DateTime? endDate = null;

            if (!string.IsNullOrEmpty(Start) && DateTime.TryParse(Start, out var parsedStart))
            {
                startDate = parsedStart;
            }

            if (!string.IsNullOrEmpty(End) && DateTime.TryParse(End, out var parsedEnd))
            {
                endDate = parsedEnd;
            }
            

            var allArticles = _newsArticleService.GetAll();

            // Filter bằng LINQ
            var filteredArticles = allArticles.AsQueryable();

            if (startDate.HasValue)
            {
                filteredArticles = filteredArticles.Where(n => n.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                filteredArticles = filteredArticles.Where(n => n.CreatedDate <= endDate.Value);
            }

            Articles = filteredArticles.ToList();



            Console.WriteLine(GroupBy); 

            ActiveCount = Articles.Count(n => n.NewsStatus == true);
            InactiveCount = Articles.Count(n => n.NewsStatus == false);
            return Page();
        }

        public IActionResult OnGetExportToExcel(string? start, string? end)
        {
            DateTime? startDate = null;
            DateTime? endDate = null;

            if (!string.IsNullOrEmpty(start) && DateTime.TryParse(start, out var parsedStart))
            {
                startDate = parsedStart;
            }

            if (!string.IsNullOrEmpty(end) && DateTime.TryParse(end, out var parsedEnd))
            {
                endDate = parsedEnd;
            }

            // Lấy và filter articles
            var list = _newsArticleService.GetAll()
                .Where(n => !startDate.HasValue || n.CreatedDate >= startDate)
                .Where(n => !endDate.HasValue || n.CreatedDate < endDate.Value.AddDays(1))
                .OrderByDescending(n => n.CreatedDate)
                .ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("News Report");

            // Add headers
            worksheet.Cells[1, 1].Value = "Article ID";
            worksheet.Cells[1, 2].Value = "Title";
            worksheet.Cells[1, 3].Value = "Headline";
            worksheet.Cells[1, 4].Value = "Created Date";
            worksheet.Cells[1, 5].Value = "Category";
            worksheet.Cells[1, 6].Value = "Status";
            worksheet.Cells[1, 7].Value = "Created By";
            worksheet.Cells[1, 8].Value = "News Source";
            
            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }
            
            // Add data
            for (int i = 0; i < list.Count; i++)
            {
                var news = list[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = news.NewsArticleId;
                worksheet.Cells[row, 2].Value = news.NewsTitle;
                worksheet.Cells[row, 3].Value = news.Headline;
                worksheet.Cells[row, 4].Value = news.CreatedDate?.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cells[row, 5].Value = news.Category?.CategoryName;
                worksheet.Cells[row, 6].Value = news.NewsStatus == true ? "Active" : "Inactive";
                worksheet.Cells[row, 7].Value = news.CreatedBy?.AccountName;
                worksheet.Cells[row, 8].Value = news.NewsSource;
            }
            
            // Add summary section
            int summaryRow = list.Count + 3;
            worksheet.Cells[summaryRow, 1].Value = "Summary:";
            worksheet.Cells[summaryRow, 1].Style.Font.Bold = true;
            
            worksheet.Cells[summaryRow + 1, 1].Value = "Total Articles:";
            worksheet.Cells[summaryRow + 1, 2].Value = list.Count;
            
            worksheet.Cells[summaryRow + 2, 1].Value = "Active:";
            worksheet.Cells[summaryRow + 2, 2].Value = list.Count(n => n.NewsStatus == true);
            
            worksheet.Cells[summaryRow + 3, 1].Value = "Inactive:";
            worksheet.Cells[summaryRow + 3, 2].Value = list.Count(n => n.NewsStatus == false);
            
            if (startDate.HasValue || endDate.HasValue)
            {
                worksheet.Cells[summaryRow + 4, 1].Value = "Date Range:";
                worksheet.Cells[summaryRow + 4, 2].Value =
                    $"{(startDate?.ToString("yyyy-MM-dd") ?? "All")} to {(endDate?.ToString("yyyy-MM-dd") ?? "All")}";
            }
            
            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();
            
            var fileName = $"NewsReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var content = package.GetAsByteArray();
            
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
