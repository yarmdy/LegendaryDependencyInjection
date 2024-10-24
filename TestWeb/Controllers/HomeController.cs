using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Reflection;
using LegendaryDependencyInjection;

namespace TestWeb.Controllers
{
    [Route("{controller=Home}/{action=Index}/{id?}")]
    public class HomeController : Controller
    {
        public virtual IEnumerable<ILazyClass> LazyClasses { get; set; } = default!;

        private ILazyClass? _defaultLazy { get; set; } = null!;
        [Keyed("def")]
        public virtual ILazyClass DefaultLazy {
            get
            {
                return _defaultLazy!;
            }
            set
            {
                _defaultLazy = value;
            }
        }
        [Keyed("fst")]
        public virtual ILazyClass FirstLazy { get; set; } = default!;


        private IDelayFactory<ILazyClass> _delayClass { get; set; } = default!;
        public virtual IServiceProvider Services { get; set; } = default!;

        public HomeController(IDelayFactory<ILazyClass> delayClass) 
        {
            _delayClass = delayClass;
        }
        public IActionResult Index()
        {
            //这里可以看到，延迟Factory模式不使用依然要创建Factory对象，而代理模式绝不会创建任何对象,只在使用的那一刻创建
            return Json(new { Code=1 ,Lazy= LazyClasses,Lazy2 = _delayClass.Service, DefaultLazy, FirstLazy });
        }
        public override string? ToString()
        {
            return "I'm HomeController Has Index Action!";
        }
        [NonAction]
        public void Test()
        {
            // 传统实例化
            TestInject test1 = new TestInject(_delayClass.Service, Services);
            // 反射
            TestInject test2 = (TestInject)Activator.CreateInstance(typeof(TestInject), _delayClass.Service, Services)!;

            // 依赖反射
            TestInject test3 = ActivatorUtilities.CreateInstance<TestInject>(Services);
            // 依赖反射提供一个参数
            TestInject test4 = ActivatorUtilities.CreateInstance<TestInject>(Services, _delayClass.Service);
            // 依赖反射提供二个参数
            TestInject test5 = ActivatorUtilities.CreateInstance<TestInject>(Services, _delayClass.Service, Services);
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
