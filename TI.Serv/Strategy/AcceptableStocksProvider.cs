using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TI.Service.Api;
using TI.Service.Strategy.Data;
using TI.Service.Time;
using Tinkoff.Trading.OpenApi.Models;

namespace TI.Service.Strategy
{
    public class AcceptableStocksProvider
    {
        private readonly TimeProvider _timeProvider;
        private readonly ContextProvider _contextProvider;

        public AcceptableStocksProvider(TimeProvider timeProvider, ContextProvider contextProvider)
        {
            _timeProvider = timeProvider;
            _contextProvider = contextProvider;
        }

        public async Task<IReadOnlyList<YesterdayCandleData>> GetStocks(CancellationToken ct)
        {
            Console.WriteLine("START SEARCHING");
            
            var allStocks = await _contextProvider.Do(ctx => ctx.MarketStocksAsync(), ct);
            ct.ThrowIfCancellationRequested();

            var acceptable = new List<YesterdayCandleData>();
            var meetAmount = 0;
            foreach (var stock in allStocks.Instruments)
            {
                CandlePayload? candle = await MeetRequirements(stock, ct).ConfigureAwait(false);
                if (candle != null)
                {
                    var data = YesterdayCandleData.Create(stock, candle);
                    acceptable.Add(data);
                    Console.WriteLine(data);
                    meetAmount++;
                }
            }

            Console.WriteLine($"Meet: {meetAmount}/{allStocks.Instruments.Count}");

            var topVolumes = PossibleVariants(acceptable);
            LogTopVolumes(topVolumes);
            return topVolumes;
        }

        private async Task<CandlePayload?> MeetRequirements(MarketInstrument instrument, CancellationToken ct)
        {
            try
            {
                var candles = await _contextProvider.Do(ctx => ctx.MarketCandlesAsync(
                    instrument.Figi, _timeProvider.Today.Subtract(TimeSpan.FromDays(7)), _timeProvider.Today, CandleInterval.Day
                ), ct);
                ct.ThrowIfCancellationRequested();

                var dayCandle = candles.Candles.LastOrDefault();
                if (dayCandle == null)
                {
                    return null;
                }
                var body = dayCandle.Close - dayCandle.Open;
                if (body >= 0)
                {
                    return null;
                }

                var shadow = dayCandle.Low - dayCandle.Close;
                return shadow / body < 0.2M ? dayCandle : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
                return null;
            }
        }

        private IReadOnlyList<YesterdayCandleData> PossibleVariants(IReadOnlyList<YesterdayCandleData> acceptable)
        {
            return acceptable
                .OrderByDescending(it => it.Volume)
                .Take(20)
                .ToList();
        }

        private void LogTopVolumes(IEnumerable<YesterdayCandleData> topVolumes)
        {
            Console.WriteLine("TOP VOLUMES:");
            foreach (var volume in topVolumes)
            {
                Console.WriteLine(volume);
            }
        }
    }
}