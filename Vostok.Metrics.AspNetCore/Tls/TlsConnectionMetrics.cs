using JetBrains.Annotations;

namespace Vostok.Metrics.AspNetCore.Tls;

/// <summary>
/// <para>Set of counters associated with the Tls.</para>
/// </summary>
[PublicAPI]
public class TlsConnectionMetrics
{
    /// <summary>
    /// <para>The number of active TLS sessions of any version.</para>
    /// <para>This metric is collected only for net5.0 and newer, zero for other platforms.</para>
    /// </summary>
    public long CurrentTlsSessions { get; set; }
    
    /// <summary>
    /// <para>The number of active TLS 1.0 sessions.</para>
    /// <para>This metric is collected only for net5.0 and newer, zero for other platforms.</para>
    /// </summary>
    public long CurrentTls10Sessions { get; set; }

    /// <summary>
    /// <para>The number of active TLS 1.1 sessions.</para>
    /// <para>This metric is collected only for net5.0 and newer, zero for other platforms.</para>
    /// </summary>
    public long CurrentTls11Sessions { get; set; }

    /// <summary>
    /// <para>The number of active TLS 1.2 sessions.</para>
    /// <para>This metric is collected only for net5.0 and newer, zero for other platforms.</para>
    /// </summary>
    public long CurrentTls12Sessions { get; set; }

    /// <summary>
    /// <para>The number of active TLS 1.3 sessions.</para>
    /// <para>This metric is collected only for net5.0 and newer, zero for other platforms.</para>
    /// </summary>
    public long CurrentTls13Sessions { get; set; }

    /// <summary>
    /// <para>The total number of failed TLS handshakes during the scrape period.</para>
    /// <para>This metric is collected only for net5.0 and newer, zero for other platforms.</para>
    /// <para>Note that this metric returns a diff (increment) between two consecutive observations: its value strongly depends on the observation period.</para>
    /// </summary>
    public long FailedTlsHandshakes { get; set; }
}