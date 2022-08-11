using System;
using System.Linq;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vostok.Commons.Local.Helpers;

namespace Vostok.Metrics.AspNetCore.Tests.Base;

internal class TestsBase
{
    protected static ServerInfo StartServer(Action<WebHostBuilder> hostSetup, Action<IApplicationBuilder> applicationSetup = null, RequestDelegate requestDelegate = default)
    {
        var builder = new WebHostBuilder();

        var startup = new ServerStartup(applicationSetup, requestDelegate);

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        builder
            .ConfigureServices(collection =>
            {
                collection.AddSingleton(startup);
            })
            .UseKestrel()
            .UseStartup<ServerStartup>();
        hostSetup?.Invoke(builder);

        var host = builder.Build();

        host.Start();
        var endpoint = new Uri(host.ServerFeatures.Get<IServerAddressesFeature>()!.Addresses.First());

        return new ServerInfo(host, endpoint);
    }

    protected static TcpClient GetTcpClient(string host, int port)
    {
        var client = null as TcpClient;

        Retrier.RetryOnException(() =>
            {
                client = new TcpClient();
                client.Connect(host, port);
            },
            3,
            "Can't connect to server",
            () => client?.Dispose());

        return client;
    }
}