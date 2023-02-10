using Microsoft.Extensions.DependencyInjection;
using SecuGen.SecuSearchSDK3;
using WebAPI1toN;
using WebAPI1toN.Interfaces;
using WebAPI1toN.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Core SecuSearch library
builder.Services.AddSingleton<ISecuSearch3, SystemSecuSearch3>();
// Holds image data on search page across the top of the page + SessionID
builder.Services.AddSingleton<IImageContainer, ImageContainer>();
// Holds the sessionid, which is the key to load/saving WebSearch DB to disk.
builder.Services.AddSingleton<IDataPartitioning, DataPartitioning>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Pages/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Pages}/{action=Home}/{id?}");

app.Run();
