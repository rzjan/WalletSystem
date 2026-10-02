using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Infrastructure.Persistence;
using WalletSystem.Infrastructure.Persistence.Outbox;
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

        services.AddDbContext<WalletDbContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("WalletDb"))
                   .AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<WalletDbContext>());
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IBetRepository, BetRepository>();

        // Quedan pendientes: IPaymentProviderClient (con Polly), IWalletReadService / IBetReadService (Dapper)

        return services;
    }
}
