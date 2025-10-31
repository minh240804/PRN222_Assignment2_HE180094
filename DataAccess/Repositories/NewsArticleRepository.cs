using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Assignment2.DataAccess.Models;
using Assignment2.DataAccess.DAO;

namespace Assignment2.DataAccess.Repositories
{
    public class NewsArticleRepository : INewsArticleRepository
    {
        private readonly NewsArticleDAO _dao;

        public NewsArticleRepository(NewsArticleDAO dao) => _dao = dao;

        private static IQueryable<NewsArticle> IncludeAll(IQueryable<NewsArticle> q) =>
            q.Include(n => n.Category)
             .Include(n => n.Tags)
             .Include(n => n.CreatedBy)
             .Include(n => n.UpdatedBy);

        public IEnumerable<NewsArticle> GetAll(bool? active = null)
        {
            var q = IncludeAll(_dao.GetBaseQuery());

            if (active.HasValue)
                q = q.Where(n => n.NewsStatus == active.Value);

            return q.OrderByDescending(n => n.CreatedDate).ToList();
        }

        public NewsArticle? Get(string id)
        {
            return IncludeAll(_dao.GetBaseQuery())
                .FirstOrDefault(n => n.NewsArticleId == id);
        }

        public string GetNextId()
        {
            var maxId = int.Parse(_dao.GetMaxIdString());
            return (maxId + 1).ToString();
        }

        public void Add(NewsArticle news)
        {
            if (string.IsNullOrEmpty(news.NewsArticleId))
                news.NewsArticleId = GetNextId();

            _dao.Add(news);
        }

        public void Update(NewsArticle news)
        {
            _dao.Update(news);
        }

        public void Delete(NewsArticle news)
        {
            _dao.Delete(news);
        }

        public IEnumerable<NewsArticle> GetByStaff(short staffId) =>
            IncludeAll(_dao.GetBaseQuery())
                .Where(n => n.CreatedById == staffId)
                .OrderByDescending(n => n.CreatedDate)
                .ToList();

        public IEnumerable<NewsArticle> Search(string? keyword,
                                               DateTime? from,
                                               DateTime? to,
                                               short? categoryId,
                                               bool? status)
        {
            var q = IncludeAll(_dao.GetBaseQuery());

            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(n =>
                     EF.Functions.Like(n.NewsTitle, $"%{keyword}%") ||
                     EF.Functions.Like(n.Headline ?? string.Empty, $"%{keyword}%"));

            if (from.HasValue) q = q.Where(n => n.CreatedDate >= from.Value);
            if (to.HasValue) q = q.Where(n => n.CreatedDate <= to.Value);
            if (categoryId.HasValue) q = q.Where(n => n.CategoryId == categoryId.Value);
            if (status.HasValue) q = q.Where(n => n.NewsStatus == status.Value);

            return q.OrderByDescending(n => n.CreatedDate).ToList();
        }

        public IEnumerable<NewsArticle> GetReport(DateTime? start, DateTime? end)
        {
            var q = IncludeAll(_dao.GetBaseQuery());

            if (start.HasValue)
                q = q.Where(n => n.CreatedDate >= start.Value);
            if (end.HasValue)
                q = q.Where(n => n.CreatedDate <= end.Value);

            return q.OrderByDescending(n => n.CreatedDate).ToList();
        }

        public IEnumerable<NewsArticle> GetRelatedArticles(string currentArticleId, short? categoryId, int take = 3)
        {
            var currentArticle = IncludeAll(_dao.GetBaseQuery())
                .FirstOrDefault(n => n.NewsArticleId == currentArticleId);

            if (currentArticle == null)
                return Enumerable.Empty<NewsArticle>();

            var currentTagIds = currentArticle.Tags.Select(t => t.TagId).ToList();

            var relatedArticles = IncludeAll(_dao.GetBaseQuery())
                .Where(n => n.NewsArticleId != currentArticleId && n.NewsStatus == true)
                .Where(n => n.CategoryId == categoryId || n.Tags.Any(t => currentTagIds.Contains(t.TagId)))
                .OrderByDescending(n => n.CreatedDate)
                .Take(take)
                .ToList();

            return relatedArticles;
        }
    }
}