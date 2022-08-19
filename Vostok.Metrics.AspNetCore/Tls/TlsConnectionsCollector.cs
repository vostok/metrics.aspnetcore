using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using JetBrains.Annotations;
using Vostok.Commons.Environment;
using Vostok.Metrics.AspNetCore.Helpers;
using Vostok.Metrics.System.Helpers;

namespace Vostok.Metrics.AspNetCore.Tls;

/// <summary>
/// <para><see cref="TlsConnectionsCollector"/> collects and returns <see cref="TlsConnectionMetrics"/>.</para>
/// <para>It is designed to be invoked periodically.</para>
/// </summary>
[PublicAPI]
public class TlsConnectionsCollector : EventListener
{
    private const string SourceName = "System.Net.Security";

    // NOTE: We check certain events in System.Net.Security event counters.
    // NOTE: See https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Security/src/System/Net/Security/NetSecurityTelemetry.cs for details. 

    #region EventId

    private const int CounterEventId = -1;

    private const string Tls10OpenedSessionsCounterName = "tls10-sessions-open";
    private const string Tls11OpenedSessionsCounterName = "tls11-sessions-open";
    private const string Tls12OpenedSessionsCounterName = "tls12-sessions-open";
    private const string Tls13OpenedSessionsCounterName = "tls13-sessions-open";

    private const string AllOpenedTlsSessionsCounterName = "all-tls-sessions-open";
    private const string FailedTlsSessionsCounterName = "failed-tls-handshakes";

    #endregion

    private readonly DeltaCollector failedTlsSessionsCounter;

    private long tls10OpenedSessions;
    private long tls11OpenedSessions;
    private long tls12OpenedSessions;
    private long tls13OpenedSessions;

    private long allOpenedTlsSessions;
    private long failedTlsSessions;

    public TlsConnectionsCollector()
    {
        failedTlsSessionsCounter = new DeltaCollector(() => failedTlsSessions);
    }

    [NotNull]
    public TlsConnectionMetrics Collect()
    {
        var metrics = new TlsConnectionMetrics();

        if (!RuntimeDetector.IsDotNet50AndNewer)
            return metrics;

        CollectCurrentSessions(metrics);

        return metrics;
    }

    private void CollectCurrentSessions(TlsConnectionMetrics metrics)
    {
        metrics.CurrentTls10Sessions = tls10OpenedSessions;
        metrics.CurrentTls11Sessions = tls11OpenedSessions;
        metrics.CurrentTls12Sessions = tls12OpenedSessions;
        metrics.CurrentTls13Sessions = tls13OpenedSessions;

        metrics.CurrentTlsSessions = allOpenedTlsSessions;
        metrics.FailedTlsHandshakes = failedTlsSessionsCounter.Collect();
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == SourceName)
            EnableEvents(eventSource,
                EventLevel.Verbose,
                EventKeywords.All,
                new Dictionary<string, string>
                {
                    {"EventCounterIntervalSec", "5"}
                });
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData.EventId != CounterEventId)
            return;

        CollectCounterValue(eventData);
    }

    private void CollectCounterValue(EventWrittenEventArgs eventData)
    {
        if (EventHelper.TryGetCounterValue(eventData, Tls10OpenedSessionsCounterName, out var value))
            Interlocked.Exchange(ref tls10OpenedSessions, value);
        if (EventHelper.TryGetCounterValue(eventData, Tls11OpenedSessionsCounterName, out value))
            Interlocked.Exchange(ref tls11OpenedSessions, value);
        if (EventHelper.TryGetCounterValue(eventData, Tls12OpenedSessionsCounterName, out value))
            Interlocked.Exchange(ref tls12OpenedSessions, value);
        if (EventHelper.TryGetCounterValue(eventData, Tls13OpenedSessionsCounterName, out value))
            Interlocked.Exchange(ref tls13OpenedSessions, value);

        if (EventHelper.TryGetCounterValue(eventData, AllOpenedTlsSessionsCounterName, out value))
            Interlocked.Exchange(ref allOpenedTlsSessions, value);
        if (EventHelper.TryGetCounterValue(eventData, FailedTlsSessionsCounterName, out value))
            Interlocked.Exchange(ref failedTlsSessions, value);
    }
}