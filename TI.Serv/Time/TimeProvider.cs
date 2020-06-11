using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace TI.Service.Time
{
    public class TimeProvider
    {
        private TimeSpan _offset = TimeSpan.Zero;
        private readonly IMongoDatabase _db;

        public DateTime Today => Now.Date;

        public DateTime Yesterday => Today.Subtract(TimeSpan.FromDays(1));

        public DateTime Now => DateTime.UtcNow - _offset;

        public TimeProvider()
        {
            var dbClient = new MongoClient(ApiData.MongoUri);
            _db = dbClient.GetDatabase("heroku_vg8j971t");

            //_db.GetCollection<TimeHolder>(nameof(TimeHolder)).DeleteMany(FilterDefinition<TimeHolder>.Empty);

        }

        public void SaveTime()
        {
            try
            {
                var collection = _db.GetCollection<TimeHolder>(nameof(TimeHolder));
                collection.DeleteMany(FilterDefinition<TimeHolder>.Empty);
                collection.InsertOne(new TimeHolder {Time = Now});
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void RestoreOrSetCurrentTime(DateTime imitateCurrentTime)
        {
            DateTime? time = null;
            try
            {
                time = _db.GetCollection<TimeHolder>(nameof(TimeHolder))
                    .AsQueryable()
                    .OrderByDescending(it => it.Time)
                    .FirstOrDefault()
                    ?.Time;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            _offset = time != null
                ? DateTime.UtcNow - time.Value
                : DateTime.UtcNow - imitateCurrentTime;
        }

        // public IObservable<DateTime> CurrentTime => Observable
        //     .Interval(TimeSpan.FromMinutes(1))
        //     .Select(_ => DateTime.Now - _offset)
        //     .Publish()
        //     .RefCount();
        //
        // public IObservable<DateTime> DayChanges => CurrentTime
        //     .Zip(CurrentTime.Skip(1), (prev, cur) => (prev, cur))
        //     .Where(t => t.cur.Date > t.prev.Date)
        //     .Select(it => it.cur.Date);
    }

    public class TimeHolder
    {
        [BsonId] public DateTime Time { get; set; }
    }
}