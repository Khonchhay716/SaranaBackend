// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.IdentityModel.Tokens;
// using Microsoft.OpenApi.Models;
// using POS.Application.Common.Interfaces;
// using POS.Application.Common.Services;
// using POS.Infrastructure.Data;
// using POS.Infrastructure.Services;
// using POS.API.Authorization;
// using System.Text;
// using FluentValidation;
// using MediatR;
// using POS.Application.Features.SendMail;

// var builder = WebApplication.CreateBuilder(args);

// // Add services to the container
// builder.Services.AddControllers();
// builder.Services.AddEndpointsApiExplorer();

// // 👇 ADD THIS LINE - Register GmailService
// builder.Services.AddScoped<GmailService>();
// builder.Services.AddScoped<VerificationService>();

// // Configure Swagger with JWT Bearer Authentication
// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1.0", new OpenApiInfo
//     {
//         Title = "Library System",
//         Version = "1.0",
//         Description = "Library System API Documentation"
//     });
//     c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//     {
//         Description = "JWT Authorization header using the Bearer scheme. Just enter your token below (without 'Bearer' prefix).",
//         Name = "Authorization",
//         In = ParameterLocation.Header,
//         Type = SecuritySchemeType.Http,
//         Scheme = "bearer",
//         BearerFormat = "JWT"
//     });

//     c.AddSecurityRequirement(new OpenApiSecurityRequirement
//     {
//         {
//             new OpenApiSecurityScheme
//             {
//                 Reference = new OpenApiReference
//                 {
//                     Type = ReferenceType.SecurityScheme,
//                     Id = "Bearer"
//                 }
//             },
//             new string[] {}  // ⭐ Changed to array instead of List
//         }
//     });
// });

// // Database Context
// builder.Services.AddDbContext<MyAppDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// builder.Services.AddScoped<IMyAppDbContext>(provider =>
//     provider.GetRequiredService<MyAppDbContext>());

// // HttpContext Accessor
// builder.Services.AddHttpContextAccessor();

// // Memory Cache
// builder.Services.AddMemoryCache();

// // Services
// builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
// builder.Services.AddScoped<IJwtService, JwtService>();
// builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// // MediatR
// builder.Services.AddMediatR(cfg =>
// {
//     cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
//     cfg.RegisterServicesFromAssemblyContaining<IMyAppDbContext>();
// });

// // FluentValidation
// builder.Services.AddValidatorsFromAssemblyContaining<IMyAppDbContext>();

// // JWT Authentication
// var jwtSettings = builder.Configuration.GetSection("JwtSettings");
// var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = true,
//             ValidateAudience = true,
//             ValidateLifetime = true,
//             ValidateIssuerSigningKey = true,
//             ValidIssuer = jwtSettings["Issuer"],
//             ValidAudience = jwtSettings["Audience"],
//             IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
//             ClockSkew = TimeSpan.Zero
//         };

//         options.Events = new JwtBearerEvents
//         {
//             OnAuthenticationFailed = context =>
//             {
//                 if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
//                 {
//                     context.Response.Headers.Add("Token-Expired", "true");
//                 }
//                 return Task.CompletedTask;
//             }
//         };
//     });

// // ⭐ ADD AUTHORIZATION SERVICES - THIS WAS MISSING!
// builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
// builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// builder.Services.AddAuthorization(options =>
// {
//     options.FallbackPolicy = new AuthorizationPolicyBuilder()
//         .RequireAuthenticatedUser()
//         .Build();
// });

// // CORS
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowAll", policy =>
//     {
//         policy.AllowAnyOrigin()
//               .AllowAnyMethod()
//               .AllowAnyHeader();
//     });
// });

// var app = builder.Build();

// var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

// // Create the folder if it doesn't exist
// if (!Directory.Exists(uploadsPath))
// {
//     Directory.CreateDirectory(uploadsPath);
// }

// app.UseStaticFiles(new StaticFileOptions
// {
//     FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
//     RequestPath = "/Uploads"
// });

// // Configure the HTTP request pipeline
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI(c =>
//     {
//         c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "POS.API v1.0");
//         c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
//     });
// }

// app.UseHttpsRedirection();

// app.UseCors("AllowAll");
// app.UseAuthentication();
// app.UseAuthorization();

// app.MapControllers();

// app.Run();

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using POS.Application.Common.Interfaces;
using POS.Application.Common.Services;
using POS.Infrastructure.Data;
using POS.Infrastructure.Services;
using POS.API.Authorization;
using System.Text;
using FluentValidation;
using MediatR;
using POS.Application.Features.SendMail;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register Services
builder.Services.AddScoped<GmailService>();
builder.Services.AddScoped<VerificationService>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1.0", new OpenApiInfo
    {
        Title = "Library System",
        Version = "1.0",
        Description = "Library System API Documentation"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Just enter your token below (without 'Bearer' prefix).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Database Context
builder.Services.AddDbContext<MyAppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IMyAppDbContext>(provider =>
    provider.GetRequiredService<MyAppDbContext>());

// HttpContext Accessor
builder.Services.AddHttpContextAccessor();

// Memory Cache
builder.Services.AddMemoryCache();

// Services
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.RegisterServicesFromAssemblyContaining<IMyAppDbContext>();
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<IMyAppDbContext>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

// Authorization Services
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ============================================
// 🌱 AUTOMATIC DATABASE SEEDING
// ============================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MyAppDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        
        var seeder = new DatabaseSeeder(context, passwordHasher);
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
        
        Console.WriteLine("\n========================================");
        Console.WriteLine("❌ CRITICAL ERROR DURING SEEDING:");
        Console.WriteLine($"   {ex.Message}");
        Console.WriteLine("========================================\n");
        
        // Optionally: throw to prevent app from starting with incomplete database
        // throw;
    }
}
// ============================================

// Create Uploads folder
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
    Console.WriteLine($"✓ Created Uploads directory: {uploadsPath}");
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/Uploads"
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "POS.API v1.0");
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();