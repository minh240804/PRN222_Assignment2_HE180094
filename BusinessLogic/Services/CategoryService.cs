using Assignment2.DataAccess;
using Assignment2.DataAccess.Models;
using System.Collections.Generic;
using System.Linq;
using Assignment2.DataAccess.Repositories;

namespace Assignment2.BusinessLogic
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;

        public CategoryService(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public IEnumerable<Category> GetAll(bool? active = null)
            => _repo.GetAll(active);

        public Category? Get(short id)
            => _repo.Get(id);

        public void Add(Category cat)
        {
            cat.IsActive = true;
            _repo.Add(cat);
        }

        public (bool Success, string Message) Update(Category cat)
        {
            var existing = _repo.Get(cat.CategoryId);
            if (existing == null)
                return (false, "Category not found.");

            if (existing.NewsArticles != null && existing.NewsArticles.Any())
            {
                var articleCount = existing.NewsArticles.Count;
                return (false, $"Cannot update category. This category has {articleCount} article(s) associated with it. Please reassign or remove the articles first.");
            }

            existing.CategoryName = cat.CategoryName;
            existing.CategoryDesciption = cat.CategoryDesciption;
            existing.ParentCategoryId = cat.ParentCategoryId;

            _repo.Update(existing);
            return (true, "Category updated successfully.");
        }

        public bool Delete(short id)
        {
            var cat = _repo.Get(id);
            if (cat == null) return false;

            if (cat.NewsArticles is not null && cat.NewsArticles.Any())
                return false;

            _repo.Delete(cat);
            return true;
        }
    }
}
