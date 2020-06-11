using System;
using Microsoft.Extensions.DependencyInjection;
using TI.Service.Time;
using IServiceCollection = Microsoft.Extensions.DependencyInjection.IServiceCollection;

#nullable enable

namespace TI.Service.Strategy
{
    public class StrategyProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeProvider _timeProvider;
        private Strategy8020? _strategy8020;

        public StrategyProvider(IServiceProvider serviceProvider, TimeProvider timeProvider)
        {
            _serviceProvider = serviceProvider;
            _timeProvider = timeProvider;
        }

        public Strategy8020 GetOrCreate()
        {
            // if (_strategy8020 == null)
            // {
            //     _timeProvider.RestoreOrSetCurrentTime(
            //         new DateTime(2020, 4, 29, 6, 50, 0, DateTimeKind.Utc)
            //     );
            // }

            return _strategy8020 ??= _serviceProvider.GetService<Strategy8020>();
        }

        public void Delete()
        {
            _strategy8020?.Dispose();
            _strategy8020 = null;
        }
    }
}