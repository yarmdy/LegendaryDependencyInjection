using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LegendaryDependencyInjection.Extensions.AspNet
{
    public class ServiceTagHelperActivator : ITagHelperActivator
    {
        public TTagHelper Create<TTagHelper>(ViewContext context) where TTagHelper : ITagHelper
        {
            return context.HttpContext.RequestServices.GetRequiredService<TTagHelper>();
        }
    }
}
