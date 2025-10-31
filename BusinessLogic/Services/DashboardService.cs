using Assignment2.DataAccess.Repositories;

namespace Assignment2.BusinessLogic
{
    public class DashboardService : IDashboardService
    {
        private readonly INewsArticleRepository _articleRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IArticleCommentRepository _commentRepository;

        public DashboardService(
            INewsArticleRepository articleRepository,
            IAccountRepository accountRepository,
            ICategoryRepository categoryRepository,
            IArticleCommentRepository commentRepository)
        {
            _articleRepository = articleRepository;
            _accountRepository = accountRepository;
            _categoryRepository = categoryRepository;
            _commentRepository = commentRepository;
        }

        public DashboardStats GetDashboardStats()
        {
            var articles = _articleRepository.GetAll().ToList();
            var accounts = _accountRepository.GetAll().ToList();
            var categories = _categoryRepository.GetAll().ToList();
            
            // Get all comments from all articles
            var allComments = new List<Assignment2.DataAccess.Models.Comment>();
            foreach (var article in articles)
            {
                var articleComments = _commentRepository.GetByArticle(article.NewsArticleId);
                allComments.AddRange(articleComments);
            }

            return new DashboardStats
            {
                TotalArticles = articles.Count,
                TotalAccounts = accounts.Count,
                TotalCategories = categories.Count,
                TotalComments = allComments.Count,
                PublishedArticles = articles.Count(a => a.NewsStatus == true),
                DraftArticles = articles.Count(a => a.NewsStatus == false),
                ActiveAccounts = accounts.Count(a => a.AccountStatus == true),
                InactiveAccounts = accounts.Count(a => a.AccountStatus == false)
            };
        }
    }
}
