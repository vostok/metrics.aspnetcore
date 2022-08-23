using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

namespace Vostok.Metrics.AspNetCore.Tests;

[TestFixture]
internal class TlsHandshakeMonitor_Tests : TestsBase, IObserver<TlsHandshakeInfo>
{
    private List<TlsHandshakeInfo> handshakes;

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

    [SetUp]
    public void Setup()
    {
        handshakes = new List<TlsHandshakeInfo>();
    }

    [Test]
    public async Task Should_measure_handshakes()
    {
        using var monitor = new TlsHandshakeMonitor();
        using var client = GetTcpClient(host, port);

        monitor.Subscribe(this);
        await using var sslStream = new SslStream(client.GetStream());
        await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = host,
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        });

        Action assertion = () => handshakes.Should().NotBeEmpty();

        assertion.ShouldPassIn(5.Seconds());
    }

    [Test]
    public async Task Should_measure_failed_handshakes()
    {
        using var monitor = new TlsHandshakeMonitor();
        using var client = GetTcpClient(host, port);

        monitor.Subscribe(this);
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

        Action assertion = () => handshakes.Should().NotBeEmpty();

        assertion.ShouldPassIn(5.Seconds());
        handshakes.Any(handshake => handshake.IsFailed).Should().BeTrue();
    }

    [Test]
    public async Task Should_measure_handshakes_duration()
    {
        using var monitor = new TlsHandshakeMonitor();
        using var client = GetTcpClient(host, port);

        monitor.Subscribe(this);
        await using var sslStream = new SslStream(client.GetStream());
        var watch = Stopwatch.StartNew();
        await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = host,
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        });
        watch.Stop();

        Action assertion = () => handshakes.Should().NotBeEmpty();

        assertion.ShouldPassIn(5.Seconds());

        var duration = handshakes.First().Duration;
        Math.Abs(watch.ElapsedMilliseconds - duration.TotalMilliseconds).Should().BeLessThan(50);
    }

    public void OnNext(TlsHandshakeInfo value) =>
        handshakes.Add(value);

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }
}