using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LegendaryDependencyInjection.Extensions.AspNet
{
    public class ServicePageModelActivatorProvider : IPageModelActivatorProvider
    {
        public Func<PageContext, object> CreateActivator(CompiledPageActionDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(descriptor);

            var modelTypeInfo = descriptor.ModelTypeInfo?.AsType();
            if (modelTypeInfo == null)
            {
                throw new ArgumentException();
            }
            return (p) => p.HttpContext.RequestServices.GetRequiredService(modelTypeInfo);
        }

        public Action<PageContext, object>? CreateReleaser(CompiledPageActionDescriptor descriptor)
        {
            return null;
        }
    }
}
