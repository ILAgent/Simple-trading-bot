using System;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace TI.Service.Strategy.Data
{
    public class StateStorage
    {
        private readonly IMongoDatabase _db;

        public StateStorage()
        {
            var dbClient = new MongoClient(ApiData.MongoUri);
            _db = dbClient.GetDatabase("heroku_vg8j971t");
            //StatesCollection.DeleteMany(FilterDefinition<StrategyState>.Empty);
        }

        public void AddState(StrategyState state)
        {
            try
            {
                StatesCollection.DeleteMany(FilterDefinition<StrategyState>.Empty);
                StatesCollection.InsertOne(state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public StrategyState LastState()
        {
            try
            {
                var restored = StatesCollection
                    .AsQueryable()
                    .OrderByDescending(it => it.Time)
                    .FirstOrDefault();
                if (restored == null)
                    return new StrategyState();
                restored.RestoresAmount++;
                return restored;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new StrategyState();
            }
        }

        private IMongoCollection<StrategyState> StatesCollection =>
            _db.GetCollection<StrategyState>(nameof(StrategyState));
    }
}