using Microsoft.Extensions.DependencyInjection;
using TI.Service.Api;
using TI.Service.Strategy;
using TI.Service.Strategy.Data;
using TI.Service.Time;

namespace TI.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton<StrategyProvider>()
                .AddSingleton<TimeProvider>()
                .AddTransient<AcceptableStocksProvider>()
                .AddTransient<Strategy8020>()
                .AddSingleton<ContextProvider>()
                .AddSingleton<StateStorage>()
                .BuildServiceProvider();

            serviceProvider.GetService<StrategyProvider>()
                .GetOrCreate()
                .Start()
                .Wait();
        }
    }
}