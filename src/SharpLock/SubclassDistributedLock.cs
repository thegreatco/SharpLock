using System.Threading;
using System;
using Serilog;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace SharpLock
{
    public class DistributedLock<TBaseObject, TLockableObject, TId> : IDisposable where TLockableObject : SharpLockable<TId> where TBaseObject : SharpLockableBase<TId>
    {
        private readonly IDataStore<TBaseObject, TLockableObject, TId> _store;
        private readonly ILogger _logger;
        private readonly CancellationToken _token;
        private readonly Expression<Func<TBaseObject, TLockableObject>> _tLockableObjectFieldSelector;
        private readonly Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> _tLockableObjectArrayFieldSelector;
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
        /// Gets the base object of the locked object. This may also be the same as <seealso cref="LockedObject"/>.
        /// </summary>
        public TBaseObject BaseObject;

        /// <summary>
        /// Gets a <see cref="bool"/> value indicating if this object has been disposed.
        /// </summary>
        public bool Disposed { get; set; } = false;

        internal DistributedLock(IDataStore<TBaseObject, TLockableObject, TId> store, int staleLockMultiplier)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));

            _store = store;
            _logger = _store.GetLogger();
            _token = _store.GetToken();
            LockTime = _store.GetLockTime();
            _staleLockMultiplier = staleLockMultiplier;
        }

        /// <summary>
        /// Creates a new instance of the DistributedLock class.
        /// </summary>
        /// <param name="store">The object store where the object exists.</param>
        /// <param name="fieldSelector">A LINQ expression giving the path to the <seealso cref="TLockableObject"/></param>
        /// <param name="staleLockMultiplier">A multiplier used to determine if a previously locked object is stale. Setting this value too short will result in one lock overwriting another.</param>
        public DistributedLock(IDataStore<TBaseObject, TLockableObject, TId> store, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, int staleLockMultiplier = 5)
            : this(store, staleLockMultiplier)
        {
            if (fieldSelector == null) throw new ArgumentNullException(nameof(fieldSelector));

            _tLockableObjectFieldSelector = fieldSelector;
        }

        /// <summary>
        /// Creates a new instance of the DistributedLock class.
        /// </summary>
        /// <param name="store">The object store where the object exists.</param>
        /// <param name="fieldSelector">A LINQ expression giving the path to the <seealso cref="TLockableObject"/></param>
        /// <param name="staleLockMultiplier">A multiplier used to determine if a previously locked object is stale. Setting this value too short will result in one lock overwriting another.</param>
        public DistributedLock(IDataStore<TBaseObject, TLockableObject, TId> store, Expression<Func<TBaseObject, IEnumerable<TLockableObject>>> fieldSelector, int staleLockMultiplier = 5)
            : this(store, staleLockMultiplier)
        {
            if (fieldSelector == null) throw new ArgumentNullException(nameof(fieldSelector));

            _tLockableObjectArrayFieldSelector = fieldSelector;
        }

        /// <summary>
        /// Asynchronously acquire a lock on the specified TLockableObject.
        /// This will wait <see cref="IDataStore.GetLockTime"/> / <see cref="staleLockMultiplier"> to acquire the lock.
        /// To specified a timeout, use the appropriate overload.
        /// </summary>
        /// <param name="obj">The object to take a lock on.</param>
        /// <param name="throwOnFailure">Throw an exception if the acquisition fails.</param>
        /// <returns>A <see cref="bool"/> indicating if the lock attempt was successful.</returns>
        public Task<bool> AcquireLockAsync(TBaseObject baseObj, TLockableObject obj, bool throwOnFailure = false)
        {
            var timeout = TimeSpan.FromMilliseconds(_store.GetLockTime().TotalMilliseconds / _staleLockMultiplier);
            return AcquireLockAsync(baseObj, obj, timeout, throwOnFailure);
        }

        /// <summary>
        /// Asynchronously acquire a lock on the specified TLockableObject. 
        /// This will wait the specified amount of time before either throwing an exception (if throwOnFailure is true) or retuning false.
        /// </summary>
        /// <param name="obj">The object to take a lock on.</param>
        /// <param name="throwOnFailure">Throw an exception if the acquisition fails.</param>
        /// <returns>A <see cref="bool"/> indicating if the lock attempt was successful.</returns>
        public async Task<bool> AcquireLockAsync(TBaseObject baseObj, TLockableObject obj, TimeSpan timeout, bool throwOnFailure = false)
        {
            var timeoutDate = DateTime.UtcNow.Add(timeout);
            _lockedObjectType = obj.GetType();
            _lockedObjectId = obj.Id;

            // TODO: Check if the supplied TLockableObject implements IEnumerable and that they supplied an IEnumerable<TLockableObject> selector.

            while (!LockAcquired && !_token.IsCancellationRequested && DateTime.UtcNow < timeoutDate)
            {
                var lockTime = DateTime.UtcNow.Add(LockTime).Ticks;
                // _logger.Debug("Attempting to acquire lock on {Type} with {Id}.", _lockedObjectType, _lockedObjectId);
                
                TBaseObject newBaseObj = null;
                if (_tLockableObjectFieldSelector != null)
                    newBaseObj = await _store.AcquireLockAsync(baseObj.Id, obj, _tLockableObjectFieldSelector, _staleLockMultiplier, _token);
                else if (_tLockableObjectArrayFieldSelector != null)
                    newBaseObj = await _store.AcquireLockAsync(baseObj.Id, obj, _tLockableObjectArrayFieldSelector, _staleLockMultiplier, _token);
                else
                    throw new AcquireDistributedLockException("No fieldSelector found.");
                
                // If the returned object is null, we need to set both the BaseObject and LockedObject
                if (newBaseObj == null)
                {
                    LockedObject = null;
                    BaseObject = null;
                }
                else
                {
                    BaseObject = newBaseObj;
                    LockedObject = GetLockableObjectFromBaseObject(newBaseObj);
                }

                if (!LockAcquired) await Task.Delay(TimeSpan.FromSeconds(1), _token);
            }

            _logger.Debug("Lock attempt complete on {Type} with LockId: {LockId} and LockTime: {LockTime}. Lock Acquired? {LockState}", _lockedObjectType, _lockedObjectId, LockedObject?.UpdateLock, LockAcquired);

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
            TBaseObject baseObj = null;
            if (_tLockableObjectFieldSelector != null)
                baseObj = await _store.RefreshLockAsync(BaseObject.Id, LockedObject, _tLockableObjectFieldSelector, _token);
            else if (_tLockableObjectArrayFieldSelector != null)
                baseObj = await _store.RefreshLockAsync(BaseObject.Id, LockedObject, _tLockableObjectArrayFieldSelector, _token);
            else
                throw new AcquireDistributedLockException("No fieldSelector found.");
            
            if (baseObj == null)
            {
                LockedObject = null;
                BaseObject = null;
            }
            else
            {
                BaseObject = baseObj;
                LockedObject = GetLockableObjectFromBaseObject(baseObj);
            }
            _logger.Debug("Lock refresh complete on {Type} with {Id}. Lock Acquired? {LockState}", _lockedObjectType, _lockedObjectId, LockAcquired);

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
            TBaseObject baseObj = null;
            if (_tLockableObjectFieldSelector != null)
                baseObj = await _store.ReleaseLockAsync(BaseObject.Id, LockedObject, _tLockableObjectFieldSelector, _token);
            else if (_tLockableObjectArrayFieldSelector != null)
                baseObj = await _store.ReleaseLockAsync(BaseObject.Id, LockedObject, _tLockableObjectArrayFieldSelector, _token);
            else
                throw new AcquireDistributedLockException("No fieldSelector found.");

            if (baseObj == null)
            {
                BaseObject = null;
                LockedObject = null;
            }
            else
            {
                var lockedObject = GetLockableObjectFromBaseObject(baseObj);
                if (lockedObject != null && lockedObject.LockId == null && lockedObject.UpdateLock == null)
                {
                    BaseObject = null;
                    LockedObject = null;
                }
            }
            
            _logger.Debug("Lock release complete on {Type} with {Id}. Lock Acquired? {LockState}", _lockedObjectType.ToString(), _lockedObjectId.ToString(), LockAcquired);
            
            if (LockAcquired && throwOnFailure)
                throw new ReleaseDistributedLockException("Failed to release lock.");

            return !LockAcquired;
        }

        /// <summary>
        /// Dispose of this current instance of <see cref="DistributedLock{TLockableObject,TId}"/>
        /// </summary>
        public void Dispose()
        {
            if (LockAcquired)
                ReleaseLockAsync().Wait(_token);
            Disposed = true;
        }

        public override string ToString()
        {
            if (LockedObject != null)
                return $"LockId: {LockedObject.LockId}, Locked ObjectId: {LockedObject.Id}, Lock Expires: {new DateTime(LockedObject.UpdateLock.GetValueOrDefault(0)).ToString("yyyy-MMM-ddThh:mm:ss.zzzz")}.";
            else
                return "No lock acquired.";
        }

        private TLockableObject GetLockableObjectFromBaseObject(TBaseObject baseObj)
        {
            if (_tLockableObjectFieldSelector != null)
                return _tLockableObjectFieldSelector.Compile().Invoke(baseObj);
            else if (_tLockableObjectArrayFieldSelector != null)
                return (_tLockableObjectArrayFieldSelector.Compile().Invoke(baseObj) as IEnumerable<TLockableObject>).SingleOrDefault(x => x.Id.Equals(_lockedObjectId));
            else
                throw new DistributedLockException($"No suitable selector found to get {typeof(TLockableObject)} from {typeof(TBaseObject)}");
        }
    }
}
