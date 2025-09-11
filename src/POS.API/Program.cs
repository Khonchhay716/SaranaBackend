using CoreAuthBackend.Client.Controllers.Extensions;
using CoreAuthBackend.Client.Controllers.Middleware;
using CoreAuthBackend.Client.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using POS.Application;
using POS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add controllers first

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCoreAuthFileUpload(builder.Configuration);
builder.Services.AddCoreAuthEmail(builder.Configuration);
// Register MyAppDbContext as the base DbContext for DI
builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<POS.Infrastructure.Data.Configurations.MyAppDbContext>());
builder.Services.AddAutoCrud();

// ✅ Add CoreAuth endpoints + client + middleware!
builder.Services.AddCoreAuthWithControllers(builder.Configuration, options =>
{
    options.EnableSwagger = true;
    options.EnableMiddleware = true;
    options.RoutePrefix = "api/auth";
    options.SwaggerTitle = "CoreAuth API";
    options.SwaggerDocName = "coreauth";
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost7777",
        policy =>
        {
            policy.WithOrigins("http://localhost:7777")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Configure Swagger to show ALL controllers in one document for now
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "POS Service with CoreAuth",
        Version = "v1",
        Description = "POS Service API with integrated CoreAuth endpoints"
    });
    
    // Include ALL controllers in the v1 document
    c.DocInclusionPredicate((docName, description) => docName == "v1");
});

var app = builder.Build();
// 1. Global Exception Handling (should be first)
app.UseMiddleware<ExceptionMiddleware>();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<POS.Infrastructure.Data.Configurations.MyAppDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        app.Logger.LogInformation("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while migrating the database");
        throw;
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "POS Service with CoreAuth");
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

app.UseCors("AllowLocalhost7777");
app.UseHttpsRedirection();

// CoreAuth middleware pipeline (includes authentication & authorization)
app.UseCoreAuthControllers();

app.MapControllers();

app.Run();