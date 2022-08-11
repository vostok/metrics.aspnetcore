using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Hosting;
using NUnit.Framework;
using Vostok.Commons.Testing;
using Vostok.Metrics.AspNetCore.Tests.Base;
using Vostok.Metrics.AspNetCore.Tls;

// ReSharper disable AccessToDisposedClosure

namespace Vostok.Metrics.AspNetCore.Tests;

[TestFixture]
internal class TlsConnectionsMonitor_Tests : TestsBase
{
    private int port;
    private IDisposable serverToken;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        port = GetPort();
        serverToken = StartServer(builder =>
            builder.ConfigureKestrel(options =>
                {
                    options.ConfigureHttpsDefaults(adapterOptions => adapterOptions.AllowAnyClientCertificate());
                    options.ListenLocalhost(port, listenOptions => listenOptions.UseHttps());
                }
            ));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        serverToken?.Dispose();
    }

    [Test]
    public async Task Should_measure_failed_tls_handshakes()
    {
        using var collector = new TlsConnectionsCollector();
        using var client = new TcpClient("localhost", port);

        await using var sslStream = new SslStream(client.GetStream());
        try
        {
            await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
                RemoteCertificateValidationCallback = (_, _, _, _) => false
            });
        }
        catch
        {
            // ignored
        }

        var assertion = () =>
        {
            var metrics = collector.Collect();
            metrics.FailedTlsHandshakes.Should().BePositive();
        };

        assertion.ShouldPassIn(10.Seconds());
    }

    [TestCaseSource(nameof(tlsVersions))]
    public async Task Should_measure_current_tls_connections(Func<TlsConnectionMetrics, long> valueProvider, SslProtocols protocols)
    {
        using var collector = new TlsConnectionsCollector();
        using var client = new TcpClient("localhost", port);

        await using var sslStream = new SslStream(client.GetStream());
        await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = "localhost",
            RemoteCertificateValidationCallback = (_, _, _, _) => true,
            EnabledSslProtocols = protocols
        });

        var assertion = () =>
        {
            var metrics = collector.Collect();
            valueProvider(metrics).Should().BePositive();
        };

        assertion.ShouldPassIn(10.Seconds());
    }

    private static object[] tlsVersions =
    {
        new object[] {new Func<TlsConnectionMetrics, long>(metrics => metrics.CurrentTls10Sessions), SslProtocols.Tls},
        new object[] {new Func<TlsConnectionMetrics, long>(metrics => metrics.CurrentTls11Sessions), SslProtocols.Tls11},
        new object[] {new Func<TlsConnectionMetrics, long>(metrics => metrics.CurrentTls12Sessions), SslProtocols.Tls12}
    };
}