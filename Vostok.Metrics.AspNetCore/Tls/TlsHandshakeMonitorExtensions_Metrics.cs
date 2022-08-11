using System;
using JetBrains.Annotations;
using Vostok.Metrics.Primitives.Timer;

namespace Vostok.Metrics.AspNetCore.Tls;

[PublicAPI]
public static class TlsHandshakeMonitorExtensions_Metrics
{
    /// <summary>
    /// <para>Enables reporting summary of Tls handshake time.</para>
    /// <para>Note that provided <see cref="IMetricContext"/> should contain tags sufficient to decouple these metrics from others.</para>
    /// <para>Dispose of the returned <see cref="IDisposable"/> object to stop reporting metrics.</para>
    /// </summary>
    [NotNull]
    public static IDisposable ReportMetrics([NotNull] this TlsHandshakeMonitor monitor, [NotNull] IMetricContext metricContext, TimeSpan? period = null)
        => monitor.Subscribe(new ReportingObserver(metricContext, period));

    private class ReportingObserver : IObserver<TlsHandshakeInfo>
    {
        private readonly ITimer lookupLatency;

        public ReportingObserver(IMetricContext metricContext, TimeSpan? period)
        {
            lookupLatency = metricContext.CreateSummary("TlsHandshakeLatency", new SummaryConfig {Unit = WellKnownUnits.Milliseconds, ScrapePeriod = period});
        }

        public void OnNext(TlsHandshakeInfo value)
        {
            lookupLatency.Report(value.Duration.TotalMilliseconds);
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}