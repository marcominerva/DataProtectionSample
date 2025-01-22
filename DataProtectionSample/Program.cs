using DataProtectionSample;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddSqlServer<ApplicationDbContext>(builder.Configuration.GetConnectionString("SqlConnection"));

// Adds Data Protection API services.
builder.Services.AddDataProtection()
    .SetApplicationName("my-application")
//.PersistKeysToFileSystem(new DirectoryInfo("keys"))   // Keys are stored in %LOCALAPPDATA%\ASP.NET\DataProtection-Keys by default.
//.PersistKeysToDbContext<ApplicationDbContext>()       // Stores keys in a database.
;

// Creates a TimeLimitedDataProtector, that is able to protect data with a finite lifetime.
builder.Services.AddSingleton(services =>
{
    var dataProtectionProvider = services.GetRequiredService<IDataProtectionProvider>();
    var protector = dataProtectionProvider.CreateProtector("default").ToTimeLimitedDataProtector();
    return protector;
});

builder.Services.AddProblemDetails();

var app = builder.Build();
await ConfigureDatabaseAsync(app.Services);

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", app.Environment.ApplicationName);
});

app.MapPost("/api/protect", (Message message, ITimeLimitedDataProtector dataProtector) =>
{
    // The payload will be valid for 1 minute.
    var protectedString = dataProtector.Protect(message.Text, TimeSpan.FromMinutes(1));

    return TypedResults.Ok(new Message(protectedString));
})
.WithSummary("Protect the given text")
.WithDescription("The payload will be valid for 1 minute. After that, trying to unprotect it will result in a CryptographicException");

app.MapPost("/api/unprotect", (Message message, ITimeLimitedDataProtector dataProtector) =>
{
    // If the payload is invalid or the lifetime is expired, a CryptographicException will be thrown.
    var unprotectedString = dataProtector.Unprotect(message.Text);

    return TypedResults.Ok(new Message(unprotectedString));
})
.WithSummary("Unprotect the given text")
.WithDescription("If the payload is invalid or the lifetime is expired, a CryptographicException will be thrown");

app.Run();

static async Task ConfigureDatabaseAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();
}

public record class Message(string Text);