using DataProtectionSample;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/protect", (Message message, ITimeLimitedDataProtector dataProtector) =>
{
    // The payload will be valid for 1 minutes.
    var protectedString = dataProtector.Protect(message.Text, TimeSpan.FromMinutes(1));

    return TypedResults.Ok(new Message(protectedString));
});

app.MapPost("/api/unprotect", (Message message, ITimeLimitedDataProtector dataProtector) =>
{
    // If the payload is invalid or the lifetime is expired, a CryptographicException will be thrown.
    var unprotectedString = dataProtector.Unprotect(message.Text);

    return TypedResults.Ok(new Message(unprotectedString));
});

app.Run();

public record class Message(string Text);