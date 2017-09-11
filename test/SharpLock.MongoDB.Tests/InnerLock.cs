using System;
using MongoDB.Bson;

namespace SharpLock.MongoDB.Tests
{
    public class InnerLock : ISharpLockable<ObjectId>
    {
        public InnerLock()
        {
            Id = ObjectId.GenerateNewId();
            SomeVal = "abcd1234";
        }
        public string SomeVal { get; set;}
        public ObjectId Id { get; set; }
        public DateTime? UpdateLock { get; set; }
        public Guid? LockId { get; set; }
    }
}