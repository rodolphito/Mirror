using System.Text;
using System.Runtime.CompilerServices;

//
// TODO: test actual effectiveness and assembly size change of aggressive inlining (JIT likely is already doing it)
//

namespace Mirror.Buffers
{
    public static class BufferUtil
    {
        const MethodImplOptions Inline = MethodImplOptions.AggressiveInlining;
        #region Min and Max: non-branching
        // from http://www.coranac.com/documents/bittrick/
        [MethodImpl(Inline)]
        public static byte Min(byte x, byte y) => (byte) (y + ((x - y) & (x - y) >> (sizeof(byte) * 8 - 1)));

        [MethodImpl(Inline)]
        public static byte Max(byte x, byte y) => (byte) (x - ((x - y) & (x - y) >> (sizeof(byte) * 8 - 1)));

        [MethodImpl(Inline)]
        public static ushort Min(ushort x, ushort y) => (ushort) (y + ((x - y) & (x - y) >> (sizeof(ushort) * 8 - 1)));

        [MethodImpl(Inline)]
        public static ushort Max(ushort x, ushort y) => (ushort) (x - ((x - y) & (x - y) >> (sizeof(ushort) * 8 - 1)));

        [MethodImpl(Inline)]
        public static uint Min(uint x, uint y) => (uint) (y + ((x - y) & (x - y) >> (sizeof(uint) * 8 - 1)));

        [MethodImpl(Inline)]
        public static uint Max(uint x, uint y) => (uint) (x - ((x - y) & (x - y) >> (sizeof(uint) * 8 - 1)));

        [MethodImpl(Inline)]
        public static ulong Min(ulong x, ulong y) => (ulong) (y + ((x - y) & (x - y) >> (sizeof(ulong) * 8 - 1)));

        [MethodImpl(Inline)]
        public static ulong Max(ulong x, ulong y) => (ulong) (x - ((x - y) & (x - y) >> (sizeof(ulong) * 8 - 1)));
        #endregion

        #region NextPow2: rounding up to closest power of two
        public static sbyte NextPow2(sbyte val) => (sbyte)NextPow2((byte)val);
        public static byte NextPow2(byte val)
        {
            val = Max(val, 1);
            val--;
            val |= (byte)(val >> 1);
            val |= (byte)(val >> 2);
            val |= (byte)(val >> 4);
            val++;
            return val;
        }

        public static short NextPow2(short val) => (short)NextPow2((ushort)val);
        public static ushort NextPow2(ushort val)
        {
            val = Max(val, 1);
            val--;
            val |= (ushort)(val >> 1);
            val |= (ushort)(val >> 2);
            val |= (ushort)(val >> 4);
            val |= (ushort)(val >> 8);
            val++;
            return val;
        }

        public static int NextPow2(int val) => (int)NextPow2((uint)val);
        public static uint NextPow2(uint val)
        {
            val = Max(val, 1);
            val--;
            val |= val >> 1;
            val |= val >> 2;
            val |= val >> 4;
            val |= val >> 8;
            val |= val >> 16;
            val++;
            return val;
        }

