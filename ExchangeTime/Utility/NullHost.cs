using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeTime.Utility
{
    public class NullHost : IHost
    {
        public static readonly IHost Instance = new NullHost();
        public IServiceProvider Services => NullServiceProvider.Instance;
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    public class NullServiceProvider : IServiceProvider
    {
        public static readonly IServiceProvider Instance = new NullServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}
