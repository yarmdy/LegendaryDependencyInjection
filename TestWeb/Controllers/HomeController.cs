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
        public virtual IServiceProvider Services { get; set; } = default!;

        public HomeController(IDelayFactory<ILazyClass> delayClass) {
            _delayClass = delayClass;
        }
        public IActionResult Index()
        {

            var test = ActivatorUtilities.CreateInstance<TestInject>(Services, new DefaultLazyClass { Name = "新的", Age = 100, Balance = -1000 });


            //这里可以看到，延迟Factory模式不使用依然要创建Factory对象，而代理模式绝不会创建任何对象,只在使用的那一刻创建
            return Json(new { Code=1 ,Lazy= LazyClasses,Lazy2 = _delayClass.Service,Test=test });
        }
        public override string? ToString()
        {
            return "I'm HomeController Has Index Action!";
        }
    }


    public class TestInject
    {
        public ILazyClass LazyClass { get; set; }
        public IServiceProvider Services { get; set; }
        public TestInject(ILazyClass lazyClass, IServiceProvider sp) {
            LazyClass = lazyClass;
            Services = sp;
        }
    }
}
