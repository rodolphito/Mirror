#define MIRROR_BUFFER_CHECK_BOUNDS
#define MIRROR_BUFFER_DYNAMIC_GROWTH

using System;
using System.Text;

namespace Mirror.Buffers
{
    public interface IBuffer
    {
        
    }

    internal unsafe class Buffer : IBuffer
    {
        private IBufferAllocator _bufferAllocator;
        private byte[] _buffer;
        private uint _offset;
        private uint _position;
        private uint _length;
        private static Encoding encoding = new UTF8Encoding(false);

        internal uint Capacity { get; private set; }

        //public int Position { get { return _position; } set { writer.BaseStream.Position = value; } }

        internal Buffer()
        {
        }

        internal void Setup(byte[] buf, uint offset, uint capacity)
        {

        }

        private void CheckPosition(uint addToPos)
        {
            uint newPos = _position + addToPos;
            if (newPos < 0)
            {
                throw new ArgumentOutOfRangeException("buffer cursor position cannot be negative");
            }

            if (newPos >= Capacity)
            {
#if MIRROR_BUFFER_DYNAMIC_GROWTH

                BufferManager.ReacquireBuffer(this, Capacity << 1);
#else
                throw new ArgumentOutOfRangeException("buffer cursor position cannot be greater than buffer capacity");
#endif
            }
        }

        private void UpdatePosition(uint addToPos)
        {
            _position += addToPos;
            _length = BufferUtil.Max(_position, _length);
        }

        private void Write(bool src) => Write((byte)(src ? 1 : 0));

        private void Write(sbyte src) => Write((byte)src);
        private unsafe void Write(byte src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckPosition(sizeof(byte));
#endif
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                *dst = src;
            }
            UpdatePosition(sizeof(byte));
        }

        private void Write(ushort src) => Write((short)src);
        private unsafe void Write(short src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckPosition(sizeof(short));
#endif
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                *(short*)dst = src;
            }
            UpdatePosition(sizeof(short));
        }

        private void Write(uint src) => Write((int)src);
        private unsafe void Write(int src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckPosition(sizeof(int));
#endif
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                *(int*)dst = src;
            }
            UpdatePosition(sizeof(int));
        }

        private void Write(ulong src) => Write((long)src);
        private unsafe void Write(long src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckPosition(sizeof(long));
#endif
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                *(long*)dst = src;
            }
            UpdatePosition(sizeof(long));
        }

        private unsafe void Write(float src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckPosition(sizeof(float));
#endif
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                *(float*)dst = src;
            }
            UpdatePosition(sizeof(float));
        }

        private unsafe void Write(double src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckPosition(sizeof(double));
#endif
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                *(double*)dst = src;
            }
            UpdatePosition(sizeof(double));
        }

        public unsafe void Write(string src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckPosition((uint) encoding.GetByteCount(src));
#endif
            uint written;

            fixed (char* s = src)
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                written = (uint) encoding.GetBytes(s, src.Length, dst, (int) (Capacity - _position));
            }
            UpdatePosition(written);
        }
    }
}
