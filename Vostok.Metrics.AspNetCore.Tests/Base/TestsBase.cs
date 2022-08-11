using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Vostok.Metrics.AspNetCore.Tests.Base;

internal class TestsBase
{
    private static int port = 11000;

    protected static int GetPort() =>
        Interlocked.Increment(ref port);

    protected static IDisposable StartServer(Action<WebHostBuilder> hostSetup, Action<IApplicationBuilder> applicationSetup = null, RequestDelegate requestDelegate = default)
    {
        var builder = new WebHostBuilder();

        var startup = new ServerStartup(applicationSetup, requestDelegate);

        builder
            .ConfigureServices(collection =>
            {
                collection.AddSingleton(startup);
            })
            .UseKestrel()
            .UseStartup<ServerStartup>();
        hostSetup?.Invoke(builder);

        var host = builder.Build();

        var cts = new CancellationTokenSource();

        Task.Run(() => host.RunAsync(cts.Token), cts.Token);

        return cts;
    }
}