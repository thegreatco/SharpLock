using System.Threading;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharpLock.Exceptions;

namespace SharpLock
{
    public class DistributedLock<TBaseObject, TLockableObject, TId> : IDisposable where TLockableObject : class, ISharpLockable<TId> where TBaseObject : class, ISharpLockableBase<TId>
    {
        private readonly ISharpLockDataStore<TBaseObject, TLockableObject, TId> _store;
        private readonly ISharpLockLogger _sharpLockLogger;
        private readonly Expression<Func<TBaseObject, TLockableObject>> _tLockableObjectFieldSelector;
        private readonly Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> _tLockableObjectArrayFieldSelector;
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
        /// Gets the current view of the locked object id. This is updated each time the lock is refreshed.
        /// </summary>
        public TId LockedObjectId { get; private set; }

        /// <summary>
        /// Gets the current Id of the lock taken on the object.
        /// </summary>
        public Guid? LockedObjectLockId { get; private set; }

        /// <summary>
        /// Gets the base object of the locked object. This may also be the same as <seealso cref="LockedObjectId"/>.
        /// </summary>
        public TId BaseObjectId { get; private set; }

        /// <summary>
        /// Gets a <see cref="bool"/> value indicating if this object has been disposed.
        /// </summary>
        public bool Disposed { get; set; }

        internal DistributedLock(ISharpLockDataStore<TBaseObject, TLockableObject, TId> store, int staleLockMultiplier)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _sharpLockLogger = _store.GetLogger();
            LockTime = _store.GetLockTime();
            _staleLockMultiplier = staleLockMultiplier;
        }

        /// <summary>
        /// Creates a new instance of the DistributedLock class.
        /// </summary>
        /// <param name="store">The object store where the object exists.</param>
        /// <param name="fieldSelector">A LINQ expression giving the path to the <seealso cref="TLockableObject"/></param>
        /// <param name="staleLockMultiplier">A multiplier used to determine if a previously locked object is stale. Setting this value too short will result in one lock overwriting another.</param>
        public DistributedLock(ISharpLockDataStore<TBaseObject, TLockableObject, TId> store, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, int staleLockMultiplier = 5)
            : this(store, staleLockMultiplier)
        {
            _tLockableObjectFieldSelector = fieldSelector ?? throw new ArgumentNullException(nameof(fieldSelector));
        }

        /// <summary>
        /// Creates a new instance of the DistributedLock class.
        /// </summary>
        /// <param name="store">The object store where the object exists.</param>
        /// <param name="fieldSelector">A LINQ expression giving the path to the <seealso cref="TLockableObject"/></param>
        /// <param name="staleLockMultiplier">A multiplier used to determine if a previously locked object is stale. Setting this value too short will result in one lock overwriting another.</param>
        public DistributedLock(ISharpLockDataStore<TBaseObject, TLockableObject, TId> store, Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, int staleLockMultiplier = 5)
            : this(store, staleLockMultiplier)
        {
            _tLockableObjectArrayFieldSelector = fieldSelector ?? throw new ArgumentNullException(nameof(fieldSelector));
        }

        /// <summary>
        /// Asynchronously acquire a lock on the specified TLockableObject.
        /// This will wait <see cref="ISharpLockDataStore{TBaseObject,TLockableObject,TId}.GetLockTime"/> / <see cref="_staleLockMultiplier"/> to acquire the lock.
        /// To specified a timeout, use the appropriate overload.
        /// </summary>
        /// <param name="obj">The object to take a lock on.</param>
        /// <param name="baseObj">The object that contains the lockable object.</param>
        /// <param name="throwOnFailure">Throw an exception if the acquisition fails.</param>
        /// <returns>A <see cref="bool"/> indicating if the lock attempt was successful.</returns>
        public Task<TBaseObject> AcquireLockAsync(TBaseObject baseObj, TLockableObject obj, bool throwOnFailure = false)
        {
            var timeout = TimeSpan.FromMilliseconds(_store.GetLockTime().TotalMilliseconds / _staleLockMultiplier);
            return AcquireLockAsync(baseObj, obj, timeout, throwOnFailure);
        }

