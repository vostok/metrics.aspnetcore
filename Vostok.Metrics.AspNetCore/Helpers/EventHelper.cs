using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Vostok.Metrics.AspNetCore.Helpers;

internal static class EventHelper
{
    public static bool TryGetCounterValue(EventWrittenEventArgs eventData, string counterName, out long value)
    {
        value = 0;
        if (eventData.Payload?.Count <= 0
            || !(eventData.Payload?[0] is IDictionary<string, object> data)
            || !data.TryGetValue("Name", out var n)
            || !(n is string name)
            || name != counterName) return false;

        if (!data.TryGetValue("Mean", out var mean))
            return false;
        value = Convert.ToInt64(mean);
        return true;
    }

    public static bool TryGetEventValue(EventWrittenEventArgs eventData, int payloadIndex, out object payload)
    {
        payload = default;
        
        if (eventData.Payload == null
            || eventData.Payload.Count <= payloadIndex)
            return false;

        payload = eventData.Payload[payloadIndex];
        return true;
    }
}