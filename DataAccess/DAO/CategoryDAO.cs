using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Assignment2.DataAccess.Models;

namespace Assignment2.DataAccess.DAO
{
    public class CategoryDAO
    {
        private readonly FunewsManagementContext _context;

        public CategoryDAO(FunewsManagementContext context) => _context = context;

        public IQueryable<Category> GetBaseQuery()
        {
            // Trả về IQueryable để Repository có thể thêm các điều kiện lọc/sắp xếp/Include
            return _context.Categories
                .Include(c => c.ParentCategory)
                .AsQueryable();
        }

        public Category? GetById(short id) =>
            _context.Categories
                .Include(c => c.NewsArticles)
                .FirstOrDefault(c => c.CategoryId == id);

        public void Add(Category cat)
        {
            _context.Categories.Add(cat);
            _context.SaveChanges();
        }

        public void Update(Category cat)
        {
            _context.Categories.Update(cat);
            _context.SaveChanges();
        }

        public void Delete(Category cat)
        {
            _context.Categories.Remove(cat);
            _context.SaveChanges();
        }

        public bool CheckForNews(short categoryId)
        {
            // Logic kiểm tra tồn tại (truy vấn thô) được giữ ở DAO
            return _context.NewsArticles.Any(n => n.CategoryId == categoryId);
        }
    }
}