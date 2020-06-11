using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

namespace TI.Service.Api
{
    public class ContextProvider
    {
        private readonly SandboxConnection _connection;
        private readonly SandboxContext _context;
        private readonly TimeSpan _minimalWait = TimeSpan.FromMilliseconds(500);
        private DateTime _lastCall;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public ContextProvider()
        {
            _connection = ConnectionFactory.GetSandboxConnection(ApiData.TokenSandbox);
            _context = _connection.Context;
            Init();
        }

        private async void Init()
        {
            await _context.RegisterAsync(BrokerAccountType.Tinkoff);
            var balance = await Do(ctx => ctx.PortfolioCurrenciesAsync());
            foreach (var currency in balance.Currencies)
            {
                if (currency.Balance > 0) continue;
                await Do(_ => _context.SetCurrencyBalanceAsync(currency.Currency, 1_000_000));
            }
        }

        public async Task<T> Do<T>(Func<Context, Task<T>> func, CancellationToken ct = default)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                var spent = DateTime.UtcNow - _lastCall;
                var wait = spent < _minimalWait ? _minimalWait - spent : TimeSpan.Zero;
                await Task.Delay(wait, ct);
                return await func(_context);
            }
            finally
            {
                _lastCall = DateTime.UtcNow;
                _semaphore.Release();
                ct.ThrowIfCancellationRequested();
            }
        }

        public async Task Do(Func<Context, Task> func)
        {
            var spent = DateTime.UtcNow - _lastCall;
            var wait = spent < _minimalWait ? _minimalWait - spent : TimeSpan.Zero;
            await Task.Delay(wait);
            _lastCall = DateTime.UtcNow;
            await func(_context);
        }
    }
}