using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace SharpLock
{
    public interface IDataStore<TBaseObject, TLockableObject, in TId> where TBaseObject : ISharpLockableBase<TId> where TLockableObject : ISharpLockable<TId>
    {
        ISharpLockLogger GetLogger();
        TimeSpan GetLockTime();

        Task<TBaseObject> AcquireLockAsync(TId baseObjId, TLockableObject obj,
            Expression<Func<TBaseObject, TLockableObject>> fieldSelector, int staleLockMultiplier,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<TBaseObject> AcquireLockAsync(TId baseObjId, TLockableObject obj,
            Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, int staleLockMultiplier,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<bool> RefreshLockAsync(TId baseObjId, TId lockedObjectId, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, TLockableObject>> fieldSelector,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<bool> RefreshLockAsync(TId baseObjId, TId lockedObjectId, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<bool> ReleaseLockAsync(TId baseObjId, TId lockedObjectId, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, TLockableObject>> fieldSelector,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<bool> ReleaseLockAsync(TId baseObjId, TId lockedObjectId, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<TBaseObject> GetLockedObjectAsync(TId baseObjId, TId lockedObjectId, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, TLockableObject>> fieldSelector,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<TBaseObject> GetLockedObjectAsync(TId baseObjId, TId lockedObjectId, Guid lockedObjectLockId,
            Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IDataStore<TLockableObject, in TId> where TLockableObject : ISharpLockable<TId>
    {
        ISharpLockLogger GetLogger();
        TimeSpan GetLockTime();

        Task<TLockableObject> AcquireLockAsync(TId baseObjId, TLockableObject obj, int staleLockMultiplier,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<bool> RefreshLockAsync(TId baseObjId, Guid lockedObjectLockId,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<bool> ReleaseLockAsync(TId baseObjId, Guid lockedObjectLockId,
            CancellationToken cancellationToken = default(CancellationToken));

        Task<TLockableObject> GetLockedObjectAsync(TId baseObjId, Guid lockedObjectLockId,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}