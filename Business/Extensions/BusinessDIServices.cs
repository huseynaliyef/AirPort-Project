using Business.Abstractions;
using Business.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Extensions
{
    public static class BusinessDIServices
    {
        public static void BusinessInject(this IServiceCollection services)
        {
            services.AddScoped<IPortRepository, PortRepository>();
        }
    }
}
