using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrackMyScore.Database;
using TrackMyScore.Services;

var builder = WebApplication.CreateBuilder(args);

// connection string for the database taken from settings json
string connectionString = builder.Configuration.GetConnectionString("DbConnection");

// lowercase urls
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// adding validation services
builder.Services.AddScoped<FollowerService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<CreateAccountService>();
builder.Services.AddScoped<UserService>();


builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();

// adding support for sessions
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// adding the database model
builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString)
);

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseSession();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();


app.Run();
