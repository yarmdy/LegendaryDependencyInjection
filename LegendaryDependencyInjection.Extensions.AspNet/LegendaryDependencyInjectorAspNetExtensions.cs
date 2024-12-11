using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LegendaryDependencyInjection.Extensions.AspNet
{
    public static class LegendaryDependencyInjectorAspNetExtensions
    {
        /// <summary>
        /// 扩展mvc，使controller构造器改为服务构造器，并把controller的构造方式改为创建代理，继承它本身，然后实例化
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMvcBuilder AddLegendaryDependencyInjector(this IMvcBuilder builder)
        {
            builder.Services.TryAdd(ServiceDescriptor.Singleton<IHttpContextAccessor, HttpContextAccessor>());
            builder.Services.AddLegendaryDependencyInjector<HttpContextServiceProviderAccessor>();
            ControllerFeature feature = new ControllerFeature();
            builder.PartManager.PopulateFeature(feature);

            foreach (Type controller in feature.Controllers.Select(c => c.AsType()))
            {
                builder.Services.AddTransient(controller, a => GetService(a, controller));
            }

            builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());
            return builder;
        }
        /// <summary>
        /// 获取传奇级依赖注入
        /// </summary>
        /// <param name="sp"></param>
        /// <returns></returns>
        private static LegendaryDependencyInjector GetLegendaryDependencyInjector(IServiceProvider sp)
        {
            return sp.GetRequiredService<LegendaryDependencyInjector>();
        }
        /// <summary>
        /// 获服务
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="implementation"></param>
        /// <returns></returns>
        private static object GetService(IServiceProvider sp, Type implementation)
        {
            return GetLegendaryDependencyInjector(sp).GetService(implementation);
        }
    }
}
