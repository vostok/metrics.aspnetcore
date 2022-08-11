using System;
using System.Diagnostics.Tracing;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Observable;
using Vostok.Metrics.AspNetCore.Helpers;

namespace Vostok.Metrics.AspNetCore.Tls;

/// <summary>
/// <para><see cref="TlsHandshakeMonitor"/>Emits notifications about every tls handshake in current process.</para>
/// <para>Should only be used on .NET Core 5.0+ due to unavailability of TLS events in earlier versions.</para>
/// <para>Subscribe to receive instances of <see cref="TlsHandshakeInfo"/>.</para>
/// </summary>
[PublicAPI]
public class TlsHandshakeMonitor : EventListener, IObservable<TlsHandshakeInfo>
{
    private const string SourceName = "System.Net.Security";

    // NOTE: We check certain events in System.Net.Security event counters.
    // NOTE: See https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Security/src/System/Net/Security/NetSecurityTelemetry.cs for details. 

    #region EventId

    private const int HandshakeStartEventId = 1;
    private const int HandshakeStopEventId = 2;
    private const int HandshakeFailedEventId = 3;

    private const int PayloadIndex = 0;

    #endregion

    // NOTE: Method call for the event is executed in the same synchronization context, so we can track the start and end of the tls handshake.
    private readonly AsyncLocal<HandshakeInfo> handshakeStartTime = new();

    private readonly BroadcastObservable<TlsHandshakeInfo> observable = new();

    public IDisposable Subscribe(IObserver<TlsHandshakeInfo> observer) =>
        observable.Subscribe(observer);

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == SourceName)
            EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (!observable.HasObservers)
            return;

        var eventId = eventData.EventId;

        switch (eventId)
        {
            case HandshakeStartEventId:
                handshakeStartTime.Value = new HandshakeInfo(DateTimeOffset.Now);
                break;
            case HandshakeStopEventId:
                OnStopHandShake(eventData);
                break;
            case HandshakeFailedEventId:
                OnFailedHandShake();
                break;
            default:
                return;
        }
    }

    private void OnStopHandShake(EventWrittenEventArgs eventData)
    {
        var handshakeStopTime = DateTimeOffset.Now;
        var handshakeInfo = handshakeStartTime.Value;

        if (handshakeInfo.StartTime == default || !EventHelper.TryGetEventValue(eventData, PayloadIndex, out var payload))
            return;

        var sslProtocol = Enum.TryParse<SslProtocols>(payload.ToString(), out var protocol) ? protocol : SslProtocols.None;

        var info = new TlsHandshakeInfo(sslProtocol, handshakeInfo.IsFailed, handshakeStopTime - handshakeInfo.StartTime);
        Task.Run(() => observable.Push(info));
    }

    private void OnFailedHandShake()
    {
        var handshakeInfo = handshakeStartTime.Value;
        if (handshakeInfo.StartTime == default)
            return;

        handshakeInfo.IsFailed = true;
        handshakeStartTime.Value = handshakeInfo;
    }
}

internal struct HandshakeInfo
{
    public readonly DateTimeOffset StartTime;
    public bool IsFailed;

    public HandshakeInfo(DateTimeOffset startTime)
    {
        StartTime = startTime;
        IsFailed = false;
    }
}