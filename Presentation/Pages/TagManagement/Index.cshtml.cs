using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Assignment2.BusinessLogic;
using Assignment2.DataAccess.Models;
using Presentation.Hubs;


namespace Presentation.Pages.TagManagement 
{
    public class IndexModel : PageModel
    {
        private readonly ITagService _tagService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<NotificationHub> _hubContext;

        public IndexModel(ITagService tagService, IHttpContextAccessor httpContextAccessor,
            IHubContext<NotificationHub> hubContext)
        {
            _tagService = tagService;
            _httpContextAccessor = httpContextAccessor;
            _hubContext = hubContext;
        }

        public List<Tag> Tags { get; set; } = new List<Tag>();


        

        public void OnGet()
        {
            //if (!IsAuthorized()) return Forbid(); 
            Tags = _tagService.GetAll().ToList();
            
        }
        public bool IsStaff => _httpContextAccessor.HttpContext?.Session.GetInt32("Role") == 1;


        public IActionResult OnGetCreatePopup()
        {
            
            return Partial("Form", new Tag());
        }

        public IActionResult OnGetEditPopup(int id)
        {
            
            var tag = _tagService.Get(id);
            if (tag == null) return NotFound();
            return Partial("Form", tag);
        }


        //[HttpPost]
        //public async Task<IActionResult> OnPostCreateAsync(Tag tag)
        //{
        //    if (!IsAuthorized()) return Forbid();
        //    if (!ModelState.IsValid)
        //    {
        //        return Partial("_TagForm", tag);
        //    }
        //    try
        //    {
        //         _tagService.Add(tag);
        //        return new JsonResult(new { success = true });
        //    }
        //    catch (Exception ex)
        //    {
        //        ModelState.AddModelError("", ex.Message); 
        //        return Partial("_TagForm", tag);
        //    }
        //}

        //[HttpPost]
        //public async Task<IActionResult> OnPostEditAsync(Tag tag)
        //{
        //    if (!IsAuthorized()) return Forbid();
        //    if (!ModelState.IsValid)
        //    {
        //        return Partial("_TagForm", tag);
        //    }
        //    try
        //    {
        //        var result =  _tagService.Update(tag);
        //        if (result.Success)
        //        {
        //            return new JsonResult(new { success = true });
        //        }
        //        else
        //        {
        //            ModelState.AddModelError("", result.Message); 
        //            return Partial("_TagForm", tag);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ModelState.AddModelError("", $"Error updating tag: {ex.Message}");
        //        return Partial("_TagForm", tag);
        //    }
        //}


        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (!IsStaff)
            {
                return Unauthorized();
            }
            try
            {
                var tag = _tagService.Get(id);
                await _hubContext.Clients.All.SendAsync("TagDeleted",tag.TagId, tag.TagName);
                //await _hubContext.Clients.All.SendAsync("UpdateDashboardCounts");

                _tagService.Delete(id);

                TempData["ToastMessage"] = $"Tag '{tag.TagName}' was deleted by Admin.";
            }
            catch (InvalidOperationException ex) 
            {
                TempData["Error"] = ex.Message; 
            }
            catch (Exception) 
            {
                TempData["Error"] = "An error occurred while deleting the tag.";
            }

            return RedirectToPage();
        }
    }
}