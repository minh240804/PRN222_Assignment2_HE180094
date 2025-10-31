using System.Collections.Generic;
using Assignment2.DataAccess.Models;

namespace Assignment2.DataAccess.Repositories
{
    public interface IArticleCommentRepository
    {
        IEnumerable<Comment> GetByArticle(string articleId);
        Comment? Get(int id);
        void Add(Comment comment);
        void Delete(int id, short? deletedBy);
    }
}
