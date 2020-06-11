using System;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Tinkoff.Trading.OpenApi.Models;

namespace TI.Service.Strategy.Data
{
    public class YesterdayCandleData
    {
        [BsonConstructor]
        public YesterdayCandleData(string figi, string name, int volume, decimal open,
            decimal close, decimal low, decimal high, Currency currency, decimal tick)
        {
            Figi = figi;
            Name = name;
            Volume = volume;
            Open = open;
            Close = close;
            Low = low;
            High = high;
            Currency = currency;
            Tick = tick;
        }

        [BsonId] public string Figi { get; }
        [BsonElement] public string Name { get; }
        [BsonElement] public int Volume { get; }
        [BsonElement] public decimal Open { get; }
        [BsonElement] public decimal Close { get; }
        [BsonElement] public decimal Low { get; }
        [BsonElement] public decimal High { get; }
        [BsonElement] public Currency Currency { get; }
        [BsonElement] public decimal Tick { get; }

        public static YesterdayCandleData Create(MarketInstrument stock, CandlePayload candle)
        {
            return new YesterdayCandleData(
                stock.Figi,
                stock.Name,
                Convert.ToInt32(candle.Volume),
                candle.Open,
                candle.Close,
                candle.Low,
                candle.High,
                stock.Currency,
                stock.MinPriceIncrement
            );
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}