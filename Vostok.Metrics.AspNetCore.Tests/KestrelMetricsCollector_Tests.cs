using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using NUnit.Framework;
using Vostok.Commons.Testing;
using Vostok.Metrics.AspNetCore.Kestrel;
using Vostok.Metrics.AspNetCore.Tests.Base;

// ReSharper disable AccessToDisposedClosure

namespace Vostok.Metrics.AspNetCore.Tests;

[TestFixture]
internal class KestrelMetricsCollector_Tests : TestsBase
{
    [Test]
    public void Should_measure_current_connections()
    {
        using var collector = new KestrelMetricsCollector();
        using var server = StartServer(SetupServer);
        using var client = GetTcpClient(server.Endpoint.Host, server.Endpoint.Port);

        var assertion = () =>
        {
            var metrics = collector.Collect();
            metrics.CurrentConnections.Should().BePositive();
        };

        assertion.ShouldPassIn(10.Seconds());
    }

    [Test]
    public async Task Should_measure_websocket_connections()
    {
        using var collector = new KestrelMetricsCollector();
        using var server = StartServer(
            SetupServer,
            builder =>
            {
                builder.UseWebSockets();
            },
            async context =>
            {
                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                await Task.Delay(10.Seconds());
            }
        );
        using var client = new ClientWebSocket();

        await client.ConnectAsync(new Uri($"ws://{server.Endpoint.Host}:{server.Endpoint.Port}"), CancellationToken.None);

        var assertion = () =>
        {
            var metrics = collector.Collect();
            metrics.CurrentWebsocketRequests.Should().BePositive();
        };

        assertion.ShouldPassIn(10.Seconds());
    }

    private static void SetupServer(WebHostBuilder builder)
    {
        builder.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0);
        });
    }
}