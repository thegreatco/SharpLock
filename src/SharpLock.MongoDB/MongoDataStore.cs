using System;
using System.Linq.Expressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace SharpLock.MongoDB
{
    public class MongoDataStore<TBaseObject, TLockableObject> : IDataStore<TBaseObject, TLockableObject> where TLockableObject : SharpLockable<ObjectId>
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

        public Task<TBaseObject> AcquireLockAsync(TLockableObject obj, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, CancellationToken token)
        {
            var lockTime = DateTime.UtcNow.Add(_lockTime).Ticks;
            return _col.FindOneAndUpdateAsync(
                    Builders<TBaseObject>.Filter.And(
                        Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.Id), obj.Id),
                        Builders<TBaseObject>.Filter.Or(
                            Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.UpdateLock), null),
                            Builders<TBaseObject>.Filter.And(
                                Builders<TBaseObject>.Filter.Ne(Combine(fieldSelector, x => x.LockId), null),
                                Builders<TBaseObject>.Filter.Lte(Combine(fieldSelector, x => x.UpdateLock), DateTime.UtcNow.Subtract(_lockTime - TimeSpan.FromSeconds(10)).Ticks)))),
                    Builders<TBaseObject>.Update
                        .Set(Combine(fieldSelector, x => x.UpdateLock), lockTime)
                        .Set(Combine(fieldSelector, x => x.LockId), Guid.NewGuid()),
                    new FindOneAndUpdateOptions<TBaseObject, TBaseObject> { ReturnDocument = ReturnDocument.After }, _token);
        }
         
        public Task<TBaseObject> RefreshLockAsync(TLockableObject obj, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, CancellationToken token)
        {
            return _col.FindOneAndUpdateAsync(
                Builders<TBaseObject>.Filter.And(
                    Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.Id), obj.Id),
                    Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.LockId), obj.LockId),
                    Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.UpdateLock), obj.UpdateLock)),
                Builders<TBaseObject>.Update.Set(Combine(fieldSelector, x => x.UpdateLock), DateTime.UtcNow.Add(_lockTime).Ticks),
                new FindOneAndUpdateOptions<TBaseObject, TBaseObject> { ReturnDocument = ReturnDocument.After }, _token);
        }

        public Task<TBaseObject> ReleaseLockAsync(TLockableObject obj, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, CancellationToken token)
        {
            return _col.FindOneAndUpdateAsync(
                Builders<TBaseObject>.Filter.And(
                    Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.Id), obj.Id),
                    Builders<TBaseObject>.Filter.Eq(Combine(fieldSelector, x => x.UpdateLock), obj.UpdateLock)),
                Builders<TBaseObject>.Update
                    .Set(Combine(fieldSelector, x => x.UpdateLock), null)
                    .Set(Combine(fieldSelector, x => x.LockId), null),
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

        private class ReplaceVisitor : ExpressionVisitor
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