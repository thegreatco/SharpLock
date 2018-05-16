using System;
using System.Linq.Expressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using System.Collections.Generic;

namespace SharpLock.MongoDB
{
    public class MongoDataStore<TBaseObject, TLockableObject> : IDataStore<TBaseObject, TLockableObject, ObjectId>
        where TLockableObject : ISharpLockable<ObjectId> where TBaseObject : class, ISharpLockableBase<ObjectId>
    {
        private readonly ISharpLockLogger _sharpLockLogger;
        private readonly IMongoCollection<TBaseObject> _col;
        private readonly TimeSpan _lockTime;

        public MongoDataStore(IMongoCollection<TBaseObject> col, ISharpLockLogger sharpLockLogger, TimeSpan lockTime)
        {
            _sharpLockLogger = sharpLockLogger ?? throw new ArgumentNullException(nameof(sharpLockLogger));
            _col = col ?? throw new ArgumentNullException(nameof(col));
            _lockTime = lockTime == default(TimeSpan) ? throw new ArgumentNullException(nameof(lockTime)) : lockTime;
        }

        public ISharpLockLogger GetLogger() => _sharpLockLogger;

        public TimeSpan GetLockTime() => _lockTime;

        public Task<TBaseObject> AcquireLockAsync(ObjectId baseObjId, TLockableObject obj,
            Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, int staleLockMultiplier,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (baseObjId == null) throw new ArgumentNullException(nameof(baseObjId), "Base Object Id cannot be null");
            if (obj == null) throw new ArgumentNullException(nameof(obj), "Lockable Object cannot be null");
            if (fieldSelector == null) throw new ArgumentNullException(nameof(fieldSelector), "Field Selector for lockable object cannot be null");
            var lockTime = DateTime.UtcNow.Add(_lockTime);
            var staleLockTime = DateTime.UtcNow.AddMilliseconds(_lockTime.TotalMilliseconds * staleLockMultiplier * -1);
            var query = Builders<TBaseObject>.Filter.And(
                Builders<TBaseObject>.Filter.Eq(x => x.Id, baseObjId),
                Builders<TBaseObject>.Filter.ElemMatch(fieldSelector,
                    Builders<TLockableObject>.Filter.And(
                        Builders<TLockableObject>.Filter.Eq(x => x.Id, obj.Id),
                        Builders<TLockableObject>.Filter.Or(
                            Builders<TLockableObject>.Filter.Eq(x => x.LockId, null),
                            Builders<TLockableObject>.Filter.And(
                                Builders<TLockableObject>.Filter.Ne(x => x.LockId, null),
                                Builders<TLockableObject>.Filter.Lte(x => x.UpdateLock, staleLockTime))))));

            var update = Builders<TBaseObject>.Update
                .Set(Combine(fieldSelector, x => x.ElementAt(-1).UpdateLock), lockTime)
                .Set(Combine(fieldSelector, x => x.ElementAt(-1).LockId), Guid.NewGuid());

            _sharpLockLogger.Trace("Acquire Lock Query: {Query}, Acquire Lock Update: {Update}",
                query.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(),
                    BsonSerializer.SerializerRegistry).ToJson(),
                update.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(),
                    BsonSerializer.SerializerRegistry).ToJson());