        public static long NextPow2(long val) => (long)NextPow2((ulong)val);
        public static ulong NextPow2(ulong val)
        {
            val = Max(val, 1);
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
        public static short SwapBytes(short input) => (short)SwapBytes((ushort)input);
        public static ushort SwapBytes(ushort input)
        {
            return (ushort)(((input & 0x00FFU) << 8) |
                            ((input & 0xFF00U) >> 8));
        }

        public static int SwapBytes(int input) => (int)SwapBytes((uint)input);
        public static uint SwapBytes(uint input)
        {
            return ((input & 0x000000FFU) << 24) |
                   ((input & 0x0000FF00U) << 8) |
                   ((input & 0x00FF0000U) >> 8) |
                   ((input & 0xFF000000U) >> 24);
        }

        public static long SwapBytes(long input) => (long)SwapBytes((ulong)input);
        public static ulong SwapBytes(ulong input)
        {
            return (((input & 0x00000000000000FFUL) << 56) |
                    ((input & 0x000000000000FF00UL) << 40) |
                    ((input & 0x0000000000FF0000UL) << 24) |
                    ((input & 0x00000000FF000000UL) << 8) |
                    ((input & 0x000000FF00000000UL) >> 8) |
                    ((input & 0x0000FF0000000000UL) >> 24) |
                    ((input & 0x00FF000000000000UL) >> 40) |
                    ((input & 0xFF00000000000000UL) >> 56));
        }
        #endregion

        #region UnsafeCopy#: raw pointer based binary copy for exact sizes from 1 - 8 bytes
        [MethodImpl(Inline)]
        public static unsafe void UnsafeCopy1(byte* pdst, byte* psrc)
        {
            *pdst = *psrc;
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeCopy2(byte* pdst, byte* psrc)
        {
            *(ushort*)pdst = *(ushort*)psrc;
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeCopy3(byte* pdst, byte* psrc)
        {
            *(ushort*)pdst = *(ushort*)psrc;
            *(pdst + sizeof(short)) = *(psrc + sizeof(short));
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeCopy4(byte* pdst, byte* psrc)
        {
            *(uint*)pdst = *(uint*)psrc;
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeCopy5(byte* pdst, byte* psrc)
        {
            *(uint*)pdst = *(uint*)psrc;
            *(pdst + sizeof(int)) = *(psrc + sizeof(int));
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeCopy6(byte* pdst, byte* psrc)
        {
            *(uint*)pdst = *(uint*)psrc;
            *(ushort*)(pdst + sizeof(int)) = *(ushort*)(psrc + sizeof(int));
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeCopy7(byte* pdst, byte* psrc)
        {
            *(uint*)pdst = *(uint*)psrc;
            *(ushort*)(pdst + sizeof(int)) = *(ushort*)(psrc + sizeof(int));
            *(pdst + sizeof(int) + sizeof(short)) = *(psrc + sizeof(int) + sizeof(short));
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeCopy8(byte* pdst, byte* psrc)
        {
            *(ulong*)pdst = *(ulong*)psrc;
        }
        #endregion

        #region UnsafeWrite: unsafe binary writing using fixed pinning
        [MethodImpl(Inline)]
        public static void UnsafeWrite(byte[] dst, int dstOffset, bool boolSrc) => UnsafeWrite(dst, dstOffset, (byte)(boolSrc ? 1 : 0));

        [MethodImpl(Inline)]
        public static void UnsafeWrite(byte[] dst, int dstOffset, sbyte sbyteSrc) => UnsafeWrite(dst, dstOffset, (byte)sbyteSrc);
        [MethodImpl(Inline)]
        public static unsafe void UnsafeWrite(byte[] dst, int dstOffset, byte byteSrc)
        {
            fixed (byte* pdst = &dst[dstOffset])
            {
                *pdst = byteSrc;
            }
        }

        [MethodImpl(Inline)]
        public static void UnsafeWrite(byte[] dst, int dstOffset, short shortSrc) => UnsafeWrite(dst, dstOffset, (ushort)shortSrc);
        [MethodImpl(Inline)]
        public static unsafe void UnsafeWrite(byte[] dst, int dstOffset, ushort ushortSrc)
        {
            fixed (byte* pdst = &dst[dstOffset])
            {
                *(ushort*)pdst = ushortSrc;
            }
        }

        [MethodImpl(Inline)]
        public static void UnsafeWrite(byte[] dst, int dstOffset, int intSrc) => UnsafeWrite(dst, dstOffset, (uint)intSrc);
        [MethodImpl(Inline)]
        public static unsafe void UnsafeWrite(byte[] dst, int dstOffset, uint uintSrc)
        {
            fixed (byte* pdst = &dst[dstOffset])
            {
                *(uint*)pdst = uintSrc;
            }
        }

        [MethodImpl(Inline)]
        public static void UnsafeWrite(byte[] dst, int dstOffset, long longSrc) => UnsafeWrite(dst, dstOffset, (ulong)longSrc);
        [MethodImpl(Inline)]
        public static unsafe void UnsafeWrite(byte[] dst, int dstOffset, ulong ulongSrc)
        {
            fixed (byte* pdst = &dst[dstOffset])
            {
                *(ulong*)pdst = ulongSrc;
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeWrite(byte[] dst, int dstOffset, float floatSrc)
        {
            fixed (byte* pdst = &dst[dstOffset])
            {
                *(float*)pdst = floatSrc;
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeWrite(byte[] dst, int dstOffset, double doubleSrc)
        {
            fixed (byte* pdst = &dst[dstOffset])
            {
                *(double*)pdst = doubleSrc;
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeWrite(byte[] dst, int dstOffset, string stringSrc)
        {
            fixed (char* s = stringSrc)
            fixed (byte* pdst = &dst[dstOffset])
            {
                Encoding.UTF8.GetBytes(s, stringSrc.Length, pdst, stringSrc.Length);
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeWrite(byte[] dst, int dstOffset, byte[] src, int srcOffset, int byteLength)
        {
            int longWriteLimit = (int)(byteLength & 0xfffffff8);
            fixed (byte* psrc = &src[srcOffset])
            fixed (byte* pdst = &dst[dstOffset])
            {
                // write anything over 8 bytes as a series of long*
                for (int cursor = 0; cursor < longWriteLimit; cursor += sizeof(long))
                {
                    UnsafeCopy8(pdst + cursor, psrc + cursor);
                }

                // write anything remaining under 8 bytes
                switch (byteLength & 0x00000007)
                {
                    case 0:
                        break;
                    case 1:
                        UnsafeCopy1(pdst + longWriteLimit, psrc + longWriteLimit);
                        break;
                    case 2:
                        UnsafeCopy2(pdst + longWriteLimit, psrc + longWriteLimit);
                        break;
                    case 3:
                        UnsafeCopy3(pdst + longWriteLimit, psrc + longWriteLimit);
                        break;
                    case 4:
                        UnsafeCopy4(pdst + longWriteLimit, psrc + longWriteLimit);
                        break;
                    case 5:
                        UnsafeCopy5(pdst + longWriteLimit, psrc + longWriteLimit);
                        break;
                    case 6:
                        UnsafeCopy6(pdst + longWriteLimit, psrc + longWriteLimit);
                        break;
                    case 7:
                        UnsafeCopy7(pdst + longWriteLimit, psrc + longWriteLimit);
                        break;
                }
            }
        }
        #endregion

        #region UnsafeRead: unsafe binary reading using fixed pinning
        [MethodImpl(Inline)]
        public static unsafe void UnsafeRead(ref bool boolDst, byte[] src, int srcOffset)
        {
            fixed (byte* psrc = &src[srcOffset])
            {
                boolDst = (*psrc == 0) ? false : true;
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeRead(ref sbyte sbyteDst, byte[] src, int srcOffset)
        {
            fixed (void* pdst = &sbyteDst)
            fixed (byte* psrc = &src[srcOffset])
            {
                UnsafeCopy1((byte*)pdst, psrc);
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeRead(ref byte byteDst, byte[] src, int srcOffset)
        {
            fixed (byte* pdst = &byteDst)
            fixed (byte* psrc = &src[srcOffset])
            {
                UnsafeCopy1(pdst, psrc);
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeRead(ref short shortDst, byte[] src, int srcOffset)
        {
            fixed (void* pdst = &shortDst)
            fixed (byte* psrc = &src[srcOffset])
            {
                UnsafeCopy2((byte*)pdst, psrc);
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeRead(ref ushort ushortDst, byte[] src, int srcOffset)
        {
            fixed (void* pdst = &ushortDst)
            fixed (byte* psrc = &src[srcOffset])
            {
                UnsafeCopy2((byte*)pdst, psrc);
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeRead(ref int intSrc, byte[] src, int srcOffset)
        {
            fixed (void* pdst = &intSrc)
            fixed (byte* psrc = &src[srcOffset])
            {
                UnsafeCopy4((byte*)pdst, psrc);
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeRead(ref uint uintDst, byte[] src, int srcOffset)
        {
            fixed (void* pdst = &uintDst)
            fixed (byte* psrc = &src[srcOffset])
            {
                UnsafeCopy4((byte*)pdst, psrc);
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeRead(ref long longDst, byte[] src, int srcOffset)
        {
            fixed (void* pdst = &longDst)
            fixed (byte* psrc = &src[srcOffset])
            {
                UnsafeCopy8((byte*)pdst, psrc);
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeRead(ref ulong ulongDst, byte[] src, int srcOffset)
        {
            fixed (void* pdst = &ulongDst)
            fixed (byte* psrc = &src[srcOffset])
            {
                UnsafeCopy8((byte*)pdst, psrc);
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeRead(ref float floatDst, byte[] src, int srcOffset)
        {
            fixed (void* pdst = &floatDst)
            fixed (byte* psrc = &src[srcOffset])
            {
                UnsafeCopy4((byte*)pdst, psrc);
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeRead(ref double doubleDst, byte[] src, int srcOffset)
        {
            fixed (void* pdst = &doubleDst)
            fixed (byte* psrc = &src[srcOffset])
            {
                UnsafeCopy8((byte*)pdst, psrc);
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeRead(ref string stringDst, byte[] src, int srcOffset, int byteLength)
        {
            fixed (byte* psrc = &src[srcOffset])
            {
                stringDst = Encoding.UTF8.GetString(psrc, byteLength);
            }
        }

        [MethodImpl(Inline)]
        public static unsafe void UnsafeRead(byte[] dst, int dstOffset, byte[] src, int srcOffset, int byteLength) => UnsafeWrite(src, srcOffset, dst, dstOffset, byteLength);
        #endregion
    }
}
