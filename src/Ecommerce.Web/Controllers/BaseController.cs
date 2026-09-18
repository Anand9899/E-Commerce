using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers
{
    public abstract class BaseController : Controller
    {
        protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        protected string GetOrCreateGuestSessionId()
        {
            var session = HttpContext.Session;
            var sessionId = session.GetString("GuestSessionId");
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString("N");
                session.SetString("GuestSessionId", sessionId);
            }
            return sessionId;
        }
    }
}
