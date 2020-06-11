using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

namespace IncomDetector
{
    [TestFixture]
    public class Class1
    {
        [Test]
        public async Task Test()
        {
            const string token =
                "t.vsqKlKaZ7zgKIYMIcd9q4NYG2vgitdbL7WVe5NaiRV4-m3UXhFegOlJZaDDQPWuHe-uacL8GY1TMNIsYafExgg";
            var connection = ConnectionFactory.GetConnection(token);
            var context = connection.Context;

            var ticker = await context.MarketSearchByTickerAsync("TEUR");

            var amountInPortfolio = (await context.PortfolioAsync())
                .Positions
                .FirstOrDefault(p => p.Figi == ticker.Instruments[0].Figi)
                ?.Lots;

            var candle = (await context.MarketCandlesAsync(
                    ticker.Instruments[0].Figi,
                    DateTime.UtcNow.Subtract(TimeSpan.FromDays(5)),
                    DateTime.UtcNow,
                    CandleInterval.Day
                ))
                .Candles
                .OrderByDescending(it => it.Close)
                .FirstOrDefault();
            var actualPrice = candle?.Close;


            var portfolioSum = amountInPortfolio * actualPrice;


            var operations = (await context.OperationsAsync(
                    DateTime.MinValue.ToUniversalTime(),
                    DateTime.UtcNow,
                    ticker.Instruments[0].Figi)
                )
                //.OrderBy(op => op.Date)
                .ToList();

            var spentMoney = operations.Sum(op =>
                op.Payment
            );

            var result = spentMoney + portfolioSum;
            Console.WriteLine("Hello World!");
        }
    }
}