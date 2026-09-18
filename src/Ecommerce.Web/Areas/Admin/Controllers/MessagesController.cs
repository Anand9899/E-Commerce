using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Admin Messages Controller:
    /// Store Manager / Admin ko customers dwara bheje gaye inquiries, complaints
    /// aur support requests ko dekhne, read karne aur delete karne ki suvidha deta hai.
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MessagesController : Controller
    {
        private readonly IContactService _contactService;

        public MessagesController(IContactService contactService)
        {
            _contactService = contactService;
        }

        /// <summary>
        /// Sabhi messages / inquiries ki table list dikhata hai (All ya Unread Only filter ke sath).
        /// </summary>
        public async Task<IActionResult> Index(bool? unreadOnly = null)
        {
            ViewBag.UnreadOnly = unreadOnly;
            ViewBag.UnreadCount = await _contactService.GetUnreadCountAsync();
            var messages = await _contactService.GetAllMessagesAsync(unreadOnly);
            return View(messages);
        }

        /// <summary>
        /// Selected message ki poori detail dikhata hai aur use automatically "Read" mark kar deta hai.
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var message = await _contactService.GetMessageByIdAsync(id, markAsRead: true);
            if (message == null)
            {
                TempData["ErrorMessage"] = "Message not found or has been deleted.";
                return RedirectToAction(nameof(Index));
            }

            return View(message);
        }

        /// <summary>
        /// Message ko delete (archive) karta hai.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _contactService.DeleteMessageAsync(id);
            TempData["SuccessMessage"] = "Inquiry message was deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
