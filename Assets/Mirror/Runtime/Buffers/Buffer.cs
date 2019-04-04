#define MIRROR_BUFFER_CHECK_BOUNDS
#define MIRROR_BUFFER_DYNAMIC_GROWTH

using System;
using System.Text;

namespace Mirror.Buffers
{
    public interface IBuffer
    {
        void WriteByte(byte src);
        void WriteUShort(ushort src);
        void WriteUInt(uint src);
        void WriteULong(ulong src);
        void WriteFloat(float src);
        void WriteDouble(double src);
        void WriteString(string src);
        byte ReadByte();
        ushort ReadUShort();
        uint ReadUInt();
        ulong ReadULong();
        float ReadFloat();
        double ReadDouble();
        string ReadString();
    }

    internal sealed unsafe class Buffer : IBuffer
    {
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
            _buffer = buf;
            _offset = offset;
            _position = 0;
            _length = 0;
            Capacity = capacity;
        }

#if MIRROR_BUFFER_CHECK_BOUNDS
        void CheckWrite(ulong addToPos)
        {
            ulong newPos = _position + addToPos;

            if (newPos > Capacity)
            {
#if MIRROR_BUFFER_DYNAMIC_GROWTH
                BufferManager.ReacquireBuffer(this, Capacity << 1);
#else
                throw new ArgumentOutOfRangeException("buffer cursor position cannot be greater than buffer capacity");
#endif
            }
        }

        void CheckRead(ulong addToPos)
        {
            ulong newPos = _position + addToPos;

            if (newPos > _length)
            {
                throw new ArgumentOutOfRangeException("buffer cursor position cannot be greater than buffer length");
            }
        }
#endif

        void UpdateWrite(uint addToPos)
        {
            _position += addToPos;
            _length = BufferUtil.Max(_position, _length);
        }

        void UpdateRead(uint addToPos)
        {
            _position += addToPos;
            // Bounds check is not needed here; we already checked before doing the read.
        }

        unsafe void IBuffer.WriteByte(byte src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(sizeof(byte));
#endif
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                *dst = src;
            }
            UpdateWrite(sizeof(byte));
        }

        unsafe void IBuffer.WriteUShort(ushort src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(sizeof(ushort));
#endif
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                *(ushort*)dst = src;
            }
            UpdateWrite(sizeof(ushort));
        }

        unsafe void IBuffer.WriteUInt(uint src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(sizeof(uint));
#endif
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                *(uint*)dst = src;
            }
            UpdateWrite(sizeof(uint));
        }

        unsafe void IBuffer.WriteULong(ulong src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(sizeof(ulong));
#endif
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                *(ulong*)dst = src;
            }
            UpdateWrite(sizeof(ulong));
        }

        unsafe void IBuffer.WriteFloat(float src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(sizeof(float));
#endif
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                *(float*)dst = src;
            }
            UpdateWrite(sizeof(float));
        }

        unsafe void IBuffer.WriteDouble(double src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite(sizeof(double));
#endif
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                *(double*)dst = src;
            }
            UpdateWrite(sizeof(double));
        }

        unsafe void IBuffer.WriteString(string src)
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckWrite((uint) _encoding.GetByteCount(src));
#endif
            uint written;

            fixed (char* s = src)
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                written = (uint) _encoding.GetBytes(s, src.Length, dst, (int) (Capacity - _position));
            }
            UpdateWrite(written);
        }

        unsafe byte IBuffer.ReadByte()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(sizeof(byte));
#endif
            byte dst;
            fixed (byte* src = &_buffer[_offset + _position])
            {
                dst = *src;
            }
            UpdateRead(sizeof(byte));
            return dst;
        }

        unsafe ushort IBuffer.ReadUShort()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(sizeof(ushort));
#endif
            ushort dst;
            fixed (byte* src = &_buffer[_offset + _position])
            {
                dst = *(ushort*)src;
            }
            UpdateRead(sizeof(ushort));
            return dst;
        }

        unsafe uint IBuffer.ReadUInt()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(sizeof(uint));
#endif
            uint dst;
            fixed (byte* src = &_buffer[_offset + _position])
            {
                dst = *(uint*)src;
            }
            UpdateRead(sizeof(uint));
            return dst;
        }

        unsafe ulong IBuffer.ReadULong()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(sizeof(ulong));
#endif
            ulong dst;
            fixed (byte* src = &_buffer[_offset + _position])
            {
                dst = *(ulong*)src;
            }
            UpdateRead(sizeof(ulong));
            return dst;
        }

        unsafe float IBuffer.ReadFloat()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(sizeof(float));
#endif
            float dst;
            fixed (byte* src = &_buffer[_offset + _position])
            {
                dst = *(float*)src;
            }
            UpdateRead(sizeof(float));
            return dst;
        }

        unsafe double IBuffer.ReadDouble()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            CheckRead(sizeof(double));
#endif
            double dst;
            fixed (byte* src = &_buffer[_offset + _position])
            {
                dst = *(double*)src;
            }
            UpdateRead(sizeof(double));
            return dst;
        }

        unsafe string IBuffer.ReadString()
        {
#if MIRROR_BUFFER_CHECK_BOUNDS
            //CheckRead((uint) _encoding.GetByteCount(src));
#endif
            //uint written;

            string dst = "c6 can you please figure this out";
            /*fixed (char* src = src)
            fixed (byte* dst = &_buffer[_offset + _position])
            {
                written = (uint) _encoding.GetBytes(s, src.Length, dst, (int) (Capacity - _position));
            }
            UpdateRead(written);*/
            return dst;
        }
    }
}
