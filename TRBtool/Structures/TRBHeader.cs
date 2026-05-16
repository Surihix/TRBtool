namespace TRBtool.Structures
{
    internal class TRBHeader
    {
        public string Magic;
        public uint Version;
        public byte EndiannessFlag;
        public byte MainType;
        public ushort HeaderSize;
        public uint FileSize;
        public byte[] Reserved;
        public uint ResourceIDsCount;
        public uint IDsStartOffset;
        public uint ResourceCount;
        public string ExtensionLE;
    }
}