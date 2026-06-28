using System;
using System.Net;
using System.Net.Http;
using log4net;
using Polly;
using Polly.Extensions.Http;

namespace HEAppE.RestUtils
{
    public static class ResiliencePolicies
    {
        private static readonly ILog Log = LogManager.GetLogger("ResiliencePolicies");

        public static readonly IAsyncPolicy<HttpResponseMessage> TransientRetryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Log.Warn($"Retry {retryCount} after {timespan.TotalSeconds}s: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                });

        public static readonly IAsyncPolicy<HttpResponseMessage> DefaultCircuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
                onBreak: (outcome, timespan, context) =>
                {
                    Log.Warn($"Circuit broken for {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                },
                onReset: (context) =>
                {
                    Log.Info("Circuit reset.");
                });
    }
}
