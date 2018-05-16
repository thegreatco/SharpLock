using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharpLock.InMemory
{
    public class InMemoryDataStore<TLockableObject> : IDataStore<TLockableObject, string>
        where TLockableObject : class, ISharpLockable<string>
    {
        private readonly InMemoryDataStore<TLockableObject, TLockableObject> _baseDataStore;

        public InMemoryDataStore(IEnumerable<TLockableObject> rawStore, ISharpLockLogger sharpLockLogger,
            TimeSpan lockTime)
        {
            _baseDataStore =
                new InMemoryDataStore<TLockableObject, TLockableObject>(rawStore, sharpLockLogger, lockTime);
        }

        public ISharpLockLogger GetLogger() => _baseDataStore.GetLogger();
        public TimeSpan GetLockTime() => _baseDataStore.GetLockTime();

        public Task<TLockableObject> AcquireLockAsync(string baseObjId, TLockableObject obj, int staleLockMultiplier,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _baseDataStore.AcquireLockAsync(baseObjId, obj, x => x, staleLockMultiplier, cancellationToken);
        }

        public Task<bool> RefreshLockAsync(string baseObjId, Guid lockedObjectLockId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _baseDataStore.RefreshLockAsync(baseObjId, baseObjId, lockedObjectLockId, x => x, cancellationToken);
        }

        public Task<bool> ReleaseLockAsync(string baseObjId, Guid lockedObjectLockId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _baseDataStore.ReleaseLockAsync(baseObjId, baseObjId, lockedObjectLockId, x => x, cancellationToken);
        }

        public Task<TLockableObject> GetLockedObjectAsync(string baseObjId, Guid lockedObjectLockId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _baseDataStore.GetLockedObjectAsync(baseObjId, baseObjId, lockedObjectLockId, x => x,
                cancellationToken);
        }
    }
}
