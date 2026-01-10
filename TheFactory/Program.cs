using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Use builder.Configuration directly for config access
var configuration = builder.Configuration;

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
builder.Services.AddScoped<SqlConnectionService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "https://localhost:44411",// local dev
            "http://localhost:44411",           // local dev        
            "https://www.hambaonline.com",       // production frontend
            "https://hambaonline.com",           // (optional, non-www)
            "https://newdomain.com",             // new Angular app domain
            "http://newdomain.com",               // (optional, non-https)
            "http://localhost:4200" // <-- add this for local Angular dev
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

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
app.UseCors(); // <-- Move here
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Optionally remove Swagger from production if not needed

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();


