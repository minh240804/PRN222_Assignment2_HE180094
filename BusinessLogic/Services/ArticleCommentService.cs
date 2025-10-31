using System;
using System.Collections.Generic;
using Assignment2.DataAccess.Models;
using Assignment2.DataAccess.Repositories;

namespace Assignment2.BusinessLogic
{
    public class ArticleCommentService : IArticleCommentService
    {
        private readonly IArticleCommentRepository _repo;

        public ArticleCommentService(IArticleCommentRepository repo)
        {
            _repo = repo;
        }

        public IEnumerable<Comment> GetByArticle(string articleId) =>
            _repo.GetByArticle(articleId);

        public Comment? Get(int id) => _repo.Get(id);

        public Comment Add(string articleId, short accountId, string commentText)
        {
            var comment = new Comment
            {
                ArticleId = articleId,
                AccountId = accountId,
                Content = commentText,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            _repo.Add(comment);
            
            // Return the comment with the generated ID
            return comment;
        }

        public void Delete(int id, short? deletedBy)
        {
            _repo.Delete(id, deletedBy);
        }
    }
}