            return _col.FindOneAndUpdateAsync(query, update,
                new FindOneAndUpdateOptions<TBaseObject, TBaseObject> {ReturnDocument = ReturnDocument.After}, cancellationToken);
        }

        public Task<TBaseObject> AcquireLockAsync(ObjectId baseObjId, TLockableObject obj,
            Expression<Func<TBaseObject, TLockableObject>> fieldSelector, int staleLockMultiplier,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (baseObjId == null) throw new ArgumentNullException(nameof(baseObjId), "Base Object Id cannot be null");
            if (obj == null) throw new ArgumentNullException(nameof(obj), "Lockable Object cannot be null");
            if (fieldSelector == null) throw new ArgumentNullException(nameof(fieldSelector), "Field Selector for lockable object cannot be null");
            var lockTime = DateTime.UtcNow.Add(_lockTime);
            var staleLockTime = DateTime.UtcNow.AddMilliseconds(_lockTime.TotalMilliseconds * staleLockMultiplier * -1);
            var query = Builders<TBaseObject>.Filter.And(
                Builders<TBaseObject>.Filter.Eq(x => x.Id, baseObjId),
                Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.Id), obj.Id),
                Builders<TBaseObject>.Filter.Or(
                    Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.LockId), null),
                    Builders<TBaseObject>.Filter.And(
                        Builders<TBaseObject>.Filter.Ne(Combine(fieldSelector, x => x.LockId), null),
                        Builders<TBaseObject>.Filter.Lte(Combine(fieldSelector, x => x.UpdateLock), staleLockTime))));

            var update = Builders<TBaseObject>.Update
                .Set(Combine(fieldSelector, x => x.UpdateLock), lockTime)
                .Set(Combine(fieldSelector, x => x.LockId), Guid.NewGuid());

            _sharpLockLogger.Trace("Acquire Lock Query: {Query}, Acquire Lock Update: {Update}",
                query.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(),
                    BsonSerializer.SerializerRegistry).ToJson(),
                update.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(),
                    BsonSerializer.SerializerRegistry).ToJson());

            return _col.FindOneAndUpdateAsync(query, update,
                new FindOneAndUpdateOptions<TBaseObject, TBaseObject> {ReturnDocument = ReturnDocument.After}, cancellationToken);
        }

        public async Task<bool> RefreshLockAsync(ObjectId baseObjId, ObjectId lockedObjectId, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = Builders<TBaseObject>.Filter.And(
                Builders<TBaseObject>.Filter.Eq(x => x.Id, baseObjId),
                Builders<TBaseObject>.Filter.ElemMatch(fieldSelector,
                    Builders<TLockableObject>.Filter.And(
                        Builders<TLockableObject>.Filter.Eq(x => x.Id, lockedObjectId),
                        Builders<TLockableObject>.Filter.Eq(x => x.LockId, lockedObjectLockId))));

            var update = Builders<TBaseObject>.Update.Set(Combine(fieldSelector, x => x.ElementAt(-1).UpdateLock),
                DateTime.UtcNow.Add(_lockTime));

            _sharpLockLogger.Trace("Refresh Lock Query: {Query}, Refresh Lock Update: {Update}",
                query.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(),
                    BsonSerializer.SerializerRegistry).ToJson(),
                update.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(),
                    BsonSerializer.SerializerRegistry).ToJson());

            var updateResult = await _col.UpdateOneAsync(query, update, cancellationToken: cancellationToken);
            return updateResult.IsModifiedCountAvailable && updateResult.MatchedCount == 1 && updateResult.ModifiedCount == 1;
        }

        public async Task<bool> RefreshLockAsync(ObjectId baseObjId, ObjectId lockedObjectId, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, TLockableObject>> fieldSelector, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = Builders<TBaseObject>.Filter.And(
                Builders<TBaseObject>.Filter.Eq(x => x.Id, baseObjId),
                Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.Id), lockedObjectId),
                Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.LockId), lockedObjectLockId));

            var update = Builders<TBaseObject>.Update.Set(Combine(fieldSelector, x => x.UpdateLock), DateTime.UtcNow.Add(_lockTime));

            _sharpLockLogger.Trace("Refresh Lock Query: {Query}, Refresh Lock Update: {Update}",
                query.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(),
                    BsonSerializer.SerializerRegistry).ToJson(),
                update.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(),
                    BsonSerializer.SerializerRegistry).ToJson());

            var updateResult = await _col.UpdateOneAsync(query, update, cancellationToken: cancellationToken);
            return updateResult.IsModifiedCountAvailable && updateResult.MatchedCount == 1 && updateResult.ModifiedCount == 1;
        }

        public async Task<bool> ReleaseLockAsync(ObjectId baseObjId, ObjectId lockedObjectId, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = Builders<TBaseObject>.Filter.And(
                Builders<TBaseObject>.Filter.Eq(x => x.Id, baseObjId),
                Builders<TBaseObject>.Filter.ElemMatch(fieldSelector,
                    Builders<TLockableObject>.Filter.And(
                        Builders<TLockableObject>.Filter.Eq(x => x.Id, lockedObjectId),
                        Builders<TLockableObject>.Filter.Eq(x => x.LockId, lockedObjectLockId))));

            var update = Builders<TBaseObject>.Update
                .Set(Combine(fieldSelector, x => x.ElementAt(-1).UpdateLock), null)
                .Set(Combine(fieldSelector, x => x.ElementAt(-1).LockId), null);

            _sharpLockLogger.Trace("Refresh Lock Query: {Query}, Refresh Lock Update: {Update}",
                query.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(),
                    BsonSerializer.SerializerRegistry).ToJson(),
                update.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(),
                    BsonSerializer.SerializerRegistry).ToJson());

            var updateResult = await _col.UpdateOneAsync(query, update, cancellationToken: cancellationToken);
            return updateResult.IsModifiedCountAvailable && updateResult.MatchedCount == 1 && updateResult.ModifiedCount == 1;
        }

        public Task<TBaseObject> GetLockedObjectAsync(ObjectId baseObjId, ObjectId lockedObjectId, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, TLockableObject>> fieldSelector, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = Builders<TBaseObject>.Filter.And(
                Builders<TBaseObject>.Filter.Eq(x => x.Id, baseObjId),
                Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.Id), lockedObjectId),
                Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.LockId), lockedObjectLockId));
            return _col.Find(query).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<TBaseObject> GetLockedObjectAsync(ObjectId baseObjId, ObjectId lockedObjectId, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = Builders<TBaseObject>.Filter.And(
                Builders<TBaseObject>.Filter.Eq(x => x.Id, baseObjId),
                Builders<TBaseObject>.Filter.ElemMatch(fieldSelector,
                    Builders<TLockableObject>.Filter.And(
                        Builders<TLockableObject>.Filter.Eq(x => x.Id, lockedObjectId),
                        Builders<TLockableObject>.Filter.Eq(x => x.LockId, lockedObjectLockId))));
            return _col.Find(query).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> ReleaseLockAsync(ObjectId baseObjId, ObjectId lockedObjectId, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, TLockableObject>> fieldSelector, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = Builders<TBaseObject>.Filter.And(
                Builders<TBaseObject>.Filter.Eq(x => x.Id, baseObjId),
                Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.Id), lockedObjectId),
                Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.LockId), lockedObjectLockId));

            var update = Builders<TBaseObject>.Update
                .Set(Combine(fieldSelector, x => x.UpdateLock), null)
                .Set(Combine(fieldSelector, x => x.LockId), null);

            _sharpLockLogger.Trace("Refresh Lock Query: {Query}, Refresh Lock Update: {Update}",
                query.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(),
                    BsonSerializer.SerializerRegistry).ToJson(),
                update.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(),
                    BsonSerializer.SerializerRegistry).ToJson());

            var updateResult = await _col.UpdateOneAsync(query, update, cancellationToken: cancellationToken);
            return updateResult.IsModifiedCountAvailable && updateResult.MatchedCount == 1 && updateResult.ModifiedCount == 1;
        }

        private static Expression<Func<T1, T3>> Combine<T1, T2, T3>(Expression<Func<T1, T2>> first,
            Expression<Func<T2, T3>> second)
        {
            var param = Expression.Parameter(typeof(T1), "param");

            var newFirst = new ReplaceVisitor(first.Parameters.First(), param)
                .Visit(first.Body);
            var newSecond = new ReplaceVisitor(second.Parameters.First(), newFirst)
                .Visit(second.Body);

            return Expression.Lambda<Func<T1, T3>>(newSecond, param);
        }

        private class ReplaceVisitor : System.Linq.Expressions.ExpressionVisitor
        {
            private readonly Expression _from, _to;

            public ReplaceVisitor(Expression from, Expression to)
            {
                _from = from;
                _to = to;
            }

            public override Expression Visit(Expression node)
            {
                return node == _from ? _to : base.Visit(node);
            }
        }
    }
}