using System;
using System.Threading.Tasks;
using System.Linq;
using Serilog;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SharpLock.MongoDB.Tests
{
    [TestClass]
    public class RaceConditionTests
    {
        private ILogger _logger;
        private IMongoCollection<LockBase> _col;

        [TestInitialize]
        public async Task Setup()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .MinimumLevel.Verbose();

            Log.Logger = logConfig.CreateLogger();
            _logger = new LoggingShim(Log.Logger);

            var client = new MongoClient();
            var db = client.GetDatabase("Test");
            _col = db.GetCollection<LockBase>($"lockables.{GetType()}");
            await _col.DeleteManyAsync(Builders<LockBase>.Filter.Empty);
        }

        [TestMethod]
        public async Task AcquireManyBaseClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase>(_col, _logger, TimeSpan.FromSeconds(30));

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, ObjectId>(dataStore)).ToList();
            Log.Logger.Information(locks.Count.ToString());
            var lockStates = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, TimeSpan.FromSeconds(1))));
            
            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");
            
            lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));
            
            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));
            
            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");
            
            locks.ForEach(x => x.Dispose());
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireManySingularSubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, y => y.SingularInnerLock)).ToList();
            Log.Logger.Information(locks.Count.ToString());
            var lockStates = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.SingularInnerLock, TimeSpan.FromSeconds(1))));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            locks.ForEach(x => x.Dispose());
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireManyEnumerableSubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, y => y.EnumerableLockables)).ToList();
            Log.Logger.Information(locks.Count.ToString());
            var lockStates = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.EnumerableLockables.First(), TimeSpan.FromSeconds(1))));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            locks.ForEach(x => x.Dispose());
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireManyListSubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, y => y.ListOfLockables)).ToList();
            Log.Logger.Information(locks.Count.ToString());
            var lockStates = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.ListOfLockables[0], TimeSpan.FromSeconds(1))));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            locks.ForEach(x => x.Dispose());
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }

        [TestMethod]
        public async Task AcquireManyArraySubClassAsync()
        {
            var lockBase = new LockBase();
            await _col.InsertOneAsync(lockBase);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(_col, _logger, TimeSpan.FromSeconds(30));

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, y => y.ArrayOfLockables)).ToList();
            Log.Logger.Information(locks.Count.ToString());
            var lockStates = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.ArrayOfLockables[1], TimeSpan.FromSeconds(1))));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to acquire lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync()));

            Assert.IsFalse(lockStates.Count(x => x) < 1, "Failed to refresh lock.");
            Assert.IsFalse(lockStates.Count(x => x) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.IsTrue(lockStates.Count(x => x) == locks.Count, "Failed to release lock.");
            Assert.IsTrue(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            locks.ForEach(x => x.Dispose());
            Assert.IsTrue(locks.Count(x => x.Disposed) == locks.Count, "Failed to mark object as disposed");
        }
    }
}