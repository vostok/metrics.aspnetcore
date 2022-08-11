using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
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

namespace Vostok.Metrics.AspNetCore.Tests;

[TestFixture]
internal class TlsHandshakeMonitor_Tests : TestsBase, IObserver<TlsHandshakeInfo>
{
    private List<TlsHandshakeInfo> handshakes;
    private bool allowServerCertificate;

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

    [SetUp]
    public void Setup()
    {
        handshakes = new List<TlsHandshakeInfo>();
        allowServerCertificate = true;
    }

    [Test]
    public async Task Should_measure_handshakes()
    {
        using var monitor = new TlsHandshakeMonitor();
        using var client = GetClient();

        monitor.Subscribe(this);
        using var response = await client.GetAsync($"https://localhost:{port}");

        Action assertion = () => handshakes.Should().NotBeEmpty();

        assertion.ShouldPassIn(500.Milliseconds());
    }

    [Test]
    public async Task Should_measure_failed_handshakes()
    {
        allowServerCertificate = false;
        using var monitor = new TlsHandshakeMonitor();
        using var client = GetClient();

        monitor.Subscribe(this);
        try
        {
            using var response = await client.GetAsync($"https://localhost:{port}");
        }
        catch
        {
            // ignored
        }

        Action assertion = () => handshakes.Should().NotBeEmpty();

        assertion.ShouldPassIn(500.Milliseconds());
        handshakes.Any(handshake => handshake.IsFailed).Should().BeTrue();
    }

    [Test]
    public async Task Should_measure_handshakes_duration()
    {
        using var monitor = new TlsHandshakeMonitor();
        using var client = new TcpClient("localhost", port);

        monitor.Subscribe(this);
        await using var sslStream = new SslStream(client.GetStream());
        var watch = Stopwatch.StartNew();
        await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = "localhost",
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        });
        watch.Stop();

        Action assertion = () => handshakes.Should().NotBeEmpty();

        assertion.ShouldPassIn(500.Milliseconds());

        var duration = handshakes.First().Duration;
        Math.Abs(watch.ElapsedMilliseconds - duration.TotalMilliseconds).Should().BeLessThan(50);
    }

    private HttpClient GetClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => allowServerCertificate,
            SslProtocols = SslProtocols.Tls12
        };

        return new HttpClient(handler);
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