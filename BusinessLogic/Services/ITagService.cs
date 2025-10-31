using System.Collections.Generic;
using Assignment2.DataAccess.Models;

namespace Assignment2.BusinessLogic
{
    public interface ITagService
    {
        IEnumerable<Tag> GetAll();
        Tag? Get(int id);
        Task Add(Tag tag);
        (bool Success, string Message) Update(Tag tag);
        bool Delete(int id);
        IEnumerable<Tag> Search(string? tagName);
        IEnumerable<NewsArticle> GetArticlesByTag(int tagId);
    }
}
