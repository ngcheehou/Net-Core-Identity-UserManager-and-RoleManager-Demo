using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp1.Constant;

namespace WebApp1.Pages.DailyVisit
{
    [Authorize(Policy = PagesNameConst.TopSecret)]
    public class TopSecretCEOOnlyModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("Passed2FA") != "true")
            {
                TempData["ReturnUrl"] = "/DailyVisit/TopSecretCEOOnly";
                return RedirectToPage("/Identity/VerifyWith2FA");
            }
            HttpContext.Session.Remove("Passed2FA");
           

            return Page();
        }

    }
}
