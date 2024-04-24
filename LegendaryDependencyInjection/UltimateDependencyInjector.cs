using System.Reflection.Emit;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace LegendaryDependencyInjection
{
    public class UltimateDependencyInjector
    {
        public static Func<IServiceProvider?>? GetProviderFunc { get; set; }
        public static Func<IServiceCollection?>? GetInjectedServices { get; set; }
        public bool HasInjected(Type type)
        {
            return _dic.ContainsKey(type)
                    || (GetInjectedServices?.Invoke()?.Any(s => s.ServiceType == type) ?? false);
        }
        public static T? GetServiceInProvider<T>() where T : class
        {
            return (T?)GetServiceInProvider(typeof(T));
        }
        private static object? GetServiceInProvider(Type type)
        {
            return GetProviderFunc?.Invoke()?.GetService(type);
        }
        private static ModuleBuilder _newModuleBuilder
        {
            get
            {
                AssemblyName assemblyName = new AssemblyName("LegendaryDependencyInjection.Assembly");
                AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                ModuleBuilder module = assembly.DefineDynamicModule("LegendaryDependencyInjection.Module");
                return module;
            }
        }
        private static ModuleBuilder? _cacheModuleBuilder;
        private static readonly object _lockModule = new object();
        private static ModuleBuilder _module
        {
            get
            {
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


        private readonly Dictionary<Type, Type> _dic = new Dictionary<Type, Type>();
        private object Create(Type type)
        {
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (constructors.Length <= 0)
            {
                return Activator.CreateInstance(type)!;
            }
            foreach (ConstructorInfo constructor in constructors.OrderByDescending(a => a.GetParameters().Length))
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                List<ParameterInfo> list = parameters.Where(a => a.ParameterType.IsClass && HasInjected(a.ParameterType)).ToList();
                if (list.Count != parameters.Length)
                {
                    continue;
                }
                object?[]? args = list.Select(a => GetServiceInProvider(a.ParameterType)).Where(a => a != null).ToArray();
                if (args.Length != parameters.Length)
                {
                    continue;
                }
                return Activator.CreateInstance(type, args)!;
            }
            throw new NotImplementedException();
        }
        public T GetService<T>() where T : class
        {
            return (T)GetService(typeof(T));
        }
        public object GetService(Type type)
        {
            lock (_dic)
            {
                if (_dic.ContainsKey(type))
                {
                    return Create(_dic[type]);
                }
                if (type.IsInterface)
                {
                    throw new InvalidOperationException();
                }
                if (type.IsAbstract)
                {
                    throw new ArgumentException();
                }
                if (type.IsSealed)
                {
                    _dic[type] = type;
                    return Create(_dic[type]);
                }
                IEnumerable<PropertyInfo> props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty).Where(a => a.GetMethod!.IsVirtual && a.PropertyType.IsClass && HasInjected(a.PropertyType));
                if (props.Count() <= 0)
                {
                    _dic[type] = type;
                    return Create(_dic[type]);
                }

                TypeBuilder builder = _module.DefineType($"{type.Name}_Lazy_{Guid.NewGuid()}", TypeAttributes.Public, type);
                foreach (PropertyInfo prop in props.AsParallel())
                {
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
                }
                ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (ConstructorInfo constructor in constructors.AsParallel())
                {
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
                }


                Type resultType = builder.CreateType();
                return Create(resultType);
            }
        }
    }
    public static class UltimateDependencyInjectorExtensions
    {
        public static IMvcBuilder AddUltimateDependencyInjector(this IMvcBuilder builder)
        {
            builder.Services.TryAddSingleton<UltimateDependencyInjector>();
            ControllerFeature feature = new ControllerFeature();
            builder.PartManager.PopulateFeature(feature);

            foreach (Type controller in feature.Controllers.Select(c => c.AsType()))
            {
                builder.Services.AddTransient(controller, a => {
                    return a.GetRequiredService<UltimateDependencyInjector>().GetService(controller);
                });
            }

            builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());

            UltimateDependencyInjector.GetInjectedServices = () => builder.Services;

            return builder;
        }
        private static UltimateDependencyInjector GetUltimateDependencyInjector(IServiceProvider sp)
        {
            return sp.GetRequiredService<UltimateDependencyInjector>();
        }
        private static TImplementation GetService<TImplementation>(IServiceProvider sp) where TImplementation : class
        {
            return GetUltimateDependencyInjector(sp).GetService<TImplementation>();
        }
        public static void AddLazyScoped<TService, TImplementation>(this IServiceCollection serviecs) where TService : class where TImplementation : class, TService
        {
            serviecs.AddScoped<TService, TImplementation>(GetService<TImplementation>);
        }
        public static void AddLazySingleton<TService, TImplementation>(this IServiceCollection serviecs) where TService : class where TImplementation : class, TService
        {
            serviecs.AddSingleton<TService, TImplementation>(GetService<TImplementation>);
        }
        public static void AddLazyTransient<TService, TImplementation>(this IServiceCollection serviecs) where TService : class where TImplementation : class, TService
        {
            serviecs.AddTransient<TService, TImplementation>(GetService<TImplementation>);
        }

        public static void AddLazyScoped<TImplementation>(this IServiceCollection serviecs) where TImplementation : class
        {
            serviecs.AddScoped(GetService<TImplementation>);
        }
        public static void AddLazySingleton<TImplementation>(this IServiceCollection serviecs) where TImplementation : class
        {
            serviecs.AddSingleton(GetService<TImplementation>);
        }
        public static void AddLazyTransient<TImplementation>(this IServiceCollection serviecs) where TImplementation : class
        {
            serviecs.AddTransient(GetService<TImplementation>);
        }
    }
}
