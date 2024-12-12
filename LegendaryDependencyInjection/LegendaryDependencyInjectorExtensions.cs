using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LegendaryDependencyInjection
{
    /// <summary>
    /// 传奇级依赖注入扩展
    /// </summary>
    public static class LegendaryDependencyInjectorExtensions
    {
        public static IServiceCollection AddLegendaryDependencyInjector(this IServiceCollection services) 
        {
            services.TryAddSingleton<LegendaryDependencyInjector>();
            return services;
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
            return GetLegendaryDependencyInjector(sp).GetService(sp,implementation);
        }
        /// <summary>
        /// 注入延迟生命周期类型
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddLazyScoped<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService
        {
            AddLazyScoped(services, typeof(TService), typeof(TImplementation));
        }
        /// <summary>
        /// 注入延迟单例类型
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddLazySingleton<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService
        {
            AddLazySingleton(services, typeof(TService), typeof(TImplementation));
        }
        /// <summary>
        /// 注入延迟临时类型
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddLazyTransient<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService
        {
            AddLazyTransient(services, typeof(TService), typeof(TImplementation));
        }
        /// <summary>
        /// 注入延迟生命周期类型
        /// </summary>
        /// <param name="services"></param>
        /// <param name="service"></param>
        /// <param name="implementation"></param>
        public static void AddLazyScoped(this IServiceCollection services, Type service, Type implementation)
        {
            services.AddScoped(service, sp => GetService(sp, implementation));
        }
        /// <summary>
        /// 注入延迟单例类型
        /// </summary>
        /// <param name="services"></param>
        /// <param name="service"></param>
        /// <param name="implementation"></param>
        public static void AddLazySingleton(this IServiceCollection services, Type service, Type implementation)
        {
            services.AddSingleton(service, sp => GetService(sp, implementation));
        }
        /// <summary>
        /// 注入延迟临时类型
        /// </summary>
        /// <param name="services"></param>
        /// <param name="service"></param>
        /// <param name="implementation"></param>
        public static void AddLazyTransient(this IServiceCollection services, Type service, Type implementation)
        {
            services.AddTransient(service, sp => GetService(sp, implementation));
        }
        /// <summary>
        /// 注入延迟生命周期类型
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddLazyScoped<TImplementation>(this IServiceCollection services) where TImplementation : class
        {
            AddLazyScoped(services, typeof(TImplementation));
        }
        /// <summary>
        /// 注入延迟单例类型
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddLazySingleton<TImplementation>(this IServiceCollection services) where TImplementation : class
        {
            AddLazySingleton(services, typeof(TImplementation));
        }
        /// <summary>
        /// 注入延迟临时类型
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddLazyTransient<TImplementation>(this IServiceCollection services) where TImplementation : class
        {
            AddLazyTransient(services, typeof(TImplementation));
        }
        /// <summary>
        /// 注入延迟生命周期类型
        /// </summary>
        /// <param name="services"></param>
        /// <param name="implementation"></param>
        public static void AddLazyScoped(this IServiceCollection services, Type implementation)
        {
            services.AddScoped(implementation, sp => GetService(sp, implementation));
        }
        /// <summary>
        /// 注入延迟单例类型
        /// </summary>
        /// <param name="services"></param>
        /// <param name="implementation"></param>
        public static void AddLazySingleton(this IServiceCollection services, Type implementation)
        {
            services.AddSingleton(implementation, sp => GetService(sp, implementation));
        }
        /// <summary>
        /// 注入延迟临时类型
        /// </summary>
        /// <param name="services"></param>
        /// <param name="implementation"></param>
        public static void AddLazyTransient(this IServiceCollection services, Type implementation)
        {
            services.AddTransient(implementation, sp => GetService(sp, implementation));
        }

        /// <summary>
        /// 注入延迟生命周期类型
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddLazyKeyedScoped<TService, TImplementation>(this IServiceCollection services,object? key) where TService : class where TImplementation : class, TService
        {
            AddLazyKeyedScoped(services, typeof(TService), typeof(TImplementation),key);
        }
        /// <summary>
        /// 注入延迟单例类型
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddLazyKeyedSingleton<TService, TImplementation>(this IServiceCollection services, object? key) where TService : class where TImplementation : class, TService
        {
            AddLazyKeyedSingleton(services, typeof(TService), typeof(TImplementation), key);
        }
        /// <summary>
        /// 注入延迟临时类型
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddLazyKeyedTransient<TService, TImplementation>(this IServiceCollection services, object? key) where TService : class where TImplementation : class, TService
        {
            AddLazyKeyedTransient(services, typeof(TService), typeof(TImplementation), key);
        }
        /// <summary>
        /// 注入延迟生命周期类型
        /// </summary>
        /// <param name="services"></param>
        /// <param name="service"></param>
        /// <param name="implementation"></param>
        public static void AddLazyKeyedScoped(this IServiceCollection services, Type service, Type implementation, object? key)
        {
            services.AddKeyedScoped(service, key, (sp,k) => GetService(sp, implementation));
        }
        /// <summary>
        /// 注入延迟单例类型
        /// </summary>
        /// <param name="services"></param>
        /// <param name="service"></param>
        /// <param name="implementation"></param>
        public static void AddLazyKeyedSingleton(this IServiceCollection services, Type service, Type implementation, object? key)
        {
            services.AddKeyedSingleton(service, key, (sp,k) => GetService(sp, implementation));
        }
        /// <summary>
        /// 注入延迟临时类型
        /// </summary>
        /// <param name="services"></param>
        /// <param name="service"></param>
        /// <param name="implementation"></param>
        public static void AddLazyKeyedTransient(this IServiceCollection services, Type service, Type implementation, object? key)
        {
            services.AddKeyedTransient(service, key, (sp,k) => GetService(sp, implementation));
        }
        /// <summary>
        /// 注入延迟生命周期类型
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddLazyKeyedScoped<TImplementation>(this IServiceCollection services, object? key) where TImplementation : class
        {
            AddLazyKeyedScoped(services, typeof(TImplementation), key);
        }
        /// <summary>
        /// 注入延迟单例类型
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddLazyKeyedSingleton<TImplementation>(this IServiceCollection services, object? key) where TImplementation : class
        {
            AddLazyKeyedSingleton(services, typeof(TImplementation), key);
        }
        /// <summary>
        /// 注入延迟临时类型
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddLazyKeyedTransient<TImplementation>(this IServiceCollection services, object? key) where TImplementation : class
        {
            AddLazyKeyedTransient(services, typeof(TImplementation), key);
        }
        /// <summary>
        /// 注入延迟生命周期类型
        /// </summary>
        /// <param name="services"></param>
        /// <param name="implementation"></param>
        public static void AddLazyKeyedScoped(this IServiceCollection services, Type implementation, object? key)
        {
            services.AddKeyedScoped(implementation, key, (sp,k) => GetService(sp, implementation));
        }
        /// <summary>
        /// 注入延迟单例类型
        /// </summary>
        /// <param name="services"></param>
        /// <param name="implementation"></param>
        public static void AddLazyKeyedSingleton(this IServiceCollection services, Type implementation, object? key)
        {
            services.AddKeyedSingleton(implementation, key, (sp,key) => GetService(sp, implementation));
        }
        /// <summary>
        /// 注入延迟临时类型
        /// </summary>
        /// <param name="services"></param>
        /// <param name="implementation"></param>
        public static void AddLazyKeyedTransient(this IServiceCollection services, Type implementation, object? key)
        {
            services.AddKeyedTransient(implementation, key, (sp,k) => GetService(sp, implementation));
        }
    }
}