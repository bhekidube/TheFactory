using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using Azure.Core;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var builder = WebApplication.CreateBuilder(args);
var azureCredential = CreateAzureCredential(builder.Environment.IsDevelopment());

// Use builder.Configuration directly for config access
var configuration = builder.Configuration;

// Add Key Vault
builder.Host.ConfigureAppConfiguration((context, config) =>
{
    var builtConfig = config.Build();
    var keyVaultUrl = builtConfig["KeyVaultUrl"];
    var loadKeyVaultInDevelopment = builtConfig.GetValue<bool>("LoadKeyVaultInDevelopment");
    var shouldLoadKeyVault = !context.HostingEnvironment.IsDevelopment() || loadKeyVaultInDevelopment;

    if (!string.IsNullOrEmpty(keyVaultUrl) && shouldLoadKeyVault)
    {
        config.AddAzureKeyVault(new Uri(keyVaultUrl), azureCredential);
    }
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<TokenCredential>(azureCredential);
builder.Services.AddScoped<SqlConnectionFactory>();
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    options.UseSqlServer(serviceProvider.GetRequiredService<SqlConnectionFactory>().CreateConnection()));
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

static TokenCredential CreateAzureCredential(bool isDevelopment)
{
    var options = new DefaultAzureCredentialOptions();

    if (OperatingSystem.IsMacOS())
    {
        options.ExcludeVisualStudioCredential = true;
        options.ExcludeVisualStudioCodeCredential = true;
    }

    if (isDevelopment)
    {
        // Local dev should not depend on broken CLI/VS Code token providers.
        return new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
        {
            RedirectUri = new Uri("http://localhost")
        });
    }

    return new DefaultAzureCredential(options);
}


