using JetBrains.Annotations;

namespace Vostok.Metrics.AspNetCore.Kestrel;

/// <summary>
/// <para>Set of metrics associated with the Kestrel server.</para>
/// </summary>
[PublicAPI]
public class KestrelMetrics
{
    /// <summary>
    /// <para>The current number of active connections to the Kestrel server.</para>
    /// <para>This metric is collected only for net5.0 and newer, zero for other platforms.</para>
    /// </summary>
    public long CurrentConnections { get; set; }

    /// <summary>
    /// <para>The current number of WebSockets requests.</para>
    /// <para>This metric is collected only for net5.0 and newer, zero for other platforms.</para>
    /// </summary>
    public long CurrentWebsocketRequests { get; set; }
}