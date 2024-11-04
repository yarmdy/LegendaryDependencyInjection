using System.Reflection.Emit;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LegendaryDependencyInjection
{
    [AttributeUsage(AttributeTargets.Property)]
    public class KeyedAttribute : InjAttribute
    {
        /// <summary>
        /// Creates a new <see cref="FromKeyedServicesAttribute"/> instance.
        /// </summary>
        /// <param name="key">The key of the keyed service to bind to.</param>
        public KeyedAttribute(object key) => Key = key;

        /// <summary>
        /// The key of the keyed service to bind to.
        /// </summary>
        public object Key { get; }
    }
}