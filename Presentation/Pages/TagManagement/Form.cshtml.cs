using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Presentation.Hubs;
using Azure;

namespace Presentation.Pages.TagManagement
{
    public class FormModel : PageModel
    {
        private readonly ITagService _tagService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IHttpContextAccessor _contextAccessor;

        public FormModel(ITagService tagService, IHubContext<NotificationHub> hubContext, IHttpContextAccessor contextAccessor)
        {
            _tagService = tagService;
            _hubContext = hubContext;
            _contextAccessor = contextAccessor;
        }

        [BindProperty]
        public Tag Tag { get; set; } = new Tag();

        public bool IsCreate => Tag.TagId == 0;
        private bool IsAdmin => _contextAccessor.HttpContext?.Session.GetInt32("Role") == 1;

        public IActionResult OnGet(int? id)
        {
            if (!IsAdmin)
                return Unauthorized();

            if (id.HasValue)
            {
                var existing = _tagService.Get(id.Value);
                if (existing == null)
                    return NotFound();
                Tag = existing;
            }
            else
            {
                Tag = new Tag();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!IsAdmin)
                return Unauthorized();

            if (!ModelState.IsValid)
                return Page();

            try
            {
                if (Tag.TagId == 0)
                {
                    // Kiểm tra tên đã tồn tại
                    var allTags = _tagService.GetAll();
                    bool isDuplicate = allTags.Any(t =>
                        t.TagName.Trim().ToLower() == Tag.TagName.Trim().ToLower() &&
                        t.TagId != Tag.TagId // Cho phép trùng chính nó nếu đang edit
                    );

                    if (isDuplicate)
                    {
                        ModelState.AddModelError("Tag.TagName", "Tag name already exists.");
                        return Page();
                    }

                    // Create
                    _tagService.Add(Tag);
                    await _hubContext.Clients.All.SendAsync("TagCreated", Tag.TagId, Tag.TagName, Tag.Note);
                    TempData["ToastMessage"] = "Tag created successfully.";
                }
                else
                {
                    // Update
                    var existing = _tagService.Get(Tag.TagId);
                    if (existing == null)
                        return NotFound();
                    // Kiểm tra tên đã tồn tại
                    //var allTags = _tagService.GetAll();
                    //bool isDuplicate = allTags.Any(t =>
                    //    t.TagName.Trim().ToLower() == Tag.TagName.Trim().ToLower() &&
                    //    t.TagId != Tag.TagId // Cho phép trùng chính nó nếu đang edit
                    //);

                    //if (isDuplicate)
                    //{
                    //    ModelState.AddModelError("Tag.TagName", "Tag name already exists.");
                    //    return Page();
                    //}

                    existing.TagName = Tag.TagName;
                    existing.Note = Tag.Note;
                    _tagService.Update(existing);
                    await _hubContext.Clients.All.SendAsync("TagUpdated", Tag.TagId, Tag.TagName,Tag.Note);
                    TempData["ToastMessage"] = "Tag updated successfully.";
                }

                return new JsonResult(new { success = true });
                //if (success = true)
                //return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return Page();
            }
        }
    }
}
