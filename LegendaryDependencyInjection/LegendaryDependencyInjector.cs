﻿using System.Reflection.Emit;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace LegendaryDependencyInjection
{
    public class LegendaryDependencyInjector
    {
        public static Func<IServiceProvider?>? GetProviderFunc { get; set; }
        public static Func<IServiceCollection?>? GetInjectedServices { get; set; }

        private static bool IsEnumerable(Type type)
        {
            return type.IsGenericType && !type.IsGenericTypeDefinition && typeof(IEnumerable<>) == type.GetGenericTypeDefinition();
        }
        private static bool isInjectType(Type source, Type target)
        {
            if (source == target)
            {
                return true;
            }
            if (target.IsGenericType && !target.IsGenericTypeDefinition)
            {
                return target.GetGenericTypeDefinition() == source;
            }
            return false;
        }

        public bool HasInjected(Type type)
        {
            if (IsEnumerable(type))
            {
                type = type.GenericTypeArguments[0];
            }
            return _dic.ContainsKey(type)
                    || (GetInjectedServices?.Invoke()?.Any(s => isInjectType(s.ServiceType, type)) ?? false);
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
                List<ParameterInfo> list = parameters.Where(a => (a.ParameterType.IsClass || a.ParameterType.IsInterface) && HasInjected(a.ParameterType)).ToList();
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
            if (_dic.ContainsKey(type))
            {
                return Create(_dic[type]);
            }
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
                IEnumerable<PropertyInfo> props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty).Where(a => a.GetMethod!.IsVirtual && (a.PropertyType.IsClass || a.PropertyType.IsInterface) && HasInjected(a.PropertyType));
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
    public static class LegendaryDependencyInjectorExtensions
    {
        public static IMvcBuilder AddLegendaryDependencyInjector(this IMvcBuilder builder)
        {
            builder.Services.TryAddSingleton<LegendaryDependencyInjector>();
            ControllerFeature feature = new ControllerFeature();
            builder.PartManager.PopulateFeature(feature);

            foreach (Type controller in feature.Controllers.Select(c => c.AsType()))
            {
                builder.Services.AddTransient(controller, a => {
                    return a.GetRequiredService<LegendaryDependencyInjector>().GetService(controller);
                });
            }

            builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());

            LegendaryDependencyInjector.GetInjectedServices = () => builder.Services;

            return builder;
        }
        private static LegendaryDependencyInjector GetLegendaryDependencyInjector(IServiceProvider sp)
        {
            return sp.GetRequiredService<LegendaryDependencyInjector>();
        }
        private static object GetService(IServiceProvider sp, Type implementation)
        {
            return GetLegendaryDependencyInjector(sp).GetService(implementation);
        }
        public static void AddLazyScoped<TService, TImplementation>(this IServiceCollection serviecs) where TService : class where TImplementation : class, TService
        {
            AddLazyScoped(serviecs, typeof(TService), typeof(TImplementation));
        }
        public static void AddLazySingleton<TService, TImplementation>(this IServiceCollection serviecs) where TService : class where TImplementation : class, TService
        {
            AddLazySingleton(serviecs, typeof(TService), typeof(TImplementation));
        }
        public static void AddLazyTransient<TService, TImplementation>(this IServiceCollection serviecs) where TService : class where TImplementation : class, TService
        {
            AddLazyTransient(serviecs, typeof(TService), typeof(TImplementation));
        }
        public static void AddLazyScoped(this IServiceCollection serviecs, Type service, Type implementation)
        {
            serviecs.AddScoped(service, sp => GetService(sp, implementation));
        }
        public static void AddLazySingleton(this IServiceCollection serviecs, Type service, Type implementation)
        {
            serviecs.AddSingleton(service, sp => GetService(sp, implementation));
        }
        public static void AddLazyTransient(this IServiceCollection serviecs, Type service, Type implementation)
        {
            serviecs.AddTransient(service, sp => GetService(sp, implementation));
        }

        public static void AddLazyScoped<TImplementation>(this IServiceCollection serviecs) where TImplementation : class
        {
            AddLazyScoped(serviecs, typeof(TImplementation));
        }
        public static void AddLazySingleton<TImplementation>(this IServiceCollection serviecs) where TImplementation : class
        {
            AddLazySingleton(serviecs, typeof(TImplementation));
        }
        public static void AddLazyTransient<TImplementation>(this IServiceCollection serviecs) where TImplementation : class
        {
            AddLazyTransient(serviecs, typeof(TImplementation));
        }
        public static void AddLazyScoped(this IServiceCollection serviecs, Type implementation)
        {
            serviecs.AddScoped(implementation, sp => GetService(sp, implementation));
        }
        public static void AddLazySingleton(this IServiceCollection serviecs, Type implementation)
        {
            serviecs.AddSingleton(implementation, sp => GetService(sp, implementation));
        }
        public static void AddLazyTransient(this IServiceCollection serviecs, Type implementation)
        {
            serviecs.AddTransient(implementation, sp => GetService(sp, implementation));
        }
    }
}





//private static bool isInjectType(Type source, Type target)
//{
//    List<Type> typeList = new List<Type> { target };
//    while (typeList.Count > 0)
//    {
//        Type last = typeList[^1];
//        typeList.Remove(last);

//        if (source == last)
//        {
//            return true;
//        }
//        if (source.IsAssignableFrom(last))
//        {
//            return true;
//        }
//        if (last.BaseType != null)
//        {
//            typeList.Add(last.BaseType);
//        }
//        if (last.IsGenericType && !last.IsGenericTypeDefinition)
//        {
//            typeList.Add(last.GetGenericTypeDefinition());
//        }
//    }
//    return false;
//}