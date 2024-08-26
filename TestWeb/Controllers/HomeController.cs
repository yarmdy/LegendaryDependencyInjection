using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace TestWeb.Controllers
{
    [Route("{controller=Home}/{action=Index}/{id?}")]
    public class HomeController : Controller
    {
        public virtual IEnumerable<ILazyClass>? LazyClasses { get; set; }
        public IActionResult Index()
        {
            LazyClasses = null;
            return Json(new { Code=1 ,Lazy= LazyClasses });
        }
    }
}
