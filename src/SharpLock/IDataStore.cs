using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SharpLock
{
    public interface IDataStore<TBaseObject, TLockableObject>
    {
        ILogger GetLogger();
        CancellationToken GetToken();
        TimeSpan GetLockTime();
        Task<TBaseObject> AcquireLockAsync(TLockableObject obj, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, CancellationToken token);
        Task<TBaseObject> RefreshLockAsync(TLockableObject obj, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, CancellationToken token);
        Task<TBaseObject> ReleaseLockAsync(TLockableObject obj, Expression<Func<TBaseObject, TLockableObject>> fieldSelector, CancellationToken token);
    }
}