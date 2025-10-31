using System;
using System.Collections.Generic;
using System.Linq;
using Assignment2.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Assignment2.DataAccess.DAO
{
    public class ArticleCommentDAO
    {
        private readonly FunewsManagementContext _ctx;

        public ArticleCommentDAO(FunewsManagementContext ctx) => _ctx = ctx;

        public IEnumerable<Comment> GetByArticle(string articleId) =>
            _ctx.Comments
                .Include(c => c.Account)
                .Where(c => c.ArticleId == articleId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

        public Comment? GetById(int id) =>
            _ctx.Comments
                .Include(c => c.Account)
                .Include(c => c.Article)
                .FirstOrDefault(c => c.CommentId == id);

        public Comment? GetByIdForDelete(int id) =>
            _ctx.Comments
                .AsNoTracking()
                .FirstOrDefault(c => c.CommentId == id);

        public void Add(Comment comment)
        {
            _ctx.Comments.Add(comment);
            _ctx.SaveChanges();
        }

        public void Delete(Comment comment, short? deletedBy)
        {
            // Detach any tracked instances to avoid conflicts
            var trackedEntity = _ctx.ChangeTracker.Entries<Comment>()
                .FirstOrDefault(e => e.Entity.CommentId == comment.CommentId);
            
            if (trackedEntity != null)
            {
                _ctx.Entry(trackedEntity.Entity).State = EntityState.Detached;
            }

            // Verify the deletedBy account exists if provided
            if (deletedBy.HasValue)
            {
                var accountExists = _ctx.SystemAccounts.Any(a => a.AccountId == deletedBy.Value);
                if (!accountExists)
                {
                    Console.WriteLine($"Warning: Account {deletedBy.Value} not found. Setting DeletedBy to null.");
                    deletedBy = null;
                }
            }

            // Soft delete with tracking - only update specific fields
            comment.IsDeleted = true;
            comment.DeletedBy = deletedBy;
            comment.DeletedAt = DateTime.Now;
            
            // Attach and mark as modified
            _ctx.Attach(comment);
            _ctx.Entry(comment).Property(c => c.IsDeleted).IsModified = true;
            _ctx.Entry(comment).Property(c => c.DeletedBy).IsModified = true;
            _ctx.Entry(comment).Property(c => c.DeletedAt).IsModified = true;
            
            _ctx.SaveChanges();
        }
    }
}
