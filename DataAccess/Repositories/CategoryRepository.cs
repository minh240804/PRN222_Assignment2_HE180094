using System.Collections.Generic;
using System.Linq;
using Assignment2.DataAccess.Models;
using Assignment2.DataAccess.DAO;

namespace Assignment2.DataAccess.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly CategoryDAO _dao;

        public CategoryRepository(CategoryDAO dao) => _dao = dao;

        public IEnumerable<Category> GetAll(bool? active = null)
        {
            var q = _dao.GetBaseQuery();

            if (active.HasValue)
                q = q.Where(c => c.IsActive == active.Value);

            return q.OrderByDescending(c => c.CategoryId).ToList();
        }

        public Category? Get(short id) => _dao.GetById(id);

        public void Add(Category cat)
        {
            _dao.Add(cat);
        }

        public void Update(Category cat)
        {
            _dao.Update(cat);
        }

        public void Delete(Category cat)
        {
            _dao.Delete(cat);
        }

        public bool HasNews(short categoryId)
        {
            return _dao.CheckForNews(categoryId);
        }
    }
}