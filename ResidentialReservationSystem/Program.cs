using Infrastructure.Deta;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// کانکشن استرینگ مستقیم در برنامه
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer("server=.;database=ResidentialReservationSystem; Encrypt=false; Integrated Security=true"));

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
