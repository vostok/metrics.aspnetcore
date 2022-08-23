using System.Net;
using System.Net.Security;
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
    private ServerInfo server;

    private string host;
    private int port;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        server = StartServer(builder =>
            builder.ConfigureKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.UseHttps());
                }
            ));

        host = server.Endpoint.Host;
        port = server.Endpoint.Port;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        server?.Dispose();
    }

    [Test]
    public async Task Should_measure_failed_tls_handshakes()
    {
        using var collector = new TlsConnectionsCollector();
        using var client = GetTcpClient(host, port);

        await using var sslStream = new SslStream(client.GetStream());
        try
        {
            await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = host,
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

    [Test]
    public async Task Should_measure_current_tls_connections()
    {
        using var collector = new TlsConnectionsCollector();
        using var client = GetTcpClient(host, port);

        await using var sslStream = new SslStream(client.GetStream());
        await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = host,
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        });

        var assertion = () =>
        {
            var metrics = collector.Collect();
            metrics.CurrentTlsSessions.Should().BePositive();

            // NOTE: We don't want to specify the tls version exactly as some test agents may not support certain versions.
            (metrics.CurrentTls10Sessions + metrics.CurrentTls11Sessions + metrics.CurrentTls12Sessions + metrics.CurrentTls13Sessions)
                .Should()
                .BePositive();
        };

        assertion.ShouldPassIn(10.Seconds());
    }
}