using Assignment2.DataAccess;
using Assignment2.DataAccess.Models;
using Assignment2.DataAccess.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assignment2.BusinessLogic
{
    public class NewsArticleService : INewsArticleService
    {
        private readonly INewsArticleRepository _newsRepo;
        private readonly ITagRepository _tagRepo;

        public NewsArticleService(INewsArticleRepository newsRepo, ITagRepository tagRepo)
        {
            _newsRepo = newsRepo;
            _tagRepo = tagRepo;
        }

        public IEnumerable<NewsArticle> GetAll(bool? status = null) =>
            _newsRepo.GetAll(status);

        public string GetNextId() => _newsRepo.GetNextId();


        public IEnumerable<NewsArticle> GetByStaff(short staffId) =>
            _newsRepo.GetByStaff(staffId);

        public IEnumerable<NewsArticle> Search(string? keyword, DateTime? from, DateTime? to, short? categoryId, bool? status) =>
            _newsRepo.Search(keyword, from, to, categoryId, status);

        public NewsArticle? Get(string id) =>
            _newsRepo.Get(id);

        public void Add(NewsArticle news, IEnumerable<int> tagIds)
        {
            news.NewsArticleId = _newsRepo.GetNextId();
            news.CreatedDate = DateTime.Now;
            news.Tags = LoadTags(tagIds);
            _newsRepo.Add(news);
        }

        public void Update(NewsArticle news, IEnumerable<int> tagIds)
        {
            var current = _newsRepo.Get(news.NewsArticleId);
            if (current == null) return;

            current.NewsTitle = news.NewsTitle;
            current.Headline = news.Headline;
            current.NewsContent = news.NewsContent;
            current.NewsSource = news.NewsSource;
            current.CategoryId = news.CategoryId;
            current.NewsStatus = news.NewsStatus;
            current.ModifiedDate = DateTime.Now;
            current.UpdatedById = news.UpdatedById;

            current.Tags.Clear();
            foreach (var tag in LoadTags(tagIds))
                current.Tags.Add(tag);

            _newsRepo.Update(current);
        }

        public bool Delete(string id)
        {
            var news = _newsRepo.Get(id);
            if (news == null) return false;

            news.Tags.Clear();
            _newsRepo.Update(news);
            _newsRepo.Delete(news);

            return true;
        }

        private List<Tag> LoadTags(IEnumerable<int> ids)
        {
            var list = new List<Tag>();
            foreach (var id in ids)
            {
                var tag = _tagRepo.Get(id);
                if (tag != null) list.Add(tag);
            }
            return list;
        }
        public IEnumerable<NewsArticle> GetByDateRange(DateTime? start, DateTime? end)
        {
            return _newsRepo.GetReport(start, end);
        }

        public IEnumerable<NewsArticle> GetRelatedArticles(string currentArticleId, short? categoryId, int take = 3)
        {
            return _newsRepo.GetRelatedArticles(currentArticleId, categoryId, take);
        }
    }
}
