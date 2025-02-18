using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace LegendaryDependencyInjection.Extensions.AspNet
{
    public static class LegendaryDependencyInjectorAspNetExtensions
    {
        /// <summary>
        /// 扩展mvc，使controller构造器改为服务构造器，并把controller的构造方式改为创建代理，继承它本身，然后实例化
        /// 并且把TagHelper构造器也改造了，还把PageModel同样改掉
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMvcBuilder AddLegendaryDependencyInjector(this IMvcBuilder builder)
        {
            builder.AddLegendaryDependencyInjectorControllers();
            builder.AddLegendaryDependencyInjectorTagHelpers();
            builder.AddLegendaryDependencyInjectorPageModels();
            builder.AddLegendaryDependencyInjectorViewComponents();
            return builder;
        }
        public static IMvcBuilder AddLegendaryDependencyInjectorControllers(this IMvcBuilder builder)
        {
            builder.Services.AddLegendaryDependencyInjector();
            ControllerFeature feature = new ControllerFeature();
            builder.PartManager.PopulateFeature(feature);

            foreach (Type controller in feature.Controllers.Select(c => c.AsType()))
            {
                builder.Services.AddLazyTransient(controller);
            }

            builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());
            return builder;
        }
        public static IMvcBuilder AddLegendaryDependencyInjectorTagHelpers(this IMvcBuilder builder)
        {
            builder.Services.AddLegendaryDependencyInjector();
            TagHelperFeature feature = new TagHelperFeature();
            builder.PartManager.PopulateFeature(feature);

            foreach (Type tagHelper in feature.TagHelpers)
            {
                builder.Services.AddLazyTransient(tagHelper);
            }

            builder.Services.Replace(ServiceDescriptor.Transient<ITagHelperActivator, ServiceTagHelperActivator>());
            return builder;
        }
        public static IMvcBuilder AddLegendaryDependencyInjectorPageModels(this IMvcBuilder builder)
        {
            builder.Services.AddLegendaryDependencyInjector();
            ViewsFeature feature = new ViewsFeature();
            builder.PartManager.PopulateFeature(feature);

            foreach (CompiledViewDescriptor view in feature.ViewDescriptors)
            {
                builder.Services.AddLazyTransient(view.Type!.GetProperty("Model")!.PropertyType);
            }

            builder.Services.Replace(ServiceDescriptor.Transient<IPageModelActivatorProvider, ServiceBasedPageModelActivatorProvider>());
            return builder;
        }
        public static IMvcBuilder AddLegendaryDependencyInjectorViewComponents(this IMvcBuilder builder)
        {
            builder.Services.AddLegendaryDependencyInjector();
            ViewComponentFeature feature = new ViewComponentFeature();
            builder.PartManager.PopulateFeature(feature);

            foreach (Type viewComponent in feature.ViewComponents.Select(a=>a.AsType()))
            {
                builder.Services.AddLazyTransient(viewComponent);
            }

            builder.Services.Replace(ServiceDescriptor.Transient<IViewComponentActivator, ServiceBasedViewComponentActivator>());
            return builder;
        }
    }
}
