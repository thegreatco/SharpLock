namespace SharpLock
{
    public interface ISharpLockableBase<T>
    {
        /// <summary>
        /// The Id of the object being locked.
        /// </summary>
        T Id { get; set; }
    }
}
