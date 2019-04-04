#define ENABLE_SPAN_T
#define UNSAFE_BYTEBUFFER
#define BYTEBUFFER_NO_BOUNDS_CHECK

using System.Collections.Generic;
using System.Buffers;

namespace Mirror.Buffers
{
    public interface IBufferAllocator
    {
        IBuffer Acquire(int minSizeInBytes);
        IBuffer Reacquire(IBuffer buffer, int newMinSizeInBytes);
        void Release(IBuffer buffer);
    }

    internal class BufferAllocator : IBufferAllocator
    {
        private Stack<Buffer> _bufferPool = new Stack<Buffer>();
        private ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
        public IBuffer Acquire(int minSizeInBytes = BufferConstants.DefaultBufferSize)
        {
            Buffer rv;
            if (_bufferPool.Count > 0)
            {
                rv = _bufferPool.Pop();
            }
            else
            {
                rv = new Buffer();
            }

            rv.Setup(_arrayPool.Rent(minSizeInBytes), 0, minSizeInBytes);

            return rv;
        }

        public IBuffer Reacquire(IBuffer ibuffer, int newMinSizeInBytes = 0)
        {
            if (ibuffer is Buffer buffer)
            {
                // one of two options here:
                // 1) rent new array from ArrayPool, copy from old, release old
                // 2) buffer segments / system.io.pipelines magic
                // for now option 1)
                
                return ibuffer;
            }
            else
            {
                throw new System.ArgumentException("Do not Reacquire buffers Acquired from a different Allocator!", ibuffer.ToString());
            }
        }

        public void Release(IBuffer ibuffer)
        {
            if (ibuffer is Buffer buffer)
            {
                _bufferPool.Push(buffer);
            }
            else
            {
                throw new System.ArgumentException("Do not Release buffers Acquired from a different Allocator!", ibuffer.ToString());
            }
        }
    }
}
