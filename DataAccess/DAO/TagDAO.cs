using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Assignment2.DataAccess.Models;

namespace Assignment2.DataAccess.DAO
{
    public class TagDAO
    {
        private readonly FunewsManagementContext _ctx;

        public TagDAO(FunewsManagementContext ctx) => _ctx = ctx;

        public IQueryable<Tag> GetBaseQuery() =>
            _ctx.Tags.AsQueryable();

        public Tag? GetById(int id) =>
            _ctx.Tags
                .Include(t => t.NewsArticles)
                .FirstOrDefault(t => t.TagId == id);

        public void Add(Tag tag)
        {
            _ctx.Tags.Add(tag);
            _ctx.SaveChanges();
        }

        public void Update(Tag tag)
        {
            _ctx.Tags.Update(tag);
            _ctx.SaveChanges();
        }

        public void Delete(Tag tag)
        {
            _ctx.Tags.Remove(tag);
            _ctx.SaveChanges();
        }

        public IEnumerable<Tag> Search(string? tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
                return _ctx.Tags.ToList();

            return _ctx.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t.TagName) &&
                            t.TagName.Contains(tagName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public IEnumerable<NewsArticle> GetArticlesByTag(int tagId)
        {
            return _ctx.NewsArticles
                .Include(a => a.Category)
                .Include(a => a.Tags)
                .Include(a => a.CreatedBy)
                .Include(a => a.UpdatedBy)
                .Where(a => a.Tags.Any(t => t.TagId == tagId))
                .OrderByDescending(a => a.CreatedDate)
                .ToList();
        }
        public short GetMaxId() =>
            _ctx.Tags.Max(a => (short?)a.TagId) ?? 0;
    }
}