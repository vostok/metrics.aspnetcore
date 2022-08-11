using System;
using System.Security.Authentication;
using JetBrains.Annotations;

namespace Vostok.Metrics.AspNetCore.Tls;

/// <summary>
/// <para><see cref="TlsHandshakeInfo"/> Describes a single tls handshake that occurred in current process.</para>
/// </summary>
[PublicAPI]
public class TlsHandshakeInfo
{
    public TlsHandshakeInfo(SslProtocols protocol, bool isFailed, TimeSpan latency)
    {
        Protocol = protocol;
        IsFailed = isFailed;
        Duration = latency;
    }
    
    /// <summary>
    /// Tls protocol version.
    /// </summary>
    public SslProtocols Protocol { get; }  
        
    /// <summary>
    /// Tls handshake success.
    /// </summary>
    public bool IsFailed { get; }
        
    /// <summary>
    /// Duration of the tls handshake.
    /// </summary>
    public TimeSpan Duration { get; }
}