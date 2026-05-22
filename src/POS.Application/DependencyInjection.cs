using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Common.Behaviors;
using POS.Application.Features.SendMail;
using System.Reflection;

namespace POS.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Register MediatR
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            // Register FluentValidation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // Register Mapster
            services.AddMapster();

            // services.AddScoped<GmailService>();
            services.AddHttpClient<GmailService>();
            services.AddScoped<VerificationService>();

            return services;
        }

        private static IServiceCollection AddMapster(this IServiceCollection services)
        {
            // Configure Mapster mappings
            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);

            return services;
        }
    }
}