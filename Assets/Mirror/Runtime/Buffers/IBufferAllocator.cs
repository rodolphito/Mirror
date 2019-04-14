namespace Mirror.Buffers
{
    public interface IBufferAllocator
    {
        IBuffer Acquire(int minSizeInBytes);
        void Release(IBuffer buffer);
    }
}
