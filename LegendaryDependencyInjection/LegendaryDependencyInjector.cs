﻿using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace LegendaryDependencyInjection
{
    /// <summary>
    /// 传奇级依赖注入器
    /// </summary>
    public class LegendaryDependencyInjector
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public LegendaryDependencyInjector(IServiceProviderIsService serviceProviderIsService, IServiceProviderIsKeyedService serviceProviderIsKeyedService)
        {
            //赋值
            ServiceProviderIsService = serviceProviderIsService;
            ServiceProviderIsKeyedService = serviceProviderIsKeyedService;
        }
        /// <summary>
        /// 判断是否可以获取服务的判断器
        /// </summary>
        public IServiceProviderIsService ServiceProviderIsService { get; set; } = default!;
        public IServiceProviderIsKeyedService ServiceProviderIsKeyedService { get; set; } = default!;
        /// <summary>
        /// 获取是否被注入过
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool HasInjected(Type type)
        {
            return ServiceProviderIsService.IsService(type);
        }

        public bool HasKeyedInjected(Type type,object? key)
        {
            return ServiceProviderIsKeyedService.IsKeyedService(type, key);
        }
        /// <summary>
        /// 从提供者获取服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T? GetServiceInProvider<T>(IServiceProvider sp) where T : class
        {
            return (T?)sp.GetService(typeof(T));
        }
        public static T? GetKeyedServiceInProvider<T>(IServiceProvider sp, object? key) where T : class
        {
            return sp.GetKeyedService<T>(key);
        }

        private static MethodInfo _getServiceMethod = typeof(LegendaryDependencyInjector).GetMethod(nameof(GetServiceInProvider), BindingFlags.Static | BindingFlags.Public)!;
        private static MethodInfo _getKeyedServiceMethod = typeof(LegendaryDependencyInjector).GetMethod(nameof(GetKeyedServiceInProvider), BindingFlags.Static | BindingFlags.Public)!;

        public static MethodInfo _getTypeMethod = typeof(object).GetMethod(nameof(GetType), BindingFlags.Instance | BindingFlags.Public)!;
        public static MethodInfo _getPropertyMethod = typeof(Type).GetMethod(nameof(Type.GetProperty), BindingFlags.Instance | BindingFlags.Public, new []{ typeof(string), typeof(BindingFlags) })!;
        private static MethodInfo _getCustomAttribute = typeof(CustomAttributeExtensions).GetMethod(nameof(CustomAttributeExtensions.GetCustomAttribute), BindingFlags.Public | BindingFlags.Static, new[] { typeof(MemberInfo) })!.MakeGenericMethod(typeof(KeyedAttribute))!;
        private static MethodInfo _getKeyMethod = typeof(KeyedAttribute).GetProperty(nameof(KeyedAttribute.Key), BindingFlags.Public | BindingFlags.Instance)!.GetGetMethod()!;

        /// <summary>
        /// 新建程序集模块
        /// </summary>
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
        /// <summary>
        /// 缓存程序集模块
        /// </summary>
        private static ModuleBuilder? _cacheModuleBuilder;
        /// <summary>
        /// 线程锁，保证线程安全
        /// </summary>
        private static readonly object _lockModule = new object();
        /// <summary>
        /// 获取模块，始终返回一个模块，不会新建
        /// </summary>
        private static ModuleBuilder _module
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
        private object Create(IServiceProvider sp, Type type)
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
            var obj = ActivatorUtilities.CreateInstance(sp, type);
            var properties = obj.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty|BindingFlags.GetProperty).Where(a=>!a.GetMethod!.IsVirtual).Select(a=>new {prop = a,injAttr = a.GetCustomAttribute<InjAttribute>() }).Where(a=>a.injAttr!=null && ((a.injAttr is not KeyedAttribute) && HasInjected(a.prop.PropertyType) || (a.injAttr is KeyedAttribute keyed) && HasKeyedInjected(a.prop.PropertyType, keyed.Key) ));

            foreach (var propinfo in properties)
            {
                var prop = propinfo.prop;
                if (prop.GetValue(obj) != null)
                {
                    continue;
                }
                if (propinfo.injAttr is not KeyedAttribute keyed)
                {
                    prop.SetValue(obj, sp.GetService(prop.PropertyType));
                    continue;
                }
                object? keyedService;
                if(sp is IKeyedServiceProvider keyedSp)
                {
                    keyedService = keyedSp.GetKeyedService(prop.PropertyType, keyed.Key);
                }
                else
                {
                    keyedService = sp.GetKeyedServices(prop.PropertyType, keyed.Key).LastOrDefault();
                }
                prop.SetValue(obj, keyedService);
            }
            return obj;
        }
        /// <summary>
        /// 类型映射缓存
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Type> _dic = new ConcurrentDictionary<Type, Type>();
        /// <summary>
        /// 获取目标类型的代理类型依赖
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetService<T>(IServiceProvider sp) where T : class
        {
            return (T)GetService(sp,typeof(T));
        }
        /// <summary>
        /// 获取目标类型的代理类型依赖，不存在就创建代理类
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public object GetService(IServiceProvider sp,Type type,object? keyed=null)
        {
            var resType = _dic.GetOrAdd(type, getType);
            return Create(sp,resType);
        }
        private Type getType(Type type)
        {
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
                return type;
            }
            //获取目标类型所有依赖和虚属性
            IEnumerable<(PropertyInfo prop, KeyedAttribute? keyed)> props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.GetProperty)
                .Where(a => (a.PropertyType.IsClass || a.PropertyType.IsInterface) && a.GetMethod!.IsVirtual)
                .Select(a => new { prop = a, keyed = a.GetCustomAttribute<KeyedAttribute>() })
                .Where(a=>a.keyed==null && HasInjected(a.prop.PropertyType) || a.keyed!=null && HasKeyedInjected(a.prop.PropertyType,a.keyed.Key)).Select(a=>(a.prop, a.keyed));
            //如果不存在，依然不需要代理，直接创建自身
            if (props.Count() <= 0)
            {
                return type;
            }
            //定义一个代理类型建造器
            TypeBuilder builder = _module.DefineType($"{type.Name}_Lazy_{Guid.NewGuid()}", TypeAttributes.Public, type);

            var fieldProvider =  builder.DefineField("legendaryServiceProvider",typeof(IServiceProvider),FieldAttributes.Private|FieldAttributes.SpecialName);

            //循环所有属性，把属性getter方法改造为如果属性为空，就自动注入依赖，并把依赖赋值给属性
            foreach (var propinfo in props)
            {
                var prop = propinfo.prop;
                MethodBuilder methodBuilder = builder.DefineMethod($"get_{prop.Name}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual, prop.PropertyType, Type.EmptyTypes);
                ILGenerator il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, prop.GetGetMethod(true)!);
                il.Emit(OpCodes.Dup);
                Label over = il.DefineLabel();
                il.Emit(OpCodes.Brtrue, over);
                il.Emit(OpCodes.Pop);
                var keyed = propinfo.keyed;
                if (keyed == null)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, fieldProvider);
                    il.Emit(OpCodes.Call, _getServiceMethod.MakeGenericMethod(prop.PropertyType));
                }
                else
                {
                    //var writeLine = typeof(Console).GetMethod("WriteLine", BindingFlags.Static | BindingFlags.Public, [typeof(string)])!;
                    //writeLine.Invoke(null, ["生成了writeline"]);
                    //il.Emit(OpCodes.Ldstr, "准备调用获取属性");
                    //il.Emit(OpCodes.Call, writeLine);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, fieldProvider);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, _getTypeMethod);
                    il.Emit(OpCodes.Ldstr, prop.Name);
                    il.Emit(OpCodes.Ldc_I4, (int)(BindingFlags.Instance | BindingFlags.Public));
                    il.Emit(OpCodes.Call, _getPropertyMethod);
                    il.Emit(OpCodes.Call, _getCustomAttribute);
                    il.Emit(OpCodes.Call, _getKeyMethod);
                    il.Emit(OpCodes.Call, _getKeyedServiceMethod.MakeGenericMethod(prop.PropertyType));
                }
                il.DeclareLocal(prop.PropertyType);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Call, prop.GetSetMethod(true)!);
                il.Emit(OpCodes.Ldloc_0);

                il.MarkLabel(over);
                il.Emit(OpCodes.Ret);
                PropertyBuilder propertyBuilder = builder.DefineProperty(prop.Name, prop.Attributes, prop.PropertyType, Type.EmptyTypes);
                propertyBuilder.SetGetMethod(methodBuilder);
            }
            //获取所有构造函数
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            //重写所有构造函数，模仿目标类型
            foreach (var constructor in constructors)
            {
                Type[] types = constructor.GetParameters().Select(a => a.ParameterType).Concat(new[] { typeof(IServiceProvider) }).ToArray();
                ConstructorBuilder constructorBuilder = builder.DefineConstructor(constructor.Attributes, constructor.CallingConvention, types);
                ILGenerator il = constructorBuilder.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg,types.Length);
                il.Emit(OpCodes.Stfld, fieldProvider);

                il.Emit(OpCodes.Ldarg_0);
                int index = 0;
                types.Take(types.Length-1).ToList().ForEach(t => {
                    il.Emit(OpCodes.Ldarg, ++index);
                });
                il.Emit(OpCodes.Call, constructor);
                il.Emit(OpCodes.Ret);
            }

            //生成新类型为代理类
            Type resultType = builder.CreateType()!;
            //使用新类型创建对象
            return resultType;
        }
    }
}