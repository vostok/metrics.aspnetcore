using System;
using JetBrains.Annotations;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Metrics.AspNetCore.Tls;

[PublicAPI]
public static class TlsHandshakeMonitorExtensions_Logging
{
    /// <summary>
    /// <para>Enables logging of all Tls handshakes matched by given <paramref name="filter"/> into provided <paramref name="log"/> with Info level.</para>
    /// <para>Dispose of the returned <see cref="IDisposable"/> object to stop the logging.</para>
    /// </summary>I
    [NotNull]
    public static IDisposable LogHandshakes([NotNull] this TlsHandshakeMonitor monitor, [NotNull] ILog log, [CanBeNull] Predicate<TlsHandshakeInfo> filter)
        => monitor.Subscribe(new LoggingObserver(log, filter));

    private class LoggingObserver : IObserver<TlsHandshakeInfo>
    {
        private readonly ILog log;
        private readonly Predicate<TlsHandshakeInfo> filter;

        public LoggingObserver(ILog log, Predicate<TlsHandshakeInfo> filter)
        {
            this.log = log.ForContext<TlsHandshakeMonitor>();
            this.filter = filter ?? (_ => true);
        }

        public void OnNext(TlsHandshakeInfo handshakeInfo)
        {
            if (!filter(handshakeInfo))
                return;

            log.Info(
                "Tls handshake occured with duration: {HandshakeDuration}. " +
                "SslProtocol: {Protocol}." +
                "Successfully: {Successfully}.",
                new
                {
                    HandshakeDuration = handshakeInfo.Duration.ToPrettyString(),
                    Protocol = handshakeInfo.Protocol.ToString(),
                    Successfully = !handshakeInfo.IsFailed
                });
        }

        public void OnError(Exception error)
            => log.Warn(error);

        public void OnCompleted()
        {
        }
    }
}