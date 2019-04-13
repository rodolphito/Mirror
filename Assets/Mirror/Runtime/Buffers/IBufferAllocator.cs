namespace Mirror.Buffers
{
    public interface IBufferAllocator
    {
        IBuffer Acquire(ulong minSizeInBytes);
        void Release(IBuffer buffer);
    }
}