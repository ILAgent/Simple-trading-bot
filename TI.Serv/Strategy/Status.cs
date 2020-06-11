using System;
using MongoDB.Bson.Serialization.Attributes;

namespace TI.Service.Strategy
{
    [BsonKnownTypes(typeof(Init), typeof(WaitingForBuy), typeof(BuyStop), typeof(WaitingForSell), typeof(Closed))]
    public abstract class Status
    {
        public override string ToString()
        {
            return GetType().Name;
        }
    }

    public class Init : Status
    {
    }

    public class WaitingForBuy : Status
    {
    }

    public class BuyStop : Status
    {
    }

    public class WaitingForSell : Status
    {
        public decimal SellPrice { get; set; }

        public decimal TakeProfit { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(SellPrice)}: {SellPrice}, {nameof(TakeProfit)}: {TakeProfit}";
        }
    }

    public class Closed : Status
    {
    }
}