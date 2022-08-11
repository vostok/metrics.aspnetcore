using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Vostok.Metrics.AspNetCore.Tests.Base;

internal class ServerStartup
{
    private readonly Action<IApplicationBuilder> setup;
    private readonly RequestDelegate requestDelegate;

    public ServerStartup(Action<IApplicationBuilder> setup, RequestDelegate requestDelegate)
    {
        this.setup = setup;
        this.requestDelegate = requestDelegate
                               ?? (context =>
                               {
                                   context.Response.StatusCode = 200;
                                   return Task.CompletedTask;
                               });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        setup?.Invoke(app);

        app.Run(requestDelegate);
    }
}