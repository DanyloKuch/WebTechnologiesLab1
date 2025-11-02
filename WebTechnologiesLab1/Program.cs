using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebTechnologiesLab1.Data;
using WebTechnologiesLab1.Models;
using WebTechnologiesLab1.Services;
using Azure.Identity;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<WebDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<WebDbContext>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    });

builder.Services.AddScoped<BlobStorageService>();
builder.Services.AddResponseCaching();
builder.Services.AddRazorPages();

string blobConnectionString = builder.Configuration.GetConnectionString("BlobStorageConnectionString");

if (!string.IsNullOrEmpty(blobConnectionString))
{
    string blobContainerName = "dataprotection-keys";

    builder.Services.AddDataProtection()
        .PersistKeysToAzureBlobStorage(blobConnectionString, blobContainerName, "keys.xml");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});


var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); 
app.UseRouting();

app.UseResponseCaching(); 

app.UseAuthentication(); 
app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();