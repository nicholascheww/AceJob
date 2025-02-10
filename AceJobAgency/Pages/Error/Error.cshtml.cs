using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AceJobAgency.Pages.Error
{
    public class IndexModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int StatusCode { get; set; }

        public void OnGet(int statusCode)
        {
            StatusCode = statusCode;
        }
    }
}

