using System;

namespace SharpLock.InMemory.Tests
{
    public class InnerLock : ISharpLockable<string>
    {
        public InnerLock()
        {
            Id = "FooBar";
            SomeVal = "abcd1234";
        }
        public string SomeVal { get; set;}
        public string Id { get; set; }
        public DateTime? UpdateLock { get; set; }
        public Guid? LockId { get; set; }
    }
}