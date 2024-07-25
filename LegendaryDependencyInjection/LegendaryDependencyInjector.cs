using System.Reflection.Emit;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Http;

namespace LegendaryDependencyInjection
{
    /// <summary>
    /// 传奇级依赖注入器
    /// </summary>
    public class LegendaryDependencyInjector
    {
        /// <summary>
        /// 单例 方便静态获取IHttpContextAccessor对象，以调取GetService方法
        /// </summary>
        private static LegendaryDependencyInjector _instance = default!;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpContextAccessor">依赖，用于获取服务提供者</param>
        /// <param name="serviceProviderIsService">依赖，用于判断是否可以获取服务</param>
        public LegendaryDependencyInjector(IHttpContextAccessor httpContextAccessor, IServiceProviderIsService serviceProviderIsService)
        {
            //赋值
            _instance = this;
            HttpContextAccessor = httpContextAccessor;
            ServiceProviderIsService = serviceProviderIsService;
        }
        /// <summary>
        /// http上下文访问器
        /// </summary>
        public IHttpContextAccessor? HttpContextAccessor { get; set; }
        /// <summary>
        /// 判断是否可以获取服务的判断器
        /// </summary>
        public IServiceProviderIsService ServiceProviderIsService { get; set; } = default!;
        /// <summary>
        /// 获取服务提供者委托，就是我通过这个方法获取IServiceProvider
        /// </summary>
        private static Func<IServiceProvider?>? GetProviderFunc = () => _instance?.HttpContextAccessor?.HttpContext?.RequestServices;
        /// <summary>
        /// 获取是否被注入过
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool HasInjected(Type type)
        {
            return ServiceProviderIsService.IsService(type);
        }
        /// <summary>
        /// 从提供者获取服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T? GetServiceInProvider<T>() where T : class
        {
            return (T?)GetServiceInProvider(typeof(T));
        }
        /// <summary>
        /// 从提供者获取服务
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static object? GetServiceInProvider(Type type)
        {
            return GetProviderFunc?.Invoke()?.GetService(type);
        }
        /// <summary>
        /// 新建程序集模块
        /// </summary>
        private ModuleBuilder _newModuleBuilder
        {
            get
            {
                AssemblyName assemblyName = new AssemblyName("LegendaryDependencyInjection.Assembly");
                AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                ModuleBuilder module = assembly.DefineDynamicModule("LegendaryDependencyInjection.Module");
                return module;
            }
        }
        /// <summary>
        /// 缓存程序集模块
        /// </summary>
        private ModuleBuilder? _cacheModuleBuilder;
        /// <summary>
        /// 线程锁，保证线程安全
        /// </summary>
        private readonly object _lockModule = new object();
        /// <summary>
        /// 获取模块，始终返回一个模块，不会新建
        /// </summary>
        private ModuleBuilder _module
        {
            get
            {
                //这里采用双判断，既要保证线程安全，又要提高效率，不能一直锁着
                if (_cacheModuleBuilder == null)
                {
                    lock (_lockModule)
                    {
                        if (_cacheModuleBuilder == null)
                        {
                            _cacheModuleBuilder = _newModuleBuilder;
                        }
                    }
                }
                return _cacheModuleBuilder;
            }
        }

