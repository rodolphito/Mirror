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
        IBufferAllocator _bufferAllocator;
        byte[] _buffer;
        ulong _offset;
        ulong _position;
        ulong _length;
        static Encoding _encoding = new UTF8Encoding(false);

        internal ulong Capacity { get; private set; }

        //public int Position { get { return _position; } set { writer.BaseStream.Position = value; } }

        internal Buffer()
        {
        }

        internal void Setup(byte[] buf, ulong offset, ulong capacity)
        {

        }

        void CheckPosition(ulong addToPos)
        {
            ulong newPos = _position + addToPos;

            if (newPos >= Capacity)
            {
#if MIRROR_BUFFER_DYNAMIC_GROWTH
                BufferManager.ReacquireBuffer(this, Capacity << 1);
#else
                throw new ArgumentOutOfRangeException("buffer cursor position cannot be greater than buffer capacity");
#endif
            }
        }

        void UpdatePosition(uint addToPos)
        {
            _position += addToPos;
            _length = BufferUtil.Max(_position, _length);
        }

        void Write(bool src) => Write((byte)(src ? 1 : 0));

        void Write(sbyte src) => Write((byte)src);
        unsafe void Write(byte src)
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

        void Write(ushort src) => Write((short)src);
        unsafe void Write(short src)
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

        void Write(uint src) => Write((int)src);
        unsafe void Write(int src)
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

        void Write(ulong src) => Write((long)src);
        unsafe void Write(long src)
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

        unsafe void Write(float src)
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

        unsafe void Write(double src)
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

        unsafe void Write(string src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckPosition((uint) _encoding.GetByteCount(src));
#endif
            uint written;

            fixed (char* s = src)
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                written = (uint) _encoding.GetBytes(s, src.Length, dst, (int) (Capacity - _position));
            }
            UpdatePosition(written);
        }
    }
}
