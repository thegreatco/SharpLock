using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Serilog;
using Serilog.Events;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Diagnostics;

namespace SharpLock.MongoDB.Tests
{
    public class RaceConditionTests
    {
        [Fact]
        public async Task AcquireManyBaseClassAsync()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.LiterateConsole(LogEventLevel.Verbose)
                .MinimumLevel.Verbose();
				
            Log.Logger = logConfig.CreateLogger();

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var client = new MongoClient();
            var db = client.GetDatabase("Test");
            var col = db.GetCollection<LockBase>($"lockables.{GetType()}");
            await col.DeleteManyAsync(Builders<LockBase>.Filter.Empty, token);
            var lockBase = new LockBase();
            await col.InsertOneAsync(lockBase, null, token);
            var dataStore = new MongoDataStore<LockBase>(col, Log.Logger, TimeSpan.FromSeconds(30), cts.Token);

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, ObjectId>(dataStore)).ToList();
            Log.Logger.Information(locks.Count().ToString());
            var lockStates = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase, TimeSpan.FromSeconds(1), false)));
            
            Assert.False(lockStates.Count(x => x == true) < 1, "Failed to acquire lock.");
            Assert.False(lockStates.Count(x => x == true) > 1, "Acquired multiple locks.");
            
            lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync(false)));
            
            Assert.False(lockStates.Count(x => x == true) < 1, "Failed to refresh lock.");
            Assert.False(lockStates.Count(x => x == true) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));
            
            Assert.True(lockStates.Count(x => x == true) == locks.Count(), "Failed to release lock.");
            Assert.True(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");
            
            locks.ForEach(x => x.Dispose());
            Assert.True(locks.Count(x => x.Disposed) == locks.Count(), "Failed to mark object as disposed");
        }

        [Fact]
        public async Task AcquireManySingularSubClassAsync()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.LiterateConsole(LogEventLevel.Verbose)
                .MinimumLevel.Verbose();

            Log.Logger = logConfig.CreateLogger();

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var client = new MongoClient();
            var db = client.GetDatabase("Test");
            var col = db.GetCollection<LockBase>($"lockables.{GetType()}");
            await col.DeleteManyAsync(Builders<LockBase>.Filter.Empty, token);
            var lockBase = new LockBase();
            await col.InsertOneAsync(lockBase, null, token);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(col, Log.Logger, TimeSpan.FromSeconds(30), cts.Token);

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, y => y.SingularInnerLock)).ToList();
            Log.Logger.Information(locks.Count().ToString());
            var lockStates = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.SingularInnerLock, TimeSpan.FromSeconds(1), false)));

            Assert.False(lockStates.Count(x => x == true) < 1, "Failed to acquire lock.");
            Assert.False(lockStates.Count(x => x == true) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync(false)));

            Assert.False(lockStates.Count(x => x == true) < 1, "Failed to refresh lock.");
            Assert.False(lockStates.Count(x => x == true) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.True(lockStates.Count(x => x == true) == locks.Count(), "Failed to release lock.");
            Assert.True(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            locks.ForEach(x => x.Dispose());
            Assert.True(locks.Count(x => x.Disposed) == locks.Count(), "Failed to mark object as disposed");
        }

        [Fact]
        public async Task AcquireManyEnumerableSubClassAsync()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.LiterateConsole(LogEventLevel.Verbose)
                .MinimumLevel.Verbose();

            Log.Logger = logConfig.CreateLogger();

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var client = new MongoClient();
            var db = client.GetDatabase("Test");
            var col = db.GetCollection<LockBase>($"lockables.{GetType()}");
            await col.DeleteManyAsync(Builders<LockBase>.Filter.Empty, token);
            var lockBase = new LockBase();
            await col.InsertOneAsync(lockBase, null, token);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(col, Log.Logger, TimeSpan.FromSeconds(30), cts.Token);

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, y => y.IEnumerableLockables)).ToList();
            Log.Logger.Information(locks.Count().ToString());
            var lockStates = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.IEnumerableLockables.First(), TimeSpan.FromSeconds(1), false)));

            Assert.False(lockStates.Count(x => x == true) < 1, "Failed to acquire lock.");
            Assert.False(lockStates.Count(x => x == true) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync(false)));

            Assert.False(lockStates.Count(x => x == true) < 1, "Failed to refresh lock.");
            Assert.False(lockStates.Count(x => x == true) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.True(lockStates.Count(x => x == true) == locks.Count(), "Failed to release lock.");
            Assert.True(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            locks.ForEach(x => x.Dispose());
            Assert.True(locks.Count(x => x.Disposed) == locks.Count(), "Failed to mark object as disposed");
        }

        [Fact]
        public async Task AcquireManyListSubClassAsync()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.LiterateConsole(LogEventLevel.Verbose)
                .MinimumLevel.Verbose();

            Log.Logger = logConfig.CreateLogger();

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var client = new MongoClient();
            var db = client.GetDatabase("Test");
            var col = db.GetCollection<LockBase>($"lockables.{GetType()}");
            await col.DeleteManyAsync(Builders<LockBase>.Filter.Empty, token);
            var lockBase = new LockBase();
            await col.InsertOneAsync(lockBase, null, token);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(col, Log.Logger, TimeSpan.FromSeconds(30), cts.Token);

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, y => y.ListOfLockables)).ToList();
            Log.Logger.Information(locks.Count().ToString());
            var lockStates = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.ListOfLockables[0], TimeSpan.FromSeconds(1), false)));

            Assert.False(lockStates.Count(x => x == true) < 1, "Failed to acquire lock.");
            Assert.False(lockStates.Count(x => x == true) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync(false)));

            Assert.False(lockStates.Count(x => x == true) < 1, "Failed to refresh lock.");
            Assert.False(lockStates.Count(x => x == true) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.True(lockStates.Count(x => x == true) == locks.Count(), "Failed to release lock.");
            Assert.True(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            locks.ForEach(x => x.Dispose());
            Assert.True(locks.Count(x => x.Disposed) == locks.Count(), "Failed to mark object as disposed");
        }

        [Fact]
        public async Task AcquireManyArraySubClassAsync()
        {
            var logConfig = new LoggerConfiguration()
                .WriteTo.LiterateConsole(LogEventLevel.Verbose)
                .MinimumLevel.Verbose();

            Log.Logger = logConfig.CreateLogger();

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var client = new MongoClient();
            var db = client.GetDatabase("Test");
            var col = db.GetCollection<LockBase>($"lockables.{GetType()}");
            await col.DeleteManyAsync(Builders<LockBase>.Filter.Empty, token);
            var lockBase = new LockBase();
            await col.InsertOneAsync(lockBase, null, token);
            var dataStore = new MongoDataStore<LockBase, InnerLock>(col, Log.Logger, TimeSpan.FromSeconds(30), cts.Token);

            var locks = Enumerable.Range(0, 100).Select(x => new DistributedLock<LockBase, InnerLock, ObjectId>(dataStore, y => y.ArrayOfLockables)).ToList();
            Log.Logger.Information(locks.Count().ToString());
            var lockStates = await Task.WhenAll(locks.Select(x => x.AcquireLockAsync(lockBase, lockBase.ArrayOfLockables[1], TimeSpan.FromSeconds(1), false)));

            Assert.False(lockStates.Count(x => x == true) < 1, "Failed to acquire lock.");
            Assert.False(lockStates.Count(x => x == true) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.RefreshLockAsync(false)));

            Assert.False(lockStates.Count(x => x == true) < 1, "Failed to refresh lock.");
            Assert.False(lockStates.Count(x => x == true) > 1, "Acquired multiple locks.");

            lockStates = await Task.WhenAll(locks.Select(x => x.ReleaseLockAsync()));

            Assert.True(lockStates.Count(x => x == true) == locks.Count(), "Failed to release lock.");
            Assert.True(locks.Count(x => x.LockAcquired) == 0, "Failed to release lock.");

            locks.ForEach(x => x.Dispose());
            Assert.True(locks.Count(x => x.Disposed) == locks.Count(), "Failed to mark object as disposed");
        }
    }
}