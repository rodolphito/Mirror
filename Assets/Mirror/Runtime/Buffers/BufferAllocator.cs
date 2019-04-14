#define MIRROR_BUFFER_PEDANTIC_ALLOCATOR
#define MIRROR_BUFFER_DO_NOT_RECYCLE

using System.Collections.Generic;
using System.Buffers;
using System;

namespace Mirror.Buffers
{
    public sealed class BufferAllocator : IBufferAllocator
    {
        Stack<Buffer> _bufferPool = new Stack<Buffer>();
        MemoryPool<byte> _memPool = MemoryPool<byte>.Shared;
        IBuffer IBufferAllocator.Acquire(int minSizeInBytes)
        {
            Buffer buffer;
#if MIRROR_BUFFER_DO_NOT_RECYCLE
            buffer = new Buffer();
#else
            if (_bufferPool.Count > 0)
            {
                buffer = _bufferPool.Pop();
            }
            else
            {
                buffer = new Buffer();
            }
#endif

            IMemoryOwner<byte> newOwnedMem = _memPool.Rent(minSizeInBytes);
            IMemoryOwner<byte> oldOwnedMem = buffer.Setup(this, newOwnedMem);
            if (oldOwnedMem != null)
            {
                oldOwnedMem.Dispose();
            }

            return buffer;
        }

        internal void Reacquire(Buffer buffer, int newMinSizeInBytes)
        {
            if (newMinSizeInBytes <= buffer.Capacity) return;

            IMemoryOwner<byte> newOwnedMem = _memPool.Rent(newMinSizeInBytes);
            IMemoryOwner<byte> oldOwnedMem = buffer.Setup(this, newOwnedMem);
            if (oldOwnedMem != null)
            {
                oldOwnedMem.Dispose();
            }
        }

        void IBufferAllocator.Release(IBuffer ibuffer)
        {
            if (ibuffer is Buffer buffer)
            {
#if MIRROR_BUFFER_PEDANTIC_ALLOCATOR
                if (_bufferPool.Contains(buffer))
                {
                    throw new ArgumentException("Do not Release buffers twice.", ibuffer.ToString());
                }
#endif

                IMemoryOwner<byte> oldOwnedMem = buffer.Setup(null, null);
                if (oldOwnedMem != null)
                {
                    oldOwnedMem.Dispose();
                }
#if MIRROR_BUFFER_DO_NOT_RECYCLE
                _bufferPool.Push(buffer);
#endif
            }
            else
            {
                throw new ArgumentException("Do not Release buffers Acquired from a different Allocator!", ibuffer.ToString());
            }
        }
    }
}
