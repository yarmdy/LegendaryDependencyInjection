using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Reflection;

namespace TestWeb.Controllers
{
    [Route("{controller=Home}/{action=Index}/{id?}")]
    public class HomeController : Controller
    {
        public virtual IEnumerable<ILazyClass> LazyClasses { get; set; } = default!;
        private IDelayFactory<ILazyClass> _delayClass { get; set; } = default!;

        public HomeController(IDelayFactory<ILazyClass> delayClass) {
            _delayClass = delayClass;
        }
        public IActionResult Index()
        {
            return Json(new { Code=1 ,Lazy= LazyClasses,Lazy2 = _delayClass.Service });
        }
        public override string? ToString()
        {
            return base.ToString();
        }
    }
}
