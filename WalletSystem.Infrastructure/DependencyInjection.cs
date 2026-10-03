using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Infrastructure.Persistence;
using WalletSystem.Infrastructure.Persistence.Outbox;
using WalletSystem.Infrastructure.Persistence.ReadServices;
using WalletSystem.Infrastructure.Persistence.Repositories;

namespace WalletSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Aquí puedes registrar servicios específicos de la infraestructura, como repositorios, servicios de mensajería, etc.       
        services.AddSingleton<OutboxSaveChangesInterceptor>();
        // Registrar el DbContext con la cadena de conexión y el interceptor de Outbox
        services.AddDbContext<WalletDbContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("WalletDb"))
                   .AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>()));
        // Registrar la fábrica de conexiones SQL para Dapper
        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
        // Registrar el UnitOfWork para que se pueda inyectar en los servicios
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<WalletDbContext>());
        //Repositorios
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IBetRepository, BetRepository>();
        //Servicios de lectura
        services.AddScoped<IWalletReadService, WalletReadService>();
        services.AddScoped<IBetReadService, BetReadService>();

        // Quedan pendientes: IPaymentProviderClient (con Polly), IWalletReadService / IBetReadService (Dapper)

        return services;
    }
}
