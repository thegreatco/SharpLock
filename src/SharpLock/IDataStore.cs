using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace SharpLock
{
    public interface IDataStore<TBaseObject, TLockableObject, in TId> where TBaseObject : ISharpLockableBase<TId> where TLockableObject : ISharpLockable<TId>
    {
        ILogger GetLogger();
        CancellationToken GetToken();
        TimeSpan GetLockTime();
        Task<TBaseObject> AcquireLockAsync(TId baseObjId, TLockableObject obj, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, int staleLockMultiplier, CancellationToken token);
        Task<TBaseObject> AcquireLockAsync(TId baseObjId, TLockableObject obj, Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, int staleLockMultiplier, CancellationToken token);
        Task<TBaseObject> RefreshLockAsync(TId baseObjId, TLockableObject obj, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, CancellationToken token);
        Task<TBaseObject> RefreshLockAsync(TId baseObjId, TLockableObject obj, Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, CancellationToken token);
        Task<TBaseObject> ReleaseLockAsync(TId baseObjId, TLockableObject obj, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, CancellationToken token);
        Task<TBaseObject> ReleaseLockAsync(TId baseObjId, TLockableObject obj, Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, CancellationToken token);
    }

    public interface IDataStore<TLockableObject, in TId> where TLockableObject : ISharpLockable<TId>
    {
        ILogger GetLogger();
        CancellationToken GetToken();
        TimeSpan GetLockTime();
        Task<TLockableObject> AcquireLockAsync(TId baseObjId, TLockableObject obj, int staleLockMultiplier, CancellationToken token);
        Task<TLockableObject> RefreshLockAsync(TId baseObjId, TLockableObject obj, CancellationToken token);
        Task<TLockableObject> ReleaseLockAsync(TId baseObjId, TLockableObject obj, CancellationToken token);
    }
}