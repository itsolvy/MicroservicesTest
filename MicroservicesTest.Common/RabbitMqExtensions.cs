using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using System.Diagnostics;

namespace MicroservicesTest.Common
{
    public static class RabbitMqExtensions
    {
        public async static Task<IConnection> CreateConnectionAsyncRetry(this IConnectionFactory factory, CancellationToken cancellationToken = default(CancellationToken))
        {
            var retryOptions = new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<RabbitMQ.Client.Exceptions.BrokerUnreachableException>(),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(2),
                UseJitter = true,
                OnRetry = static args =>
                {
                    Debug.WriteLine($"Попытка {args.AttemptNumber + 1} не удалась. " +
                                      $"Ожидание: {args.RetryDelay.TotalSeconds:F2} сек. " +
                                      $"Ошибка: {args.Outcome.Exception?.Message}");
                    return ValueTask.CompletedTask;
                }
            };
            ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
                .AddRetry(retryOptions)
                .Build();

            return await pipeline.ExecuteAsync(async token => await factory.CreateConnectionAsync(token), cancellationToken);
        }
    }
}
