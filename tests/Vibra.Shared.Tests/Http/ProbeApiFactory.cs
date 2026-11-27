using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vibra.Shared.Api;

namespace Vibra.Shared.Tests.Http;

/// <summary>El host real en Development, donde es más fácil que se filtren trazas.</summary>
public sealed class ProbeApiFactory : WebApplicationFactory<Program>
{
    public const string ClaimHeader = "X-Test-Sub";
    public const string AnonymousHeader = "X-Test-Unauthenticated";

    public CapturingLoggerProvider Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Estas pruebas no usan base de datos.
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Jwt:SigningKey", "clave-solo-para-pruebas-0123456789abcdef");
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(ProbeController).Assembly);
            services.AddSingleton<IStartupFilter, TestIdentityFilter>();
            services.AddSingleton<IRequestValidator<ProbeBody>, ProbeBodyValidator>();
        });
    }

    // La autenticación real llega con Identity; aquí la identidad se simula con cabeceras.
    private sealed class TestIdentityFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, proceed) =>
            {
                if (context.Request.Headers.TryGetValue(ClaimHeader, out var subject))
                {
                    var authentication = context.Request.Headers.ContainsKey(AnonymousHeader) ? null : "test";
                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity([new Claim("sub", subject.ToString())], authentication));
                }

                return proceed(context);
            });
            next(app);
        };
    }
}

public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<LogEntry> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly ConcurrentQueue<LogEntry> _entries;

        public CapturingLogger(ConcurrentQueue<LogEntry> entries)
        {
            _entries = entries;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            _entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), exception));
    }
}
