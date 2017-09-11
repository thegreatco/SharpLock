using System;
using System.Threading;
using System.Threading.Tasks;
using SharpLock.Exceptions;

namespace SharpLock
{
    public class DistributedLock<TLockableObject, TId> : IDisposable where TLockableObject : class, ISharpLockable<TId>
    {
        private readonly IDataStore<TLockableObject, TId> _store;
        private readonly ILogger _logger;
        private readonly CancellationToken _token;
        private readonly int _staleLockMultiplier;
        private Type _lockedObjectType;
        private TId _lockedObjectId;
        
        /// <summary>
        /// Gets a <see cref="bool"/> value indicating if the lock has been successfully acquired.
        /// </summary>
        public bool LockAcquired => LockedObject != null;

        /// <summary>
        /// Gets the length of time the lock is acquired for. The shorter this value, the more often <see cref="RefreshLockAsync"/> must be called.
        /// </summary>
        public readonly TimeSpan LockTime;

        /// <summary>
        /// Gets a the current view of the locked object. This is updated each time the lock is refreshed.
        /// </summary>
        public TLockableObject LockedObject;

        /// <summary>
        /// Gets a <see cref="bool"/> value indicating if this object has been disposed.
        /// </summary>
        public bool Disposed { get; set; }

        /// <summary>
        /// Creates a new instance of the DistributedLock class.
        /// </summary>
        /// <param name="store">The object store where the object exists.</param>
        /// <param name="staleLockMultiplier">A multiplier used to determine if a previously locked object is stale. Setting this value too short will result in one lock overwriting another.</param>
        public DistributedLock(IDataStore<TLockableObject, TId> store, int staleLockMultiplier = 10)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = _store.GetLogger();
            _token = _store.GetToken();
            LockTime = _store.GetLockTime();
            _staleLockMultiplier = staleLockMultiplier;
        }

        /// <summary>
        /// Asynchronously acquire a lock on the specified TLockableObject.
        /// This will wait <see cref="IDataStore{TLockableObject,TId}.GetLockTime"/> * <see cref="_staleLockMultiplier"/> to acquire the lock.
        /// To specified a timeout, use the appropriate overload.
        /// </summary>
        /// <param name="obj">The object to take a lock on.</param>
        /// <param name="throwOnFailure">Throw an exception if the acquisition fails.</param>
        /// <returns>A <see cref="bool"/> indicating if the lock attempt was successful.</returns>
        public Task<bool> AcquireLockAsync(TLockableObject obj, bool throwOnFailure = false)
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
        /// <returns>A <see cref="bool"/> indicating if the lock attempt was successful.</returns>
        public async Task<bool> AcquireLockAsync(TLockableObject obj, TimeSpan timeout, bool throwOnFailure = false)
        {
            var timeoutDate = DateTime.UtcNow.Add(timeout);
            _lockedObjectType = obj.GetType();
            _lockedObjectId = obj.Id;

            // TODO: Check if the supplied TLockableObject implements IEnumerable and that they supplied an IEnumerable<TLockableObject> selector.

            while (!LockAcquired && !_token.IsCancellationRequested && DateTime.UtcNow < timeoutDate)
            {
                var newLockObj = await _store.AcquireLockAsync(obj.Id, obj, _staleLockMultiplier, _token);
                
                // If the returned object is null, we need to set both the BaseObject and LockedObject
                LockedObject = newLockObj;

                if (!LockAcquired) await Task.Delay(TimeSpan.FromSeconds(1), _token);
            }

            _logger.Trace("Lock attempt complete on {Type} with LockId: {LockId} and LockTime: {LockTime}. Lock Acquired? {LockState}", _lockedObjectType, _lockedObjectId, LockedObject?.UpdateLock, LockAcquired);

            if (!LockAcquired && throwOnFailure)
                throw new AcquireDistributedLockException();

            return LockAcquired;
        }

        /// <summary>
        /// Refresh the lock on this instance of <see cref="SharpLockable{T}"/>
        /// </summary>
        /// <param name="throwOnFailure">A <see cref="bool"/> indicating if a failure to renew the lock should throw an exception.</param>
        /// <returns>A value indicating if the renewal of the lock was successful.</returns>
        public async Task<bool> RefreshLockAsync(bool throwOnFailure = false)
        {
            if (!LockAcquired) return false;

            // _logger.Debug("Attempting to refresh lock on {Type} with {Id}.", _lockedObjectType, _lockedObjectId);
            var baseObj = await _store.RefreshLockAsync(LockedObject.Id, LockedObject, _token);
            
            LockedObject = baseObj;
            _logger.Trace("Lock refresh complete on {Type} with {Id}. Lock Acquired? {LockState}", _lockedObjectType, _lockedObjectId, LockAcquired);

            if (!LockAcquired && throwOnFailure)
                throw new RefreshDistributedLockException("Failed to refresh lock.");
            return LockAcquired;
        }

        /// <summary>
        /// Release the acquired lock.
        /// </summary>
        /// <param name="throwOnFailure">A <see cref="bool"/> indicating if a failure to release the lock should throw an exception.</param>
        /// <returns>A value indicating if the release of the lock was successful.</returns>
        public async Task<bool> ReleaseLockAsync(bool throwOnFailure = false)
        {
            if (!LockAcquired) return true;

            //_logger.Debug("Attempting to release lock on {Type} with {Id}.", _lockedObjectType, _lockedObjectId);
            var baseObj = await _store.ReleaseLockAsync(LockedObject.Id, LockedObject, _token);

            if (baseObj == null)
            {
                LockedObject = null;
            }
            else
            {
                var lockedObject = baseObj;
                if (lockedObject.LockId == null && lockedObject.UpdateLock == null)
                {
                    LockedObject = null;
                }
            }
            
            _logger.Trace("Lock release complete on {Type} with {Id}. Lock Acquired? {LockState}", _lockedObjectType.ToString(), _lockedObjectId.ToString(), LockAcquired);
            
            if (LockAcquired && throwOnFailure)
                throw new ReleaseDistributedLockException("Failed to release lock.");

            return !LockAcquired;
        }

        /// <inheritdoc />
        /// <summary>
        /// Dispose of this current instance of <see cref="T:SharpLock.DistributedLock`2" />
        /// </summary>
        public void Dispose()
        {
            if (LockAcquired)
                ReleaseLockAsync().Wait(_token);
            Disposed = true;
        }

        public override string ToString()
        {
            return LockedObject != null ? $"LockId: {LockedObject.LockId}, Locked ObjectId: {LockedObject.Id}, Lock Expires: {LockedObject.UpdateLock.GetValueOrDefault(DateTime.MinValue):yyyy-MMM-ddThh:mm:ss.zzzz}." : "No lock acquired.";
        }
    }
}