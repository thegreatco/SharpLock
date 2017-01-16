using System;
using System.Linq.Expressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using Serilog;
using System.Collections.Generic;

namespace SharpLock.MongoDB
{
    public class MongoDataStore<TBaseObject, TLockableObject> : IDataStore<TBaseObject, TLockableObject, ObjectId> where TLockableObject : SharpLockable<ObjectId> where TBaseObject : SharpLockableBase<ObjectId>
    {
        private readonly ILogger _logger;
        private readonly CancellationToken _token;
        private readonly IMongoCollection<TBaseObject> _col;
        private readonly TimeSpan _lockTime;
        
        public MongoDataStore(IMongoCollection<TBaseObject> col, ILogger logger, TimeSpan lockTime, CancellationToken token)
        {
            _logger = logger;
            _col = col;
            _token = token;
            _lockTime = lockTime;
        }

        public ILogger GetLogger() => _logger;
        public CancellationToken GetToken() => _token;

        public TimeSpan GetLockTime() => _lockTime;

        public Task<TBaseObject> AcquireLockAsync(ObjectId baseObjId, TLockableObject obj, Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, int staleLockMultiplier, CancellationToken token)
        {
            var lockTime = DateTime.UtcNow.Add(_lockTime).Ticks;
            var staleLockTime = DateTime.UtcNow.AddMilliseconds(_lockTime.TotalMilliseconds * staleLockMultiplier * -1).Ticks;
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
            
            _logger.Verbose("Acquire Lock Query: {Query}, Acquire Lock Update: {Update}", 
                query.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(), BsonSerializer.SerializerRegistry).ToJson(), 
                update.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(), BsonSerializer.SerializerRegistry).ToJson());
            
