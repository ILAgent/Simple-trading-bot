using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

namespace TI.Service.Strategy.Data
{
    public class StrategyState
    {
        [BsonId] public DateTime Time { get; set; }
        public int RestoresAmount { get; set; }
        public IReadOnlyList<FollowingStock> Stocks { get; set; } = new List<FollowingStock>();
        public int DayChanges { get; set; }
        public IList<PlacedOrder> Orders { get; set; } = new List<PlacedOrder>();
        public IList<string> Exceptions { get; set; } = new List<string>();
    }

    public class FollowingStock
    {
        public void Deconstruct(out YesterdayCandleData candleData, out Status status)
        {
            candleData = CandleData;
            status = Status;
        }

        [BsonElement] public YesterdayCandleData CandleData { get; }
        [BsonElement] public Status Status { get; }

        [BsonConstructor]
        public FollowingStock(YesterdayCandleData candleData, Status status)
        {
            CandleData = candleData;
            Status = status;
        }
    }

    public class PlacedOrder
    {
        [BsonId] public string Id { get; }
        [BsonElement] public OperationType Type { get; }
        [BsonElement] public string StockName { get; }
        [BsonElement] public int Lots { get; }
        [BsonElement] public OrderStatus Status { get; }
        [BsonElement] public DateTime Time { get; }
        [BsonElement] public decimal Price { get; }

        [BsonConstructor]
        public PlacedOrder(string id, OperationType type, string stockName, int lots, OrderStatus status,
            DateTime time, decimal price)
        {
            Id = id;
            Type = type;
            StockName = stockName;
            Lots = lots;
            Status = status;
            Time = time;
            Price = price;
        }

        public PlacedOrder(PlacedMarketOrder order, string name, DateTime time, decimal price) :
            this(order.OrderId, order.Operation, name, order.ExecutedLots, order.Status, time, price)
        {
        }
    }

    public static class StateExtensions
    {
        public static async Task Dump(this StrategyState state, Context context, CancellationToken ct)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=======STATISTICS =====");
            sb.AppendLine(state.Time.ToString(CultureInfo.InvariantCulture));

            sb.AppendLine($"DAYS: {state.DayChanges}");
            sb.AppendLine($"RESTORES: {state.RestoresAmount}");
            sb.AppendLine($"EXCEPTION: {state.Exceptions.Count}");
            foreach (var exception in state.Exceptions)
            {
                Console.WriteLine(exception);
            }

            var balance = await context.PortfolioCurrenciesAsync();
            sb.AppendLine("BALANCE:");
            sb.AppendLine(JsonConvert.SerializeObject(balance));

            sb.AppendLine($"FOLLOWED STOCKS ( {state.Stocks.Count} ):");
            foreach (var (data, status) in state.Stocks)
            {
                sb.AppendLine(data.Name);
                sb.Append(status.GetType().Name);
                sb.AppendLine(JsonConvert.SerializeObject(status));
            }

            sb.AppendLine($"ORDERS HISTORY ( {state.Orders.Count} ):");
            foreach (var order in state.Orders)
            {
                sb.AppendLine(JsonConvert.SerializeObject(order));
            }

            Console.WriteLine(sb);
        }
    }
}