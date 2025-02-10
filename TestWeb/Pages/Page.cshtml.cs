using LegendaryDependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestWeb.Pages
{
    public class PageModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        [Keyed("def")]
        protected virtual ILazyClass LazyClass { get; set; } = default!;
        public void OnGet()
        {
            Console.WriteLine(LazyClass.Name);
        }
    }
}
