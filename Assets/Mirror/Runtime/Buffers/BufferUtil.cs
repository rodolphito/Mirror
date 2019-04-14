using System;
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mirror.Buffers
{
    public static class BufferUtil
    {
        const MethodImplOptions Inline = MethodImplOptions.AggressiveInlining;
        static Encoding _encoding = new UTF8Encoding(false);

        #region Min and Max: inlined
        [MethodImpl(Inline)]
        public static byte Min(byte x, byte y) => x < y ? x : y;
        [MethodImpl(Inline)]
        public static sbyte Min(sbyte x, sbyte y) => (sbyte)Min((byte)x, (byte)y);

        [MethodImpl(Inline)]
        public static byte Max(byte x, byte y) => x > y ? x : y;
        [MethodImpl(Inline)]
        public static sbyte Max(sbyte x, sbyte y) => (sbyte)Max((byte)x, (byte)y);

        [MethodImpl(Inline)]
        public static ushort Min(ushort x, ushort y) => x < y ? x : y;
        [MethodImpl(Inline)]
        public static short Min(short x, short y) => (short)Min((ushort)x, (ushort)y);

        [MethodImpl(Inline)]
        public static ushort Max(ushort x, ushort y) => x > y ? x : y;
        [MethodImpl(Inline)]
        public static short Max(short x, short y) => (short)Max((ushort)x, (ushort)y);

        [MethodImpl(Inline)]
        public static uint Min(uint x, uint y) => x < y ? x : y;
        [MethodImpl(Inline)]
        public static int Min(int x, int y) => (int)Min((uint)x, (uint)y);

        [MethodImpl(Inline)]
        public static uint Max(uint x, uint y) => x > y ? x : y;
        [MethodImpl(Inline)]
        public static int Max(int x, int y) => (int)Max((uint)x, (uint)y);

        [MethodImpl(Inline)]
        public static ulong Min(ulong x, ulong y) => x < y ? x : y;
        [MethodImpl(Inline)]
        public static long Min(long x, long y) => (long)Min((ulong)x, (ulong)y);

        [MethodImpl(Inline)]
        public static ulong Max(ulong x, ulong y) => x > y ? x : y;
        [MethodImpl(Inline)]
        public static long Max(long x, long y) => (long)Max((ulong)x, (ulong)y);
        #endregion

        #region NextPow2: rounding up to closest power of two
        [MethodImpl(Inline)]
        public static sbyte NextPow2(sbyte val) => (sbyte)NextPow2((byte)val);
        [MethodImpl(Inline)]
        public static byte NextPow2(byte val)
        {
            val = Max(val, (byte)1);
            val--;
            val |= (byte)(val >> 1);
            val |= (byte)(val >> 2);
            val |= (byte)(val >> 4);
            val++;
            return val;
        }

        [MethodImpl(Inline)]
        public static short NextPow2(short val) => (short)NextPow2((ushort)val);
        [MethodImpl(Inline)]
        public static ushort NextPow2(ushort val)
        {
            val = Max(val, (ushort)1);
            val--;
            val |= (ushort)(val >> 1);
            val |= (ushort)(val >> 2);
            val |= (ushort)(val >> 4);
            val |= (ushort)(val >> 8);
            val++;
            return val;
        }

        [MethodImpl(Inline)]
        public static int NextPow2(int val) => (int)NextPow2((uint)val);
        [MethodImpl(Inline)]
        public static uint NextPow2(uint val)
        {
            val = Max(val, 1U);
            val--;
            val |= val >> 1;
            val |= val >> 2;
            val |= val >> 4;
            val |= val >> 8;
            val |= val >> 16;
            val++;
            return val;
        }

        [MethodImpl(Inline)]
        public static long NextPow2(long val) => (long)NextPow2((ulong)val);
        [MethodImpl(Inline)]
        public static ulong NextPow2(ulong val)
        {
            val = Max(val, 1UL);
            val--;
            val |= val >> 1;
            val |= val >> 2;
            val |= val >> 4;
            val |= val >> 8;
            val |= val >> 16;
            val |= val >> 32;
            val++;
            return val;
        }
        #endregion

        #region SwapBytes: endian swapping
        [MethodImpl(Inline)]
        public static ushort SwapBytes(ushort input)
        {
            return (ushort)(((input & 0x00FFU) << 8) |
                            ((input & 0xFF00U) >> 8));
        }

        [MethodImpl(Inline)]
        public static uint SwapBytes(uint input)
        {
            return ((input & 0x000000FFU) << 24) |
                   ((input & 0x0000FF00U) << 8) |
                   ((input & 0x00FF0000U) >> 8) |
                   ((input & 0xFF000000U) >> 24);
        }

        [MethodImpl(Inline)]
        public static ulong SwapBytes(ulong input)
        {
            return ((input & 0x00000000000000FFUL) << 56) |
                   ((input & 0x000000000000FF00UL) << 40) |
                   ((input & 0x0000000000FF0000UL) << 24) |
                   ((input & 0x00000000FF000000UL) << 8) |
                   ((input & 0x000000FF00000000UL) >> 8) |
                   ((input & 0x0000FF0000000000UL) >> 24) |
                   ((input & 0x00FF000000000000UL) >> 40) |
                   ((input & 0xFF00000000000000UL) >> 56);
        }
        #endregion

        #region Write: safe binary writing using Span
        [MethodImpl(Inline)]
        public static int Write<T>(Span<byte> dst, T src) where T : struct
        {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(dst), src);
            return Unsafe.SizeOf<T>();
        }

        [MethodImpl(Inline)]
        public static int WriteBool(Span<byte> dst, int dstOffset, bool src) => Write(dst.Slice(dstOffset), (byte)(src ? 1 : 0));

        [MethodImpl(Inline)]
        public static int WriteSByte(Span<byte> dst, int dstOffset, sbyte src) => Write(dst.Slice(dstOffset), src);
        [MethodImpl(Inline)]
        public static int WriteByte(Span<byte> dst, int dstOffset, byte src) => Write(dst.Slice(dstOffset), src);

        [MethodImpl(Inline)]
        public static int WriteShort(Span<byte> dst, int dstOffset, short src) => Write(dst.Slice(dstOffset), src);
        [MethodImpl(Inline)]
        public static int WriteUShort(Span<byte> dst, int dstOffset, ushort src) => Write(dst.Slice(dstOffset), src);

        [MethodImpl(Inline)]
        public static int WriteInt(Span<byte> dst, int dstOffset, int src) => Write(dst.Slice(dstOffset), src);
        [MethodImpl(Inline)]
        public static int WriteUInt(Span<byte> dst, int dstOffset, uint src) => Write(dst.Slice(dstOffset), src);

        [MethodImpl(Inline)]
        public static int WriteLong(Span<byte> dst, int dstOffset, long src) => Write(dst.Slice(dstOffset), src);
        [MethodImpl(Inline)]
        public static int WriteULong(Span<byte> dst, int dstOffset, ulong src) => Write(dst.Slice(dstOffset), src);

        [MethodImpl(Inline)]
        public static int WriteFloat(Span<byte> dst, int dstOffset, float src) => Write(dst.Slice(dstOffset), src);

        [MethodImpl(Inline)]
        public static int WriteDouble(Span<byte> dst, int dstOffset, double src) => Write(dst.Slice(dstOffset), src);

        [MethodImpl(Inline)]
        public static int WriteDecimal(Span<byte> dst, int dstOffset, decimal src) => Write(dst.Slice(dstOffset), src);

        [MethodImpl(Inline)]
        public static int WriteBytes(Span<byte> dst, int dstOffset, ReadOnlySpan<byte> src, int srcOffset, int byteLength)
        {
            if (src.Slice(srcOffset, byteLength).TryCopyTo(dst))
            {
                return byteLength;
            }
            return 0;
        }
        #endregion

        #region Read: safe binary reading using span
        [MethodImpl(Inline)]
        public static int Read<T>(out T dst, Span<byte> src) where T : struct
        {
            dst = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(src));
            return Unsafe.SizeOf<T>();
        }

        [MethodImpl(Inline)]
        public static int ReadBool(out bool dst, Span<byte> src, int srcOffset) => Read(out dst, src.Slice(srcOffset));

        [MethodImpl(Inline)]
        public static int ReadSByte(out sbyte dst, Span<byte> src, int srcOffset) => Read(out dst, src.Slice(srcOffset));
        [MethodImpl(Inline)]
        public static int ReadByte(out byte dst, Span<byte> src, int srcOffset) => Read(out dst, src.Slice(srcOffset));

        [MethodImpl(Inline)]
        public static int ReadShort(out short dst, Span<byte> src, int srcOffset) => Read(out dst, src.Slice(srcOffset));
        [MethodImpl(Inline)]
        public static int ReadUShort(out ushort dst, Span<byte> src, int srcOffset) => Read(out dst, src.Slice(srcOffset));

        [MethodImpl(Inline)]
        public static int ReadInt(out int dst, Span<byte> src, int srcOffset) => Read(out dst, src.Slice(srcOffset));
        [MethodImpl(Inline)]
        public static int ReadUInt(out uint dst, Span<byte> src, int srcOffset) => Read(out dst, src.Slice(srcOffset));

        [MethodImpl(Inline)]
        public static int ReadLong(out long dst, Span<byte> src, int srcOffset) => Read(out dst, src.Slice(srcOffset));
        [MethodImpl(Inline)]
        public static int ReadULong(out ulong dst, Span<byte> src, int srcOffset) => Read(out dst, src.Slice(srcOffset));

        [MethodImpl(Inline)]
        public static int ReadFloat(out float dst, Span<byte> src, int srcOffset) => Read(out dst, src.Slice(srcOffset));

        [MethodImpl(Inline)]
        public static int ReadDouble(out double dst, Span<byte> src, int srcOffset) => Read(out dst, src.Slice(srcOffset));

        [MethodImpl(Inline)]
        public static int ReadDecimal(out decimal dst, Span<byte> src, int srcOffset) => Read(out dst, src.Slice(srcOffset));

        [MethodImpl(Inline)]
        public static int ReadBytes(Span<byte> dst, int dstOffset, Span<byte> src, int srcOffset, int byteLength) => WriteBytes(src, srcOffset, dst, dstOffset, byteLength);
        #endregion

        [MethodImpl(Inline)]
        internal static int StringByteCount(string stringSrc) => _encoding.GetByteCount(stringSrc);
    }
}
