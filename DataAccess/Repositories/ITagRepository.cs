using System.Collections.Generic;
using Assignment2.DataAccess.Models;

namespace Assignment2.DataAccess.Repositories
{
    public interface ITagRepository
    {
        IEnumerable<Tag> GetAll();
        Tag? Get(int id);
        void Add(Tag tag);
        void Update(Tag tag);
        void Delete(Tag tag);
        IEnumerable<Tag> Search(string? tagName);
        IEnumerable<NewsArticle> GetArticlesByTag(int tagId);
    }
}
