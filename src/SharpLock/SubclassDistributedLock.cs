using System.Threading;
using System;
using Serilog;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SharpLock
{
    public class DistributedLock<TBaseObject, TLockableObject, TId> : IDisposable where TLockableObject : SharpLockable<TId> where TBaseObject : class
    {
        private readonly IDataStore<TBaseObject, TLockableObject> _store;
        private readonly ILogger _logger;
        private readonly CancellationToken _token;
        private readonly Expression<Func<TBaseObject, TLockableObject>> _fieldSelector;
        private Type _lockedObjectType;
        private TId _lockedObjectId;
        public bool LockAcquired => LockedObject != null;
        public readonly TimeSpan LockTime;
        public TLockableObject LockedObject;
        public TBaseObject BaseObject;
        public bool Disposed { get; set; } = false;

        public DistributedLock(IDataStore<TBaseObject, TLockableObject> store, Expression<Func<TBaseObject, TLockableObject>> fieldSelector)
        {
            _store = store;
            _logger = _store.GetLogger();
            _token = _store.GetToken();
            LockTime = _store.GetLockTime();
            _fieldSelector = fieldSelector;
        }

        public Task<bool> AcquireLockAsync(TLockableObject obj, bool throwOnFailure = false)
        {
            return AcquireLockAsync(obj, TimeSpan.FromHours(1), throwOnFailure);
        }

        public async Task<bool> AcquireLockAsync(TLockableObject obj, TimeSpan timeout, bool throwOnFailure = false)
        {
            var timeoutDate = DateTime.UtcNow.Add(timeout);
            _lockedObjectType = obj.GetType();
            _lockedObjectId = obj.Id;

            while (!LockAcquired && !_token.IsCancellationRequested && DateTime.UtcNow < timeoutDate)
            {
                var lockTime = DateTime.UtcNow.Add(LockTime).Ticks;
                // _logger.Debug("Attempting to acquire lock on {Type} with {Id}.", _lockedObjectType, _lockedObjectId);
                var baseObj = await _store.AcquireLockAsync(obj, _fieldSelector, _token);
                
                // If the returned object is null, we need to set both the BaseObject and LockedObject
                if (baseObj == null)
                {
                    LockedObject = null;
                    BaseObject = null;
                }
                else
                {
                    BaseObject = baseObj;
                    LockedObject = _fieldSelector.Compile().Invoke(baseObj);
                }

                if (!LockAcquired) await Task.Delay(TimeSpan.FromSeconds(1), _token);
            }

            _logger.Debug("Lock attempt complete on {Type} with Id: {Id} and LockTime: {LockTime}. Lock Acquired? {LockState}", _lockedObjectType, _lockedObjectId, LockedObject?.UpdateLock, LockAcquired);

            if (!LockAcquired && throwOnFailure)
                throw new AcquireDistributedLockException();

            return LockAcquired;
        }

        public async Task<bool> RefreshLockAsync(bool throwOnFailure = false)
        {
            // _logger.Debug("Attempting to refresh lock on {Type} with {Id}.", _lockedObjectType, _lockedObjectId);
            var baseObj = await _store.RefreshLockAsync(LockedObject, _fieldSelector, _token);
            if (baseObj == null)
            {
                LockedObject = null;
                BaseObject = null;
            }
            else
            {
                BaseObject = baseObj;
                LockedObject = _fieldSelector.Compile().Invoke(baseObj);
            }
            _logger.Debug("Lock refresh complete on {Type} with {Id}. Lock Acquired? {LockState}", _lockedObjectType, _lockedObjectId, LockAcquired);

            if (!LockAcquired && throwOnFailure)
                throw new RefreshDistributedLockException("Failed to refresh lock.");
            return LockAcquired;
        }

        public async Task<bool> ReleaseLockAsync(bool throwOnFailure = false)
        {
            if (!LockAcquired) return true;
            //_logger.Debug("Attempting to release lock on {Type} with {Id}.", _lockedObjectType, _lockedObjectId);
            var baseObj = await _store.ReleaseLockAsync(LockedObject, _fieldSelector, _token);
            if (baseObj == null)
            {
                BaseObject = null;
                LockedObject = null;
            }
            else
            {
                var lockedObject = _fieldSelector.Compile().Invoke(baseObj);
                if (lockedObject == null)
                {
                    BaseObject = null;
                    LockedObject = null;
                }
            }
            
            _logger.Debug("Lock release complete on {Type} with {Id}. Lock Acquired? {LockState}", _lockedObjectType.ToString(), _lockedObjectId.ToString(), LockAcquired);
            
            if (!LockAcquired && throwOnFailure)
                throw new ReleaseDistributedLockException("Failed to release lock.");

            return LockAcquired;
        }

        public void Dispose()
        {
            ReleaseLockAsync().Wait(_token);
            Disposed = true;
        }
    }
}
