using Assignment2.DataAccess.Models;
using System;
using System.Collections.Generic;

namespace Assignment2.BusinessLogic
{
    public interface INewsArticleService
    {
        IEnumerable<NewsArticle> GetAll(bool? status = null);
        IEnumerable<NewsArticle> GetByStaff(short staffId);
        IEnumerable<NewsArticle> Search(string? keyword, DateTime? from, DateTime? to, short? categoryId, bool? status);
        NewsArticle? Get(string id);
        public string GetNextId();

        void Add(NewsArticle news, IEnumerable<int> tagIds);
        void Update(NewsArticle news, IEnumerable<int> tagIds);
        bool Delete(string id);
        public IEnumerable<NewsArticle> GetByDateRange(DateTime? start, DateTime? end);
        IEnumerable<NewsArticle> GetRelatedArticles(string currentArticleId, short? categoryId, int take = 3);
    }
}
