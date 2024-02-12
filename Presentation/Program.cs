using Data.DAL;
using Data.Extensions;
using Business.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.DbInject(builder.Configuration.GetConnectionString("Postgresql"));

builder.Services.BusinessInject();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AirPortDbContext>();
            context.Database.EnsureCreated(); 
        }
    }
}



app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
