using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Mirror
{
    // Note: This class is intended to be extremely pedantic, and
    // throw exceptions whenever stuff is going slightly wrong.
    // The exceptions will be handled in NetworkServer/NetworkClient.
    public class NetworkReader
    {
        // cache encoding instead of creating it with BinaryWriter each time
        // 1000 readers before:  1MB GC, 30ms
        // 1000 readers after: 0.8MB GC, 18ms
        static readonly UTF8Encoding encoding = new UTF8Encoding(false, true);

        readonly BinaryReader reader;

        public NetworkReader(byte[] buffer)
        {
            reader = new BinaryReader(new MemoryStream(buffer, false), encoding);
        }

        // 'int' is the best type for .Position. 'short' is too small if we send >32kb which would result in negative .Position
        // -> converting long to int is fine until 2GB of data (MAX_INT), so we don't have to worry about overflows here
        public int Position { get { return (int)reader.BaseStream.Position; }  set { reader.BaseStream.Position = value; } }
        public int Length => (int)reader.BaseStream.Length;

        [NetworkReader] public byte ReadByte() => reader.ReadByte();
        [NetworkReader] public sbyte ReadSByte() => reader.ReadSByte();
        [NetworkReader] public char ReadChar() => reader.ReadChar();
        [NetworkReader] public bool ReadBoolean() => reader.ReadBoolean();
        [NetworkReader] public short ReadInt16() => reader.ReadInt16();
        [NetworkReader] public ushort ReadUInt16() => reader.ReadUInt16();
        public int ReadInt32() => reader.ReadInt32();
        public uint ReadUInt32() => reader.ReadUInt32();
        public long ReadInt64() => reader.ReadInt64();
        public ulong ReadUInt64() => reader.ReadUInt64();
        [NetworkReader] public decimal ReadDecimal() => reader.ReadDecimal();
        [NetworkReader] public float ReadSingle() => reader.ReadSingle();
        [NetworkReader] public double ReadDouble() => reader.ReadDouble();

        // note: this will throw an ArgumentException if an invalid utf8 string is sent
        // null support, see NetworkWriter
        [NetworkReader] public string ReadString() => ReadBoolean() ? reader.ReadString() : null;

        public byte[] ReadBytes(int count)
        {
            byte[] data = reader.ReadBytes(count);
            if (data.Length != count) throw new EndOfStreamException("Could not fulfill request to read a byte[] of length " + count);
            return data;
        }

        // Use checked() to force it to throw OverflowException if data is invalid
        // null support, see NetworkWriter
        [NetworkReader] public byte[] ReadBytesAndSize() => 
            ReadBoolean() ? ReadBytes(checked((int)ReadPackedUInt32())) : null;

        // zigzag decoding https://gist.github.com/mfuerstenau/ba870a29e16536fdbaba
        [NetworkReader]
        public int ReadPackedInt32()
        {
            uint data = ReadPackedUInt32();
            return (int)((data >> 1) ^ -(data & 1));
        }

        // http://sqlite.org/src4/doc/trunk/www/varint.wiki
        // NOTE: big endian.
        // Use checked() to force it to throw OverflowException if data is invalid
        [NetworkReader] public uint ReadPackedUInt32() => checked((uint)ReadPackedUInt64());

        // zigzag decoding https://gist.github.com/mfuerstenau/ba870a29e16536fdbaba
        [NetworkReader]
        public long ReadPackedInt64()
        {
            ulong data = ReadPackedUInt64();
            return ((long)(data >> 1)) ^ -((long)data & 1);
        }

        [NetworkReader]
        public ulong ReadPackedUInt64()
        {
            byte a0 = ReadByte();
            if (a0 < 241)
            {
                return a0;
            }

            byte a1 = ReadByte();
            if (a0 >= 241 && a0 <= 248)
            {
                return 240 + 256 * (a0 - ((ulong)241)) + a1;
            }

            byte a2 = ReadByte();
            if (a0 == 249)
            {
                return 2288 + (((ulong)256) * a1) + a2;
            }

            byte a3 = ReadByte();
            if (a0 == 250)
            {
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16);
            }

            byte a4 = ReadByte();
            if (a0 == 251)
            {
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24);
            }

            byte a5 = ReadByte();
            if (a0 == 252)
            {
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32);
            }

            byte a6 = ReadByte();
            if (a0 == 253)
            {
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40);
            }

            byte a7 = ReadByte();
            if (a0 == 254)
            {
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40) + (((ulong)a7) << 48);
            }

            byte a8 = ReadByte();
            if (a0 == 255)
            {
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40) + (((ulong)a7) << 48)  + (((ulong)a8) << 56);
            }

            throw new IndexOutOfRangeException("ReadPackedUInt64() failure: " + a0);
        }

        [NetworkReader] public Vector2 ReadVector2() => new Vector2(ReadSingle(), ReadSingle());
        [NetworkReader] public Vector3 ReadVector3() => new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
        [NetworkReader] public Vector4 ReadVector4() => new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        [NetworkReader] public Vector2Int ReadVector2Int() => new Vector2Int(ReadPackedInt32(), ReadPackedInt32());
        [NetworkReader] public Vector3Int ReadVector3Int() => new Vector3Int(ReadPackedInt32(), ReadPackedInt32(), ReadPackedInt32());
        [NetworkReader] public Color ReadColor() => new Color(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        [NetworkReader] public Color32 ReadColor32() => new Color32(ReadByte(), ReadByte(), ReadByte(), ReadByte());
        [NetworkReader] public Quaternion ReadQuaternion() => new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        [NetworkReader] public Rect ReadRect() => new Rect(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        [NetworkReader] public Plane ReadPlane() => new Plane(ReadVector3(), ReadSingle());
        [NetworkReader] public Ray ReadRay() => new Ray(ReadVector3(), ReadVector3());

        [NetworkReader]
        public Matrix4x4 ReadMatrix4x4()
        {
            return new Matrix4x4
            {
                m00 = ReadSingle(),
                m01 = ReadSingle(),
                m02 = ReadSingle(),
                m03 = ReadSingle(),
                m10 = ReadSingle(),
                m11 = ReadSingle(),
                m12 = ReadSingle(),
                m13 = ReadSingle(),
                m20 = ReadSingle(),
                m21 = ReadSingle(),
                m22 = ReadSingle(),
                m23 = ReadSingle(),
                m30 = ReadSingle(),
                m31 = ReadSingle(),
                m32 = ReadSingle(),
                m33 = ReadSingle()
            };
        }

        [NetworkReader] public Guid ReadGuid() => new Guid(ReadBytes(16));
        [NetworkReader] public Transform ReadTransform() => ReadNetworkIdentity()?.transform;
        [NetworkReader] public GameObject ReadGameObject() => ReadNetworkIdentity()?.gameObject;

        [NetworkReader]
        public NetworkIdentity ReadNetworkIdentity()
        {
            uint netId = ReadPackedUInt32();
            if (netId == 0) return null;

            if (NetworkIdentity.spawned.TryGetValue(netId, out NetworkIdentity identity))
            {
                return identity;
            }

            if (LogFilter.Debug) Debug.Log("ReadNetworkIdentity netId:" + netId + " not found in spawned");
            return null;
        }

        public override string ToString() => reader.ToString();
    }
}
