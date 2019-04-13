#define MIRROR_BUFFER_CHECK_BOUNDS
#define MIRROR_BUFFER_DYNAMIC_GROWTH

using System;
using System.Text;

namespace Mirror.Buffers
{
    internal sealed unsafe class Buffer : IBuffer
    {
#if MIRROR_BUFFER_DYNAMIC_GROWTH
        IBufferAllocator _allocator;
#endif
        byte[] _buffer;
        Memory<byte> mem;
        ulong _offset;
        ulong _position;
        ulong _length;
        ulong _capacity;

        internal ulong Capacity => _capacity;

        ulong IBuffer.Position
        {
            get
            {
                return _position;
            }
            set
            {
#if MIRROR_BUFFER_CHECK_BOUNDS
                CheckPosition(value);
#endif
                _position = value;
            }
        }

        ulong IBuffer.Length
        {
            get
            {
                return _length;
            }
            set
            {
                // TODO: zero-fill newly opened space
                CheckCapacity(_length);
                _length = value;
            }
        }

        internal Buffer()
        {
        }

        internal void Setup(IBufferAllocator allocator, byte[] buf, ulong offset, ulong capacity)
        {
#if MIRROR_BUFFER_DYNAMIC_GROWTH
            _allocator = allocator;
#endif
            _buffer = buf;
            _offset = offset;
            _position = 0;
            _length = 0;
            _capacity = capacity;
        }

        void CheckCapacity(ulong minimum)
        {
            if (minimum > _capacity)
            {
#if MIRROR_BUFFER_DYNAMIC_GROWTH
                _allocator.Reacquire(this, BufferUtil.NextPow2(minimum));
#else
                throw new ArgumentException("buffer dynamic growth is disabled");
#endif
            }
        }

#if MIRROR_BUFFER_CHECK_BOUNDS
        void CheckWrite(ulong addToPos)
        {
            ulong newPos = _position + addToPos;

            CheckCapacity(newPos);
        }

        void CheckRead(ulong addToPos)
        {
            ulong newPos = _position + addToPos;

            if (newPos > _length)
            {
                throw new ArgumentException("buffer cursor position cannot be greater than buffer length");
            }
        }

        void CheckPosition(ulong newPosition)
        {
            if (newPosition > _length)
            {
                throw new ArgumentException("buffer cursor position cannot be greater than buffer length");
            }
        }
#endif

        void UpdateWrite(ulong addToPos)
        {
            _position += addToPos;
            _length = BufferUtil.Max(_position, _length);
        }

        void UpdateRead(ulong addToPos)
        {
            _position += addToPos;
        }

        unsafe void IBuffer.WriteByte(byte src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(sizeof(byte));
#endif
            UpdateWrite(BufferUtil.UnsafeWrite(_buffer, _offset + _position, src));
        }

        unsafe void IBuffer.WriteUShort(ushort src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(sizeof(ushort));
#endif
            UpdateWrite(BufferUtil.UnsafeWrite(_buffer, _offset + _position, src));
        }

        unsafe void IBuffer.WriteUInt(uint src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(sizeof(uint));
#endif
            UpdateWrite(BufferUtil.UnsafeWrite(_buffer, _offset + _position, src));
        }

        unsafe void IBuffer.WriteULong(ulong src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(sizeof(ulong));
#endif
            UpdateWrite(BufferUtil.UnsafeWrite(_buffer, _offset + _position, src));
        }

        unsafe void IBuffer.WriteFloat(float src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(sizeof(float));
#endif
            UpdateWrite(BufferUtil.UnsafeWrite(_buffer, _offset + _position, src));
        }

        unsafe void IBuffer.WriteDouble(double src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(sizeof(double));
#endif
            UpdateWrite(BufferUtil.UnsafeWrite(_buffer, _offset + _position, src));
        }

        unsafe void IBuffer.WriteDecimal(decimal src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(sizeof(decimal));
#endif
            UpdateWrite(BufferUtil.UnsafeWrite(_buffer, _offset + _position, src));
        }

        unsafe void IBuffer.WriteBytes(byte[] data, ulong offset, ulong length)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(length);
#endif
            UpdateRead(BufferUtil.UnsafeWrite(_buffer, _position, data, offset, length));
        }

        unsafe void IBuffer.WriteString(string src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(BufferUtil.StringByteCount(src));
#endif
            UpdateWrite(BufferUtil.UnsafeWrite(_buffer, _offset + _position, src));
        }

        unsafe byte IBuffer.ReadByte()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(sizeof(byte));
#endif
            UpdateRead(BufferUtil.UnsafeRead(out byte dst, _buffer, _offset + _position));
            return dst;
        }

        unsafe ushort IBuffer.ReadUShort()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(sizeof(ushort));
#endif
            UpdateRead(BufferUtil.UnsafeRead(out ushort dst, _buffer, _offset + _position));
            return dst;
        }

        unsafe uint IBuffer.ReadUInt()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(sizeof(uint));
#endif
            UpdateRead(BufferUtil.UnsafeRead(out uint dst, _buffer, _offset + _position));
            return dst;
        }

        unsafe ulong IBuffer.ReadULong()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(sizeof(ulong));
#endif
            UpdateRead(BufferUtil.UnsafeRead(out ulong dst, _buffer, _offset + _position));
            return dst;
        }

        unsafe float IBuffer.ReadFloat()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(sizeof(float));
#endif
            UpdateRead(BufferUtil.UnsafeRead(out float dst, _buffer, _offset + _position));
            return dst;
        }

        unsafe double IBuffer.ReadDouble()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(sizeof(double));
#endif
            UpdateRead(BufferUtil.UnsafeRead(out double dst, _buffer, _offset + _position));
            return dst;
        }

        unsafe decimal IBuffer.ReadDecimal()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(sizeof(decimal));
#endif
            UpdateRead(BufferUtil.UnsafeRead(out decimal dst, _buffer, _offset + _position));
            return dst;
        }

        unsafe ulong IBuffer.ReadBytes(byte[] data, ulong offset, ulong length)
        {
            length = BufferUtil.Min(length, _length - _position);
            UpdateRead(BufferUtil.UnsafeRead(_buffer, _position, data, offset, length));
            return length;
        }

        unsafe string IBuffer.ReadString(uint length)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(length);
#endif
            UpdateRead(BufferUtil.UnsafeRead(out string dst, _buffer, _offset + _position, (int)length));
            return dst;
        }
    }
}
