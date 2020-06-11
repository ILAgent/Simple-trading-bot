using System;
using Newtonsoft.Json;
using TI.Service.Strategy.Data;
using Tinkoff.Trading.OpenApi.Models;

namespace TI.Service.common
{
    public static class Extensions
    {
        public static decimal Half(this YesterdayCandleData candle) => (candle.Open - candle.Close) / 2;

        public static decimal Part(this YesterdayCandleData candle, decimal percent) =>
            (candle.Open - candle.Close) / 100M * percent;

        public static void Dump<T>(this T? obj) where T : class
        {
            Console.WriteLine(obj == null
                ? $"{typeof(T).Name} is Null"
                : typeof(T).Name + " is " + JsonConvert.SerializeObject(obj)
            );
        }
    }
}