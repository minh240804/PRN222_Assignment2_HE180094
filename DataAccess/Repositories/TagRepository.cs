using System.Collections.Generic;
using System.Linq;
using Assignment2.DataAccess.Models;
using Assignment2.DataAccess.DAO;
using Microsoft.EntityFrameworkCore;

namespace Assignment2.DataAccess.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly TagDAO _dao;

        public TagRepository(TagDAO dao) => _dao = dao;

        public IEnumerable<Tag> GetAll() =>
            _dao.GetBaseQuery()
                .OrderBy(t => t.TagName)
                .ToList();

        public Tag? Get(int id) => _dao.GetById(id);

        public short GetNextId()
        {
            var max = _dao.GetMaxId();
            return (short)(max + 1);
        }

        public void Add(Tag tag)
        {
            if (tag.TagId == 0) tag.TagId = GetNextId();

            _dao.Add(tag);
        }

        public void Update(Tag tag)
        {
            _dao.Update(tag);
        }

        public void Delete(Tag tag)
        {
            _dao.Delete(tag);
        }

        public IEnumerable<Tag> Search(string? tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
                return GetAll();
            return _dao.Search(tagName);
        }

        public IEnumerable<NewsArticle> GetArticlesByTag(int tagId)
        {
            return _dao.GetArticlesByTag(tagId);
        }

    }
}