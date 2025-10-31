using System.Collections.Generic;
using Assignment2.DataAccess.Models;

namespace Assignment2.BusinessLogic
{
    public interface IArticleCommentService
    {
        IEnumerable<Comment> GetByArticle(string articleId);
        Comment? Get(int id);
        Comment Add(string articleId, short accountId, string commentText);
        void Delete(int id, short? deletedBy);
    }
}
