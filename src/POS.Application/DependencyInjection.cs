using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Common.Behaviors;
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

            return services;
        }

        private static IServiceCollection AddMapster(this IServiceCollection services)
        {
            // Configure Mapster mappings
            TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);

            TypeAdapterConfig<Domain.Entities.Product, Features.Product.ProductInfo>
                .NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.SKU, src => src.SKU)
                .Map(dest => dest.Price, src => src.Price)
                .Map(dest => dest.CostPrice, src => src.CostPrice)
                .Map(dest => dest.StockQuantity, src => src.StockQuantity)
                .Map(dest => dest.MinStockLevel, src => src.MinStockLevel)
                .Map(dest => dest.Category, src => src.Category)
                .Map(dest => dest.Brand, src => src.Brand)
                .Map(dest => dest.Barcode, src => src.Barcode)
                .Map(dest => dest.IsActive, src => src.IsActive)
                .Map(dest => dest.IsDeleted, src => src.IsDeleted)
                .Map(dest => dest.CreatedDate, src => src.CreatedDate)
                .Map(dest => dest.UpdatedDate, src => src.UpdatedDate);

            return services;
        }
    }
}