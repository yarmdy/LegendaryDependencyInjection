using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace LegendaryDependencyInjection
{
    public interface IServiceProviderAccessor
    {
        public IServiceProvider Provider { get; }
    }
}