        /// <summary>
        /// 依赖注入生成新对象的方法，注意这里是生成新对象，不包括判断对象是否存在，仅仅是在需要生成新对象的时候使用
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private object Create(Type type)
        {
            //获取所有构造函数
            //ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            ////如果没有构造函数，也就是说有默认的0参数构造函数，那就直接通过反射创建对象
            //if (constructors.Length <= 0)
            //{
            //    return Activator.CreateInstance(type)!;
            //}
            ////循环所有构造函数，找到参数个数和类型跟现有注入的依赖匹配的，然后再通过反射创建对象，参数试用获取的依赖填充
            //foreach (ConstructorInfo constructor in constructors.OrderByDescending(a => a.GetParameters().Length))
            //{
            //    ParameterInfo[] parameters = constructor.GetParameters();
            //    List<ParameterInfo> list = parameters.Where(a => (a.ParameterType.IsClass || a.ParameterType.IsInterface) && HasInjected(a.ParameterType)).ToList();
            //    if (list.Count != parameters.Length)
            //    {
            //        continue;
            //    }
            //    object?[]? args = list.Select(a => GetServiceInProvider(a.ParameterType)).Where(a => a != null).ToArray();
            //    if (args.Length != parameters.Length)
            //    {
            //        continue;
            //    }
            //    return Activator.CreateInstance(type, args)!;
            //}
            //throw new NotImplementedException();
            return ActivatorUtilities.CreateInstance(HttpContextAccessor?.HttpContext?.RequestServices!, type);
        }
        /// <summary>
        /// 类型映射缓存
        /// </summary>
        private readonly Dictionary<Type, Type> _dic = new Dictionary<Type, Type>();
        /// <summary>
        /// 获取目标类型的代理类型依赖
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetService<T>() where T : class
        {
            return (T)GetService(typeof(T));
        }
        /// <summary>
        /// 获取目标类型的代理类型依赖，不存在就创建代理类
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public object GetService(Type type)
        {
            //如果存在代理类，直接创建代理类的实例
            if (_dic.ContainsKey(type))
            {
                return Create(_dic[type]);
            }
            //锁住保证线程安全
            lock (_dic)
            {
                //这里是双判断，提高效率
                if (_dic.ContainsKey(type))
                {
                    return Create(_dic[type]);
                }
                //如果目标类型是接口，就报错
                if (type.IsInterface)
                {
                    throw new InvalidOperationException();
                }
                //如果目标类型是抽象的，就报错
                if (type.IsAbstract)
                {
                    throw new ArgumentException();
                }
                //如果目标类型是封闭的，那我就把它自己当作代理类，当然也就没有了代理功能
                if (type.IsSealed)
                {
                    _dic[type] = type;
                    return Create(_dic[type]);
                }
                //获取目标类型所有依赖和虚属性
                IEnumerable<PropertyInfo> props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty).Where(a => a.GetMethod!.IsVirtual && (a.PropertyType.IsClass || a.PropertyType.IsInterface) && HasInjected(a.PropertyType));
                //如果不存在，依然不需要代理，直接创建自身
                if (props.Count() <= 0)
                {
                    _dic[type] = type;
                    return Create(_dic[type]);
                }
                //定义一个代理类型建造器
                TypeBuilder builder = _module.DefineType($"{type.Name}_Lazy_{Guid.NewGuid()}", TypeAttributes.Public, type);
                //循环所有属性，把属性getter方法改造为如果属性为空，就自动注入依赖，并把依赖赋值给属性
                props.AsParallel().ForAll(prop => {
                    MethodBuilder methodBuilder = builder.DefineMethod($"get_{prop.Name}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual, prop.PropertyType, Type.EmptyTypes);
                    ILGenerator il = methodBuilder.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, type.GetMethod($"get_{prop.Name}", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!);
                    il.Emit(OpCodes.Dup);
                    Label over = il.DefineLabel();
                    il.Emit(OpCodes.Brtrue, over);
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Call, GetType().GetMethod("GetServiceInProvider", BindingFlags.Static | BindingFlags.Public)!.MakeGenericMethod(prop.PropertyType));
                    LocalBuilder propV = il.DeclareLocal(prop.PropertyType);
                    il.Emit(OpCodes.Stloc_0, propV);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Call, type.GetMethod($"set_{prop.Name}", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!);
                    il.Emit(OpCodes.Ldloc_0);

                    il.MarkLabel(over);
                    il.Emit(OpCodes.Ret);
                    PropertyBuilder propertyBuilder = builder.DefineProperty(prop.Name, prop.Attributes, prop.PropertyType, Type.EmptyTypes);
                    propertyBuilder.SetGetMethod(methodBuilder);
                });
                //获取所有构造函数
                ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                //重写所有构造函数，模仿目标类型
                constructors.AsParallel().ForAll(constructor => {
                    Type[] types = constructor.GetParameters().Select(a => a.ParameterType).ToArray();
                    ConstructorBuilder constructorBuilder = builder.DefineConstructor(constructor.Attributes, constructor.CallingConvention, types);
                    ILGenerator il = constructorBuilder.GetILGenerator();

                    il.Emit(OpCodes.Ldarg_0);
                    int index = 0;
                    types.ToList().ForEach(t => {
                        il.Emit(OpCodes.Ldarg, ++index);
                    });
                    il.Emit(OpCodes.Call, constructor);
                    il.Emit(OpCodes.Ret);
                });
                
                //生成新类型为代理类
                Type resultType = builder.CreateType();
                //使用新类型创建对象
                return Create(resultType);
            }
        }
    }
    /// <summary>
    /// 传奇级依赖注入扩展
    /// </summary>
    public static class LegendaryDependencyInjectorExtensions
    {
        /// <summary>
        /// 扩展mvc，使controller构造器改为服务构造器，并把controller的构造方式改为创建代理，继承它本身，然后实例化
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMvcBuilder AddLegendaryDependencyInjector(this IMvcBuilder builder)
        {
            builder.Services.TryAddSingleton<LegendaryDependencyInjector>();
            ControllerFeature feature = new ControllerFeature();
            builder.PartManager.PopulateFeature(feature);

            foreach (Type controller in feature.Controllers.Select(c => c.AsType()))
            {
                builder.Services.AddTransient(controller, a => GetService(a, controller));
            }

            builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());
            builder.Services.TryAdd(ServiceDescriptor.Singleton<IHttpContextAccessor,HttpContextAccessor>());

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
    }
}