using System;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace SharpLock.MongoDB
{
    public class MongoDataStore<TBaseObject> : MongoDataStore<TBaseObject, TBaseObject> where TBaseObject : SharpLockable<ObjectId>
    {
        public MongoDataStore(IMongoCollection<TBaseObject> col, ILogger logger, TimeSpan lockTime, CancellationToken token)
            : base(col, logger, lockTime, token)
        {
        }
    }
}