using System;
using System.Collections.Generic;

namespace HEAppE.DomainObjects.Monitoring;

public class ExternalServiceLiveStatus
{
    public string ServiceName { get; set; }
    public string Type { get; set; }
    public string Protocol { get; set; }
    public string EndpointOrHost { get; set; }
    public int? Port { get; set; }
    public bool IsAvailable { get; set; }
    public long ResponseTimeMs { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime LastCheck { get; set; }
}

public class ExternalServiceStatistics
{
    public string ServiceName { get; set; }
    public string CommandOrPath { get; set; }
    public double AvailabilityPercentage { get; set; }
    public long AverageResponseTimeMs { get; set; }
    public long MinResponseTimeMs { get; set; }
    public long MaxResponseTimeMs { get; set; }
    public long TotalChecks { get; set; }
}

public class ExternalServicesReport
{
    public List<ExternalServiceLiveStatus> LiveStatus { get; set; } = new();
    public List<ExternalServiceStatistics> Statistics { get; set; } = new();
}
