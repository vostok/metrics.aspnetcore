using System;
using Microsoft.AspNetCore.Hosting;

namespace Vostok.Metrics.AspNetCore.Tests.Base;

internal class ServerInfo : IDisposable
{
    public readonly Uri Endpoint;
    private readonly IWebHost webHost;

    public ServerInfo(IWebHost webHost, Uri endpoint)
    {
        this.webHost = webHost;
        Endpoint = endpoint;
    }

    public void Dispose()
    {
        webHost?.Dispose();
    }
}