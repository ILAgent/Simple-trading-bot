using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TI.Service.Api;
using TI.Service.common;
using TI.Service.Strategy.Data;
using Tinkoff.Trading.OpenApi.Models;

namespace TI.Service.Strategy
{
    public class StockFollower
    {
        private readonly ContextProvider _contextProvider;
        private readonly Action<PlacedOrder> _addOrder;

        public YesterdayCandleData Stock { get; }
        public Status Status { get; private set; } = new Init();

        public StockFollower(YesterdayCandleData stock, ContextProvider contextProvider,
            Action<PlacedOrder> addOrder,
            Status? status = null)
        {
            Stock = stock;
            _contextProvider = contextProvider;
            _addOrder = addOrder;
            if (status != null)
            {
                Status = status;
            }
        }

        public async Task Perform(DateTime time, CancellationToken ct)
        {
            Console.WriteLine($"PERFORM START {Stock} at {time}");

            var candles = await _contextProvider.Do(ctx => ctx.MarketCandlesAsync(Stock.Figi,
                time.Subtract(TimeSpan.FromMinutes(20)), time,
                CandleInterval.Minute), ct);
            ct.ThrowIfCancellationRequested();

            CandlePayload? candle = candles.Candles.LastOrDefault();
            candle.Dump();

            bool IsBuyStop() => candle != null && (Stock.Close - candle.Close >= Stock.Tick * 20);
            switch (Status)
            {
                case Init _:
                {
                    if (IsBuyStop())
                        Status = new BuyStop();
                    else if (candle != null)
                        Status = new WaitingForBuy();
                }
                    break;
                case WaitingForBuy _:
                {
                    if (candle == null)
                        Status = new Closed();
                    else if (IsBuyStop())
                        Status = new BuyStop();
                }
                    break;
                case BuyStop _:
                {
                    if (candle == null)
                    {
                        Status = new Closed();
                    }
                    else if (candle.Close > Stock.Close)
                    {
                        PlacedMarketOrder order = await _contextProvider.Do(ctx => ctx.PlaceMarketOrderAsync(
                            new MarketOrder(Stock.Figi, 1, OperationType.Buy)
                        ), ct);
                        ct.ThrowIfCancellationRequested();
                        order.Dump();
                        _addOrder(new PlacedOrder(order, Stock.Name, time, candle.Close));

                        if (order.Status == OrderStatus.Rejected)
                        {
                            Console.WriteLine($"Buy order rejected: {order.RejectReason}");
                            break;
                        }

                        Status = new WaitingForSell
                        {
                            SellPrice = candle.Close - Stock.Half(),
                            TakeProfit = candle.Close + Stock.Part(60M)
                        };
                    }
                }
                    break;
                case WaitingForSell status:
                {
                    if (candle == null) break;

                    var possiblyNewStop = candle.Close - Stock.Half();
                    if (possiblyNewStop > status.SellPrice)
                    {
                        status.SellPrice = possiblyNewStop;
                        Console.WriteLine("Limit became higher");
                    }

                    if (candle.Close <= status.SellPrice ||
                        candle.Close >= status.TakeProfit && status.TakeProfit != default)
                    {
                        var position = (await _contextProvider.Do(c => c.PortfolioAsync()))
                            .Positions
                            .FirstOrDefault(it => it.Figi == candle.Figi);
                        if (position != null)
                        {
                            var closeOrder = await _contextProvider.Do(ctx => ctx.PlaceMarketOrderAsync(
                                new MarketOrder(Stock.Figi, 1, OperationType.Sell)
                            ), ct);
                            ct.ThrowIfCancellationRequested();
                            closeOrder.Dump();
                            _addOrder(new PlacedOrder(closeOrder, Stock.Name, time, candle.Close));

                            if (closeOrder.Status == OrderStatus.Rejected)
                            {
                                Console.WriteLine($"Sell order rejected: {closeOrder.RejectReason}");
                                break;
                            }
                        }

                        Status = new Closed();
                    }
                }
                    break;
            }

            Console.WriteLine($"STATUS: {Status}");
        }
    }
}