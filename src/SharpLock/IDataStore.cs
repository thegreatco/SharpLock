using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SharpLock
{
    public interface IDataStore<TBaseObject, TLockableObject, TId> where TBaseObject : SharpLockableBase<TId> where TLockableObject : SharpLockable<TId>
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
}