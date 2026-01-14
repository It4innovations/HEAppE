# Configuration keys

**BaseUrl**: address pointing to the Expirio API.

**TimeoutSeconds**: call timeout, in seconds.

**MaxRetries**: maximum number of times that a retry will be done.

**RetryInitialDelayMs**: delay, in milliseconds, before a retry.


# Dependency Injection registration

    Configuration.Bind("ExpirioSettings", new ExpirioSettings());

    services.AddHttpClient<IExpirioService, ExpirioService>(client =>
        {
            client.BaseAddress = new Uri(ExpirioSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(ExpirioSettings.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        // add Polly policies:
        .AddPolicyHandler(Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(ExpirioSettings.MaxRetries, _ => TimeSpan.FromMilliseconds(ExpirioSettings.RetryInitialDelayMs)))
        .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(
                                            handledEventsAllowedBeforeBreaking: 5, 
                                            durationOfBreak: TimeSpan.FromSeconds(ExpirioSettings.TimeoutSeconds)
                                            ));



# Usage



# Exceptions

**ExpirioBadRequestException**: The request was not properly built.

**ExpirioUnauthorizedException**: The request requires proper authentication.

**ExpirioNotFoundException**: The intended ticket was not found by the server.

**ExpirioServerException**: The server had an internal exception while processing the request.

**ExpirioUpstreamException**: Bad response upstream.

**ExpirioException**: Unidentified error.