            return _col.FindOneAndUpdateAsync(query, update,
                    new FindOneAndUpdateOptions<TBaseObject, TBaseObject> { ReturnDocument = ReturnDocument.After }, _token);
        }

        public Task<TBaseObject> AcquireLockAsync(ObjectId baseObjId, TLockableObject obj, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, int staleLockMultiplier, CancellationToken token)
        {
            var lockTime = DateTime.UtcNow.Add(_lockTime).Ticks;
            var staleLockTime = DateTime.UtcNow.AddMilliseconds(_lockTime.TotalMilliseconds * staleLockMultiplier * -1).Ticks;
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
            
            _logger.Verbose("Acquire Lock Query: {Query}, Acquire Lock Update: {Update}", 
                query.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(), BsonSerializer.SerializerRegistry).ToJson(), 
                update.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(), BsonSerializer.SerializerRegistry).ToJson());
            
            return _col.FindOneAndUpdateAsync(query, update,
                    new FindOneAndUpdateOptions<TBaseObject, TBaseObject> { ReturnDocument = ReturnDocument.After }, _token);
        }

        public Task<TBaseObject> RefreshLockAsync(ObjectId baseObjId, TLockableObject obj, Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, CancellationToken token)
        {
            var query = Builders<TBaseObject>.Filter.And(
                            Builders<TBaseObject>.Filter.Eq(x => x.Id, baseObjId),
                            Builders<TBaseObject>.Filter.ElemMatch(fieldSelector,
                                Builders<TLockableObject>.Filter.And(
                                    Builders<TLockableObject>.Filter.Eq(x => x.Id, obj.Id),
                                    Builders<TLockableObject>.Filter.Eq(x => x.LockId, obj.LockId),
                                    Builders<TLockableObject>.Filter.Eq(x => x.UpdateLock, obj.UpdateLock))));
            
            var update = Builders<TBaseObject>.Update.Set(Combine(fieldSelector, x => x.ElementAt(-1).UpdateLock), DateTime.UtcNow.Add(_lockTime).Ticks);
            
            _logger.Verbose("Refresh Lock Query: {Query}, Refresh Lock Update: {Update}", 
                query.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(), BsonSerializer.SerializerRegistry).ToJson(), 
                update.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(), BsonSerializer.SerializerRegistry).ToJson());
            
            return _col.FindOneAndUpdateAsync(query, update,
                new FindOneAndUpdateOptions<TBaseObject, TBaseObject> { ReturnDocument = ReturnDocument.After }, _token);
        }
         
        public Task<TBaseObject> RefreshLockAsync(ObjectId baseObjId, TLockableObject obj, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, CancellationToken token)
        {
            var query = Builders<TBaseObject>.Filter.And(
                    Builders<TBaseObject>.Filter.Eq(x => x.Id, baseObjId),
                    Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.Id), obj.Id),
                    Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.LockId), obj.LockId),
                    Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.UpdateLock), obj.UpdateLock));
            
            var update = Builders<TBaseObject>.Update.Set(Combine(fieldSelector, x => x.UpdateLock), DateTime.UtcNow.Add(_lockTime).Ticks);
            
            _logger.Verbose("Refresh Lock Query: {Query}, Refresh Lock Update: {Update}", 
                query.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(), BsonSerializer.SerializerRegistry).ToJson(), 
                update.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(), BsonSerializer.SerializerRegistry).ToJson());

            return _col.FindOneAndUpdateAsync(query, update,
                new FindOneAndUpdateOptions<TBaseObject, TBaseObject> { ReturnDocument = ReturnDocument.After }, _token);
        }

        public Task<TBaseObject> ReleaseLockAsync(ObjectId baseObjId, TLockableObject obj, Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, CancellationToken token)
        {
            var query = Builders<TBaseObject>.Filter.And(
                            Builders<TBaseObject>.Filter.Eq(x => x.Id, baseObjId),
                            Builders<TBaseObject>.Filter.ElemMatch(fieldSelector,
                                Builders<TLockableObject>.Filter.And(
                                    Builders<TLockableObject>.Filter.Eq(x => x.Id, obj.Id),
                                    Builders<TLockableObject>.Filter.Eq(x => x.UpdateLock, obj.UpdateLock))));

            var update = Builders<TBaseObject>.Update
                    .Set(Combine(fieldSelector, x => x.ElementAt(-1).UpdateLock), null)
                    .Set(Combine(fieldSelector, x => x.ElementAt(-1).LockId), null);
                    
            _logger.Verbose("Refresh Lock Query: {Query}, Refresh Lock Update: {Update}", 
                query.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(), BsonSerializer.SerializerRegistry).ToJson(), 
                update.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(), BsonSerializer.SerializerRegistry).ToJson());

            return _col.FindOneAndUpdateAsync(query, update,
                new FindOneAndUpdateOptions<TBaseObject, TBaseObject> { ReturnDocument = ReturnDocument.After }, _token);
        }

        public Task<TBaseObject> ReleaseLockAsync(ObjectId baseObjId, TLockableObject obj, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, CancellationToken token)
        {
            var query = Builders<TBaseObject>.Filter.And(
                    Builders<TBaseObject>.Filter.Eq(x => x.Id, baseObjId),
                    Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.Id), obj.Id),
                    Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.UpdateLock), obj.UpdateLock));

            var update = Builders<TBaseObject>.Update
                    .Set(Combine(fieldSelector, x => x.UpdateLock), null)
                    .Set(Combine(fieldSelector, x => x.LockId), null);

            _logger.Verbose("Refresh Lock Query: {Query}, Refresh Lock Update: {Update}", 
                query.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(), BsonSerializer.SerializerRegistry).ToJson(), 
                update.Render(BsonSerializer.SerializerRegistry.GetSerializer<TBaseObject>(), BsonSerializer.SerializerRegistry).ToJson());

            return _col.FindOneAndUpdateAsync(query, update,
                new FindOneAndUpdateOptions<TBaseObject, TBaseObject> { ReturnDocument = ReturnDocument.After }, _token);
        }

        private static Expression<Func<T1, T3>> Combine<T1, T2, T3>(Expression<Func<T1, T2>> first, Expression<Func<T2, T3>> second)
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
                this._from = @from;
                this._to = to;
            }

            public override Expression Visit(Expression node)
            {
                return node == _from ? _to : base.Visit(node);
            }
        }
    }
}