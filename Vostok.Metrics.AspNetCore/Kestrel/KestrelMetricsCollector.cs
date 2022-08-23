using System;
using JetBrains.Annotations;

namespace Vostok.Metrics.AspNetCore.Kestrel;

/// <summary>
/// <para><see cref="KestrelMetricsCollector"/> collects and returns <see cref="KestrelMetrics"/>.</para>
/// <para>It is designed to be invoked periodically.</para>
/// </summary>
[PublicAPI]
public class KestrelMetricsCollector : IDisposable
{
    private readonly KestrelConnectionsMonitor connectionsMonitor = new();

    [NotNull]
    public KestrelMetrics Collect()
    {
        var metrics = new KestrelMetrics();

        CollectConnectionsMetrics(metrics);

        return metrics;
    }

    public void Dispose()
    {
        connectionsMonitor?.Dispose();
    }

    private void CollectConnectionsMetrics(KestrelMetrics metrics)
    {
        connectionsMonitor.Collect(metrics);
    }
}