        /// <summary>
        /// Asynchronously acquire a lock on the specified TLockableObject. 
        /// This will wait the specified amount of time before either throwing an exception (if throwOnFailure is true) or retuning false.
        /// </summary>
        /// <param name="obj">The object to take a lock on.</param>
        /// <param name="baseObj">The object that contains the lockable object.</param>
        /// <param name="timeout">The amount of time to wait to acquire the lock.</param>
        /// <param name="throwOnFailure">Throw an exception if the acquisition fails.</param>
        /// <returns>A <see cref="bool"/> indicating if the lock attempt was successful.</returns>
        public async Task<TBaseObject> AcquireLockAsync(TBaseObject baseObj, TLockableObject obj, TimeSpan timeout, bool throwOnFailure = false, CancellationToken cancellationToken = default)
        {
            if (baseObj == null) throw new ArgumentNullException(nameof(baseObj), "Base Object cannot be null");
            if (obj == null) throw new ArgumentNullException(nameof(obj), "Lockable Object cannot be null");
            var timeoutDate = DateTime.UtcNow.Add(timeout);
            _lockedObjectType = obj.GetType();
            _lockedObjectId = obj.Id;

            // TODO: Check if the supplied TLockableObject implements IEnumerable and that they supplied an IEnumerable<TLockableObject> selector.
            TBaseObject newBaseObj = null;
            while (newBaseObj == null && !cancellationToken.IsCancellationRequested && DateTime.UtcNow < timeoutDate)
            {
                if (_tLockableObjectFieldSelector != null)
                    newBaseObj = await _store.AcquireLockAsync(baseObj.Id, obj, _tLockableObjectFieldSelector, _staleLockMultiplier, cancellationToken).ConfigureAwait(false);
                else if (_tLockableObjectArrayFieldSelector != null)
                    newBaseObj = await _store.AcquireLockAsync(baseObj.Id, obj, _tLockableObjectArrayFieldSelector, _staleLockMultiplier, cancellationToken).ConfigureAwait(false);
                else
                    throw new AcquireDistributedLockException("No fieldSelector found.");
                
                // If the returned object is null, we need to set both the BaseObject and LockedObject
                if (newBaseObj == null)
                {
                    LockedObjectId = default;
                    BaseObjectId = default;
                    LockedObjectLockId = null;
                }
                else
                {
                    BaseObjectId = newBaseObj.Id;
                    var lockedObject = GetLockableObjectFromBaseObject(newBaseObj);
                    LockedObjectLockId = lockedObject.LockId;
                    LockedObjectId = lockedObject.Id;
                }

                if (!LockAcquired) 
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }

            _sharpLockLogger.Debug("Lock attempt complete on {0} with LockId: {1}. Lock Acquired? {2}", _lockedObjectType, _lockedObjectId, LockAcquired);

            if (!LockAcquired && throwOnFailure)
                throw new AcquireDistributedLockException("Failed to acquire lock.");

            return newBaseObj;
        }

        /// <summary>
        /// Refresh the lock on this instance of <see cref="SharpLockable{T}"/>
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

            bool lockRefreshed;
            if (_tLockableObjectFieldSelector != null)
                lockRefreshed = await _store.RefreshLockAsync(BaseObjectId, LockedObjectId, LockedObjectLockId.Value, _tLockableObjectFieldSelector, cancellationToken).ConfigureAwait(false);
            else if (_tLockableObjectArrayFieldSelector != null)
                lockRefreshed = await _store.RefreshLockAsync(BaseObjectId, LockedObjectId, LockedObjectLockId.Value, _tLockableObjectArrayFieldSelector, cancellationToken).ConfigureAwait(false);
            else
                throw new RefreshDistributedLockException("No fieldSelector found.");
            
            if (lockRefreshed == false)
            {
                LockedObjectId = default;
                LockedObjectLockId = null;
                BaseObjectId = default;
            }
            _sharpLockLogger.Debug("Lock refresh complete on {0} with {1}. Lock Acquired? {2}", _lockedObjectType, _lockedObjectId, LockAcquired);

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

            bool lockReleased;
            if (_tLockableObjectFieldSelector != null)
                lockReleased = await _store.ReleaseLockAsync(BaseObjectId, LockedObjectId, LockedObjectLockId.Value, _tLockableObjectFieldSelector, cancellationToken).ConfigureAwait(false);
            else if (_tLockableObjectArrayFieldSelector != null)
                lockReleased = await _store.ReleaseLockAsync(BaseObjectId, LockedObjectId, LockedObjectLockId.Value, _tLockableObjectArrayFieldSelector, cancellationToken).ConfigureAwait(false);
            else
                throw new ReleaseDistributedLockException("No fieldSelector found.");

            if (lockReleased)
            {
                BaseObjectId = default;
                LockedObjectId = default;
                LockedObjectLockId = null;
            }
            
            _sharpLockLogger.Debug("Lock release complete on {0} with {1}. Lock Acquired? {2}", _lockedObjectType.ToString(), _lockedObjectId.ToString(), LockAcquired);
            
            if (LockAcquired && throwOnFailure)
                throw new ReleaseDistributedLockException("Failed to release lock.");

            return !LockAcquired;
        }

        /// <summary>
        /// Get the latest copy of the base object containing the locked object.
        /// </summary>
        /// <param name="throwOnFailure">A <see cref="bool"/> indicating if a failure to release the lock should throw an exception.</param>
        /// <returns>An instance of the base object.</returns>
        public async Task<TBaseObject> GetObjectAsync(bool throwOnFailure = false, CancellationToken cancellationToken = default)
        {
            if (!LockAcquired)
            {
                if (throwOnFailure) throw new DistributedLockException("No lock is acquired.");
                return null;
            }

            Debug.Assert(LockedObjectLockId != null, nameof(LockedObjectLockId) + " != null");

            if (_tLockableObjectFieldSelector != null)
                return await _store.GetLockedObjectAsync(BaseObjectId, LockedObjectId, LockedObjectLockId.Value, _tLockableObjectFieldSelector, cancellationToken).ConfigureAwait(false);
            if (_tLockableObjectArrayFieldSelector != null)
                return await _store.GetLockedObjectAsync(BaseObjectId, LockedObjectId, LockedObjectLockId.Value, _tLockableObjectArrayFieldSelector, cancellationToken).ConfigureAwait(false);
            
            throw new DistributedLockException("No fieldSelector found.");
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

        private TLockableObject GetLockableObjectFromBaseObject(TBaseObject baseObj)
        {
            if (_tLockableObjectFieldSelector != null)
                return _tLockableObjectFieldSelector.Compile().Invoke(baseObj);
            if (_tLockableObjectArrayFieldSelector != null)
                return _tLockableObjectArrayFieldSelector.Compile().Invoke(baseObj).SingleOrDefault(x => x.Id.Equals(_lockedObjectId));
            throw new DistributedLockException($"No suitable selector found to get {typeof(TLockableObject)} from {typeof(TBaseObject)}");
        }
    }
}
