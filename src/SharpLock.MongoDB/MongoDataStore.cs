using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SharpLock.MongoDB
{
    public class MongoDataStore<TLockableObject> : IDataStore<TLockableObject, ObjectId>
        where TLockableObject : ISharpLockable<ObjectId>
    {
        private readonly MongoDataStore<TLockableObject, TLockableObject> _baseDataStore;

        public MongoDataStore(IMongoCollection<TLockableObject> col, ILogger logger, TimeSpan lockTime,
            CancellationToken token)
        {
            _baseDataStore = new MongoDataStore<TLockableObject, TLockableObject>(col, logger, lockTime, token);
        }

        public MongoDataStore(IMongoCollection<TLockableObject> col, ILogger logger, TimeSpan lockTime)
        {
            _baseDataStore = new MongoDataStore<TLockableObject, TLockableObject>(col, logger, lockTime, new CancellationToken());
        }

        public ILogger GetLogger() => _baseDataStore.GetLogger();
        public CancellationToken GetToken() => _baseDataStore.GetToken();

        public TimeSpan GetLockTime() => _baseDataStore.GetLockTime();

        public Task<TLockableObject> AcquireLockAsync(ObjectId baseObjId, TLockableObject obj, int staleLockMultiplier,
            CancellationToken token)
        {
            return _baseDataStore.AcquireLockAsync(baseObjId, obj, x => x, staleLockMultiplier, token);
        }

        public Task<TLockableObject> RefreshLockAsync(ObjectId baseObjId, TLockableObject obj, CancellationToken token)
        {
            return _baseDataStore.RefreshLockAsync(baseObjId, obj, x => x, token);
        }

        public Task<TLockableObject> ReleaseLockAsync(ObjectId baseObjId, TLockableObject obj, CancellationToken token)
        {
            return _baseDataStore.ReleaseLockAsync(baseObjId, obj, x => x, token);
        }
    }
}