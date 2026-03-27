using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpToolkit.Abstractions.Contracts;
using SpToolkit.Abstractions.Options;
using SpToolkit.Runtime.Execution;

namespace SpToolkit.Runtime.DependencyInjection;

/// <summary>
/// Extension methods for registering SpToolkit services in an IServiceCollection.
/// </summary>
public static class SpToolkitServiceCollectionExtensions
{
    /// <summary>
    /// Registers SpToolkit using a connection string supplied in <paramref name="configure"/>.
    /// The executor is scoped (one per request / unit of work).
    /// </summary>
    /// <example>
    /// services.AddSpToolkit(opts =>
    /// {
    ///     opts.ConnectionString = configuration.GetConnectionString("Default");
    /// });
    /// </example>
    public static IServiceCollection AddSpToolkit(
        this IServiceCollection services,
        Action<SpToolkitOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SpToolkitOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddScoped<IStoredProcedureExecutor>(sp =>
        {
            var opts   = sp.GetRequiredService<SpToolkitOptions>();
            var logger = sp.GetService<ILogger<StoredProcedureExecutor>>();
            return new StoredProcedureExecutor(opts, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers SpToolkit reusing the connection (and active transaction) from the
    /// EF Core <typeparamref name="TContext"/> that is already registered in the container.
    /// The executor is scoped to match the DbContext lifetime.
    /// </summary>
    /// <example>
    /// // No connection string needed -- SpToolkit borrows it from EF Core
    /// services.AddSpToolkit&lt;AppDbContext&gt;();
    ///
    /// // Optional extra configuration
    /// services.AddSpToolkit&lt;AppDbContext&gt;(opts => opts.EnableLogging = false);
    /// </example>
    public static IServiceCollection AddSpToolkit<TContext>(
        this IServiceCollection services,
        Action<SpToolkitOptions>? configure = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SpToolkitOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<IStoredProcedureExecutor>(sp =>
        {
            var opts      = sp.GetRequiredService<SpToolkitOptions>();
            var dbContext = sp.GetRequiredService<TContext>();
            var logger    = sp.GetService<ILogger<StoredProcedureExecutor>>();
            return new StoredProcedureExecutor(opts, dbContext, logger);
        });

        return services;
    }
}
