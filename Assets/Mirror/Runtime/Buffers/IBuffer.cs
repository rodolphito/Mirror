namespace Mirror.Buffers
{
    public interface IBuffer
    {
        ulong Position { get; set; }
        ulong Length { get; set; }
        void WriteByte(byte src);
        void WriteUShort(ushort src);
        void WriteUInt(uint src);
        void WriteULong(ulong src);
        void WriteFloat(float src);
        void WriteDouble(double src);
        void WriteDecimal(decimal src);
        void WriteBytes(byte[] data, ulong offset, ulong length);
        void WriteString(string src);
        byte ReadByte();
        ushort ReadUShort();
        uint ReadUInt();
        ulong ReadULong();
        float ReadFloat();
        double ReadDouble();
        decimal ReadDecimal();
        ulong ReadBytes(byte[] data, ulong offset, ulong length);
        string ReadString(uint length);
    }

    public interface IBufferSink
    {
        void Sink(IBuffer buffer);
    }

    public interface IBufferSource
    {
        IBuffer Source();
    }
}
