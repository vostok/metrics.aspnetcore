using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using Vostok.Commons.Environment;
using Vostok.Metrics.AspNetCore.Helpers;

namespace Vostok.Metrics.AspNetCore.Kestrel;

internal class KestrelConnectionsMonitor : EventListener
{
    private const string SourceName = "Microsoft-AspNetCore-Server-Kestrel";

    // NOTE: We check certain events in Microsoft-AspNetCore-Server-Kestrel event counters.
    // NOTE: See https://github.com/dotnet/aspnetcore/blob/main/src/Servers/Kestrel/Core/src/Internal/Infrastructure/KestrelEventSource.cs for details. 

    #region EventId

    private const int CounterEventId = -1;

    private const string CurrentConnectionCounterName = "current-connections";
    private const string CurrentWebsocketRequestsCounterName = "current-upgraded-requests";

    #endregion

    private long currentConnections;
    private long upgradedRequests;

    public void Collect(KestrelMetrics metrics)
    {
        if (!RuntimeDetector.IsDotNet50AndNewer)
            return;

        metrics.CurrentConnections = currentConnections;
        metrics.CurrentWebsocketRequests = upgradedRequests;
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
        if (EventHelper.TryGetCounterValue(eventData, CurrentConnectionCounterName, out var value))
            Interlocked.Exchange(ref currentConnections, value);

        if (EventHelper.TryGetCounterValue(eventData, CurrentWebsocketRequestsCounterName, out value))
            Interlocked.Exchange(ref upgradedRequests, value);
    }
}