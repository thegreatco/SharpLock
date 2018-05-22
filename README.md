This is the base package for implementations of SharpLock. This provides the interfaces and base types for distributed locking.

# Usage
## Creating a Lockable Object
Objects that need to be locked must implement the `ISharpLockable<TId>` interface. The `TId` is the unique identifier for the object within the `ISharpLockDataStore`.
## Creating a `ISharpLockDataStore`
This interface comes in 2 forms.
 - `ISharpLockDataStore<TLockableObject, TId>`
 - `ISharpLockDataStore<TBaseObject, TLockableObject, TId>`
 
This allows for locking both root objects within the data store and subobjects within objects in the data store. When the lockable object is not a root object in the data store, it must be accompanied by a field selector.
## Acquire a Lock
    ISharpLockLogger sharpLockLogger = new LoggingShim();
    var col = new List<LockBase>();
    var dataStore = new SharpLockInMemoryDataStore<LockBase>(col, _sharpLockLogger, TimeSpan.FromSeconds(30));
    using(var lck = new DistributedLock<LockBase, string>(dataStore))
    {
        var lockedObject = await lck.AcquireLockAsync(lockBase);
    }
    
## Refresh a Lock

    await lck.RefreshLockAsync();
    
## Explicitly Release a Lock
Locks can be released automatically by wrapping the creation of the lock in a `using` statement, or they can be explicitly released.

    await lck.ReleaseLockAsync();

# Options
### Lock Time
This is typically specified by the concrete implementation of the `ISharpLockDataStore`. It is the amount of time a lock should be held for on first acquisition. Locks should be refreshed within this time as they are not guaranteed beyond that.
### Stale Lock Multiplier

    new DistributedLock<LockBase, string>(dataStore, 5)
    
This multiplier is used to determine when a previous lock on the object is stale. The default should be 10x the lock time.

### Throw On Failure
Most methods contain a `throwOnFailure` parameter. By default, methods like `RefreshLockAsync` and `ReleaseLockAsync` return a `bool` indicating whether or not the operation was a success (`AcquireLockAsync` returns an object or `null`). Optionally, they can throw an exception instead.

### Logger
To be as flexible as possible and not requiring a particular logging framework, a shim must be implemented that implements the `ISharpLockLogger` interface. It follows similar patterns to `Serilog.ILogger` but is easily adapted to `Microsoft.Extensions.Logging` as well.