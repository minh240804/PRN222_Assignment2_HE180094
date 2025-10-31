using System.Collections.Generic;
using System.Linq;
using Assignment2.DataAccess.Models;
using Assignment2.DataAccess.Repositories;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
namespace Assignment2.BusinessLogic
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _repo;

        public TagService(ITagRepository repo)
        {
            _repo = repo;
        }

        public IEnumerable<Tag> GetAll()
            => _repo.GetAll();

        public Tag? Get(int id)
            => _repo.Get(id);

        public async Task Add(Tag tag)
        {
            // ... (Logic kiểm tra trùng tên) ...
            _repo.Add(tag);
        //    await _hubContext.Clients.All.SendAsync("ReceiveTagListUpdate");
        //    await _hubContext.Clients.All.SendAsync("TagCreated", new { tagId = tag.TagId, tagName = tag.TagName });
        }

        public (bool Success, string Message) Update(Tag tag)
        {
            var existing = _repo.Get(tag.TagId);
            if (existing == null)
                return (false, "Tag not found.");

            if (string.IsNullOrWhiteSpace(tag.TagName))
                return (false, "Tag name cannot be empty.");

            var duplicateExists = _repo.GetAll()
                .Any(t => t.TagId != tag.TagId &&
                          !string.IsNullOrWhiteSpace(t.TagName) &&
                          !string.IsNullOrWhiteSpace(tag.TagName) &&
                          t.TagName.Equals(tag.TagName, StringComparison.OrdinalIgnoreCase));
            if (duplicateExists)
                return (false, "A tag with the same name already exists.");

            existing.Note = tag.Note;

            _repo.Update(existing);
            return (true, "Tag updated successfully!");
        }


        public bool Delete(int id)
        {
            var tag = _repo.Get(id);
            if (tag == null) return false;

            if (tag.NewsArticles is not null && tag.NewsArticles.Any())
                return false;

            _repo.Delete(tag);
            return true;
        }

        public IEnumerable<Tag> Search(string? tagName)
        {
            return _repo.Search(tagName);
        }

        public IEnumerable<NewsArticle> GetArticlesByTag(int tagId)
        {
            return _repo.GetArticlesByTag(tagId);
        }
    }
}
