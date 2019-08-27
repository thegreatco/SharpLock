using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SharpLock.Exceptions;

namespace SharpLock
{
    public class DistributedLock<TLockableObject, TId> : IDisposable where TLockableObject : class, ISharpLockable<TId>
    {
        private readonly ISharpLockDataStore<TLockableObject, TId> _store;
        private readonly ISharpLockLogger _sharpLockLogger;
        private readonly int _staleLockMultiplier;
        private Type _lockedObjectType;
        private TId _lockedObjectId;

        /// <summary>
        /// Gets a <see cref="bool"/> value indicating if the lock has been successfully acquired.
        /// </summary>
        public bool LockAcquired => LockedObjectLockId != null;

        /// <summary>
        /// Gets the length of time the lock is acquired for. The shorter this value, the more often <see cref="RefreshLockAsync"/> must be called.
        /// </summary>
        public readonly TimeSpan LockTime;

        /// <summary>
        /// Gets a the current view of the locked object. This is updated each time the lock is refreshed.
        /// </summary>
        public TId LockedObjectId;

        public Guid? LockedObjectLockId;

        /// <summary>
        /// Gets a <see cref="bool"/> value indicating if this object has been disposed.
        /// </summary>
        public bool Disposed { get; set; }

        /// <summary>
        /// Creates a new instance of the DistributedLock class.
        /// </summary>
        /// <param name="store">The object store where the object exists.</param>
        /// <param name="staleLockMultiplier">A multiplier used to determine if a previously locked object is stale. Setting this value too short will result in one lock overwriting another.</param>
        public DistributedLock(ISharpLockDataStore<TLockableObject, TId> store, int staleLockMultiplier = 10)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _sharpLockLogger = _store.GetLogger();
            LockTime = _store.GetLockTime();
            _staleLockMultiplier = staleLockMultiplier;
        }

        /// <summary>
        /// Asynchronously acquire a lock on the specified TLockableObject.
        /// This will wait <see cref="ISharpLockDataStore{TLockableObject,TId}.GetLockTime"/> * <see cref="_staleLockMultiplier"/> to acquire the lock.
        /// To specified a timeout, use the appropriate overload.
        /// </summary>
        /// <param name="obj">The object to take a lock on.</param>
        /// <param name="throwOnFailure">Throw an exception if the acquisition fails.</param>
        /// <returns>A <see cref="bool"/> indicating if the lock attempt was successful.</returns>
        public Task<TLockableObject> AcquireLockAsync(TLockableObject obj, bool throwOnFailure = false)
        {
            var timeout = TimeSpan.FromMilliseconds(_store.GetLockTime().TotalMilliseconds * (_staleLockMultiplier + 1));
            return AcquireLockAsync(obj, timeout, throwOnFailure);
        }

        /// <summary>
        /// Asynchronously acquire a lock on the specified TLockableObject. 
        /// This will wait the specified amount of time before either throwing an exception (if throwOnFailure is true) or retuning false.
        /// </summary>
        /// <param name="obj">The object to take a lock on.</param>
        /// <param name="timeout">The amount of time to spend attempting to acquire the lock.</param>
        /// <param name="throwOnFailure">Throw an exception if the acquisition fails.</param>
        /// <param name="cancellationToken">The cancellation token to use during lock acquisition.</param>
        /// <returns>A <see cref="bool"/> indicating if the lock attempt was successful.</returns>
        public async Task<TLockableObject> AcquireLockAsync(TLockableObject obj, TimeSpan timeout, bool throwOnFailure = false, CancellationToken cancellationToken = default)
        {
            var timeoutDate = DateTime.UtcNow.Add(timeout);
            _lockedObjectType = obj.GetType();
            _lockedObjectId = obj.Id;

            // TODO: Check if the supplied TLockableObject implements IEnumerable and that they supplied an IEnumerable<TLockableObject> selector.
            TLockableObject lockedObj = null;
            while (lockedObj == null && !cancellationToken.IsCancellationRequested && DateTime.UtcNow < timeoutDate)
            {
                lockedObj = await _store.AcquireLockAsync(obj.Id, obj, _staleLockMultiplier, cancellationToken).ConfigureAwait(false);
                
                if (lockedObj == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    LockedObjectLockId = lockedObj.LockId;
                    LockedObjectId = lockedObj.Id;
                }
            }

            _sharpLockLogger.Trace("Lock attempt complete on {0} with LockId: {1}. Lock Acquired? {2}", _lockedObjectType, _lockedObjectId, LockAcquired);

            if (lockedObj == null && throwOnFailure)
                throw new AcquireDistributedLockException("Failed to acquire lock.");

            return lockedObj;
        }

