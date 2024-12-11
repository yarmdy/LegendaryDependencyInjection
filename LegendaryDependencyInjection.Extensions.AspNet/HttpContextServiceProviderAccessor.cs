using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LegendaryDependencyInjection.Extensions.AspNet
{
    public class HttpContextServiceProviderAccessor : IServiceProviderAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public HttpContextServiceProviderAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public IServiceProvider Provider
        {
            get
            {
                var services = _httpContextAccessor.HttpContext?.RequestServices;
                if (services == null)
                {
                    throw new ArgumentNullException(nameof(_httpContextAccessor.HttpContext));
                }
                return services;
            }
        }
    }
}
