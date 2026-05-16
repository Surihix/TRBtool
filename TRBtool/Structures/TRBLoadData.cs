namespace TRBtool.Structures
{
    internal class TRBLoadData
    {
        public TRBHeader Header;
        public TRBResourceInfoEntry[] ResourceInfoTable;
        public long ResourcesDataStartOffset;
        public string[] ResourceIDTable;
        public string[] ResourceTypeTable;
    }
}