using System;
using System.Collections.Generic;
using Assignment2.DataAccess.Models;
using Assignment2.DataAccess.DAO;

namespace Assignment2.DataAccess.Repositories
{
    public class ArticleCommentRepository : IArticleCommentRepository
    {
        private readonly ArticleCommentDAO _dao;

        public ArticleCommentRepository(ArticleCommentDAO dao) => _dao = dao;

        public IEnumerable<Comment> GetByArticle(string articleId) =>
            _dao.GetByArticle(articleId);

        public Comment? Get(int id) => _dao.GetById(id);

        public void Add(Comment comment)
        {
            comment.CreatedAt = DateTime.Now;
            comment.IsDeleted = false;
            _dao.Add(comment);
        }

        public void Delete(int id, short? deletedBy)
        {
            var comment = _dao.GetByIdForDelete(id);
            if (comment != null)
            {
                _dao.Delete(comment, deletedBy);
            }
        }
    }
}
