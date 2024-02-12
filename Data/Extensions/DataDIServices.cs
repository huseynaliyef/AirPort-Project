using Data.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Extensions
{
    public static class DataDIServices
    {
        public static void DbInject(this IServiceCollection services, string ConnectionString)
        {
            services.AddDbContext<AirPortDbContext>((options) =>
            {
                options.UseNpgsql(ConnectionString);
            });
        }
    }
}