        /// <summary>
        /// Refresh the lock on this instance of <see cref="TLockableObject"/>
        /// </summary>
        /// <param name="throwOnFailure">A <see cref="bool"/> indicating if a failure to renew the lock should throw an exception.</param>
        /// <returns>A value indicating if the renewal of the lock was successful.</returns>
        public async Task<bool> RefreshLockAsync(bool throwOnFailure = false, CancellationToken cancellationToken = default)
        {
            if (!LockAcquired)
            {
                if (throwOnFailure) throw new RefreshDistributedLockException("No lock is acquired.");
                return false;
            }

            Debug.Assert(LockedObjectLockId != null, nameof(LockedObjectLockId) + " != null");
            var lockRefreshed = await _store.RefreshLockAsync(LockedObjectId, LockedObjectLockId.Value, cancellationToken).ConfigureAwait(false);

            if (lockRefreshed == false)
            {
                LockedObjectLockId = null;
                LockedObjectId = default;
            }

            _sharpLockLogger.Trace("Lock refresh complete on {0} with {1}. Lock Acquired? {2}", _lockedObjectType, _lockedObjectId, LockAcquired);

            if (!LockAcquired && throwOnFailure)
                throw new RefreshDistributedLockException("Failed to refresh lock.");
            return LockAcquired;
        }

        /// <summary>
        /// Release the acquired lock.
        /// </summary>
        /// <param name="throwOnFailure">A <see cref="bool"/> indicating if a failure to release the lock should throw an exception.</param>
        /// <returns>A value indicating if the release of the lock was successful.</returns>
        public async Task<bool> ReleaseLockAsync(bool throwOnFailure = false, CancellationToken cancellationToken = default)
        {
            if (!LockAcquired)
            {
                if (throwOnFailure) throw new ReleaseDistributedLockException("No lock is acquired.");
                return true;
            }

            Debug.Assert(LockedObjectLockId != null, nameof(LockedObjectLockId) + " != null");
            var lockReleased = await _store.ReleaseLockAsync(LockedObjectId, LockedObjectLockId.Value, cancellationToken).ConfigureAwait(false);

            if (lockReleased)
            {
                LockedObjectLockId = null;
                LockedObjectId = default;
            }

            _sharpLockLogger.Trace("Lock release complete on {0} with {1}. Lock Acquired? {2}", _lockedObjectType.ToString(), _lockedObjectId.ToString(), LockAcquired);
            
            if (LockAcquired && throwOnFailure)
                throw new ReleaseDistributedLockException("Failed to release lock.");

            return !LockAcquired;
        }

        /// <summary>
        /// Get the latest copy of the locked object.
        /// </summary>
        /// <param name="throwOnFailure">A <see cref="bool"/> indicating if a failure to release the lock should throw an exception.</param>
        /// <returns>An instance of the base object.</returns>
        public async Task<TLockableObject> GetObjectAsync(bool throwOnFailure = false, CancellationToken cancellationToken = default)
        {
            if (!LockAcquired)
            {
                if (throwOnFailure) throw new DistributedLockException("No lock is acquired.");
                return null;
            }

            Debug.Assert(LockedObjectLockId != null, nameof(LockedObjectLockId) + " != null");
            return await _store.GetLockedObjectAsync(LockedObjectId, LockedObjectLockId.Value, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <summary>
        /// Dispose of this current instance of <see cref="T:SharpLock.DistributedLock`2" />
        /// </summary>
        public void Dispose()
        {
            if (LockAcquired)
                Disposed = ReleaseLockAsync().Result;
            Disposed = true;
        }

        public override string ToString()
        {
            return LockAcquired ? $"LockId: {LockedObjectLockId}, Locked ObjectId: {LockedObjectId}." : "No lock acquired.";
        }
    }
}