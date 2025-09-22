using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Add Key Vault
builder.Host.ConfigureAppConfiguration((context, config) =>
{
    var builtConfig = config.Build();
    var keyVaultUrl = builtConfig["KeyVaultUrl"];
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        config.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());
    }
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AzureSqlDb")));

// Use the PORT environment variable if set (Azure best practice)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();


