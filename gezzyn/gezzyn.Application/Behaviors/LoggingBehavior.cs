using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace gezzyn.Application.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            var name = typeof(TRequest).Name;
            _logger.LogInformation("→ {Name} başladı", name);

            var sw = Stopwatch.StartNew();
            var response = await next();
            sw.Stop();

            if (sw.ElapsedMilliseconds > 500)
                _logger.LogWarning("⚠ {Name} yavaş çalıştı: {Ms}ms", name, sw.ElapsedMilliseconds);
            else
                _logger.LogInformation("← {Name} tamamlandı: {Ms}ms", name, sw.ElapsedMilliseconds);

            return response;
        }
    }
}
