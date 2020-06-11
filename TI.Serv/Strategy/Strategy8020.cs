using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TI.Service.Api;
using TI.Service.Strategy.Data;
using TI.Service.Time;

#nullable enable
namespace TI.Service.Strategy
{
    public class Strategy8020 : IDisposable
    {
        private readonly TimeProvider _timeProvider;
        private readonly AcceptableStocksProvider _stocksProvider;
        private readonly ContextProvider _contextProvider;
        private readonly StateStorage _stateStorage;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private List<StockFollower> _followers = new List<StockFollower>();

        private StrategyState _state = new StrategyState();

        public Strategy8020(TimeProvider timeProvider, AcceptableStocksProvider stocksProvider,
            ContextProvider contextProvider, StateStorage stateStorage)
        {
            _timeProvider = timeProvider;
            _stocksProvider = stocksProvider;
            _contextProvider = contextProvider;
            _stateStorage = stateStorage;
        }

        public async Task Start()
        {
            while (true)
            {
                try
                {
                    await Loop();
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Strategy canceled");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    _state.Exceptions.Add(ex.ToString());
                }

                await Task.Delay(5000);
            }
        }

        private async Task Loop()
        {
            _state = _stateStorage.LastState();
            _followers = _state
                .Stocks
                .Select(it => new StockFollower(it.CandleData, _contextProvider,
                    _state.Orders.Add, it.Status))
                .ToList();

            while (true)
            {
                Console.WriteLine($"LOOP START {_timeProvider.Now}");
                var lastLoopDate = _state.Time.Date;
                _state.Time = _timeProvider.Now;

                if (lastLoopDate < _timeProvider.Today)
                {
                    _state.DayChanges++;
                    var savedFollowers = _followers
                        .Where(it => it.Status is WaitingForSell)
                        .ToList();

                    var stocks = await _stocksProvider.GetStocks(_cancellationTokenSource.Token);
                    _followers = stocks
                        .Select(it => new StockFollower(it, _contextProvider, _state.Orders.Add))
                        .ToList();
                    _followers.AddRange(savedFollowers);
                    _state.Stocks = _followers.Select(it => new FollowingStock(it.Stock, it.Status)).ToList();
                }

                if (_followers.Count == 0 && _state.Time.Date == _timeProvider.Today)
                {
                    var tomorrow = _timeProvider.Today.Add(TimeSpan.FromDays(1));
                    var waiting = tomorrow - _timeProvider.Now + TimeSpan.FromMinutes(5);
                    Console.WriteLine($"GO SLEEPING FOR {waiting}");
                    await Task.Delay(waiting, _cancellationTokenSource.Token);
                    Console.WriteLine("WAKE UP");
                }

                foreach (var follower in _followers)
                {
                    await follower.Perform(_timeProvider.Now, _cancellationTokenSource.Token);
                }

                _state.Stocks = _followers.Select(it => new FollowingStock(it.Stock, it.Status)).ToList();

                _stateStorage.AddState(_state);

                _timeProvider.SaveTime();

                await _contextProvider.Do(it => _state.Dump(it, _cancellationTokenSource.Token));

                _followers.RemoveAll(it => it.Status is Closed);
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}