using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Metrics.Models;
using Vostok.Metrics.Primitives.Gauge;

namespace Vostok.Metrics.AspNetCore.Kestrel;

[PublicAPI]
public static class KestrelMetricsCollectorExtensions_Metrics
{
    /// <summary>
    /// <para>Enables reporting of Kestrel counters metrics of the current process.</para>
    /// <para>Note that provided <see cref="IMetricContext"/> should contain tags sufficient to decouple these metrics from others.</para>
    /// <para>Dispose of the returned <see cref="IDisposable"/> object to stop reporting metrics.</para>
    /// </summary>
    public static IDisposable ReportMetrics([NotNull] this KestrelMetricsCollector collector, [NotNull] IMetricContext metricContext, TimeSpan? period = null)
        => metricContext.CreateMultiFuncGauge(() => ProvideMetrics(collector), new FuncGaugeConfig {ScrapePeriod = period}) as IDisposable;

    private static IEnumerable<MetricDataPoint> ProvideMetrics(KestrelMetricsCollector collector)
    {
        var metrics = collector.Collect();

        foreach (var property in typeof(KestrelMetrics).GetProperties())
        {
            var value = property.GetValue(metrics);

            if (value != null)
                yield return new MetricDataPoint(Convert.ToDouble(value), (WellKnownTagKeys.Name, property.Name));
        }
    }
}