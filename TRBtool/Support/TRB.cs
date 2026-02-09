namespace TRBtool.Support
{
    internal class TRB
    {
        public uint Version;
        public byte MainType;
        public uint ResourceCount;
        public (uint, uint)[] ResourceInfo;
    }
}