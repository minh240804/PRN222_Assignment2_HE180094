using System.Collections.Generic;
using System.Linq;
using Assignment2.DataAccess.Models;

namespace Assignment2.DataAccess.DAO
{
    public class NewsArticleDAO
    {
        private readonly FunewsManagementContext _ctx;

        public NewsArticleDAO(FunewsManagementContext ctx) => _ctx = ctx;

        // Trả về IQueryable cơ bản để Repository có thể tùy chỉnh
        public IQueryable<NewsArticle> GetBaseQuery() =>
            _ctx.NewsArticles.AsQueryable();

        public NewsArticle? GetById(string id) => _ctx.NewsArticles.FirstOrDefault(n => n.NewsArticleId == id);

        public string GetMaxIdString()
        {
            var max = _ctx.NewsArticles
                .Select(n => n.NewsArticleId)
                .AsEnumerable()
                .Select(id => int.TryParse(id, out int val) ? val : 0)
                .DefaultIfEmpty(0)
                .Max();

            return max.ToString();
        }

        public void Add(NewsArticle news)
        {
            _ctx.NewsArticles.Add(news);
            _ctx.SaveChanges();
        }

        public void Update(NewsArticle news)
        {
            _ctx.NewsArticles.Update(news);
            _ctx.SaveChanges();
        }

        public void Delete(NewsArticle news)
        {
            // First, delete all comments associated with this article
            var comments = _ctx.Comments.Where(c => c.ArticleId == news.NewsArticleId).ToList();
            if (comments.Any())
            {
                _ctx.Comments.RemoveRange(comments);
                _ctx.SaveChanges(); // Save changes to delete comments first
            }

            // Then delete the article
            _ctx.NewsArticles.Remove(news);
            _ctx.SaveChanges();
        }
    }
}