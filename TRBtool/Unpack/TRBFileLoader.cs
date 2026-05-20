using System;
using System.IO;
using TRBtool.Support;
using TRBtool.Support.Structures;

namespace TRBtool.Unpack
{
    internal class TRBFileLoader
    {
        public static TRBLoadData LoadTRBFile(string trbFile)
        {
            var trbLoadData = new TRBLoadData();

            using (var trbFileReader = new BinaryReader(new FileStream(trbFile, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                var trbHeader = new TRBHeader()
                {
                    Magic = trbFileReader.ReadBytesString(8, false)
                };

                if (trbHeader.Magic != "SEDBRES ")
                {
                    SharedMethods.ErrorExit("Error: Not a valid TRB file!");
                }

                trbHeader.Version = trbFileReader.ReadUInt32();
                trbHeader.EndiannessFlag = trbFileReader.ReadByte();
                trbHeader.MainType = trbFileReader.ReadByte();
                trbHeader.HeaderSize = trbFileReader.ReadUInt16();
                trbHeader.FileSize = trbFileReader.ReadUInt32();
                trbHeader.Reserved = trbFileReader.ReadBytes(28);
                trbHeader.ResourceIDsCount = trbFileReader.ReadUInt32();
                trbHeader.IDsStartOffset = trbFileReader.ReadUInt32();
                trbHeader.ResourceCount = trbFileReader.ReadUInt32();
                trbHeader.FileType = trbFileReader.ReadBytesString(4, true);

                Console.WriteLine($"TRB Version: {trbHeader.Version}");
                Console.WriteLine($"File Size: {trbHeader.FileSize}");
                Console.WriteLine($"Resource Count: {trbHeader.ResourceCount}");
                Console.WriteLine("");

                Console.WriteLine("Loading tables....");

                var resourceInfoTable = LoadResourceInfoTable(trbHeader.ResourceCount, trbFileReader);
                if (resourceInfoTable == null || resourceInfoTable.Length == 0)
                {
                    SharedMethods.ErrorExit("Error: Failed to load Resource Info Table!");
                }

                var resourcesDataStartOffset = trbFileReader.BaseStream.Position;

                var resourceIDTable = LoadResourceIDTable(trbHeader.ResourceIDsCount, trbFileReader, trbHeader.IDsStartOffset);
                if (resourceIDTable == null || resourceIDTable.Length == 0)
                {
                    SharedMethods.ErrorExit($"Error: Failed to load {SharedMethods.TRBResourceIDString} Table!");
                }

                _ = trbFileReader.BaseStream.Position = resourcesDataStartOffset;
                var resourceTypeTable = LoadResourceTypeTable(trbHeader.ResourceCount, trbFileReader, resourceInfoTable[trbHeader.ResourceCount - 2].ResourceOffset);
                if (resourceTypeTable == null || resourceTypeTable.Length == 0)
                {
                    SharedMethods.ErrorExit($"Error: Failed to load {SharedMethods.TRBResourceTypeString} Table!");
                }

                Console.WriteLine("");

                trbLoadData.Header = trbHeader;
                trbLoadData.ResourceInfoTable = resourceInfoTable;
                trbLoadData.ResourcesDataStartOffset = resourcesDataStartOffset;
                trbLoadData.ResourceIDTable = resourceIDTable;
                trbLoadData.ResourceTypeTable = resourceTypeTable;
            }

            return trbLoadData;
        }

        private static TRBResourceInfoEntry[] LoadResourceInfoTable(uint resourceCount, BinaryReader trbFileReader)
        {
            var resourceInfoTable = new TRBResourceInfoEntry[resourceCount];

            for (int i = 0; i < resourceCount; i++)
            {
                var currentEntry = new TRBResourceInfoEntry()
                {
                    ResourceIndex = trbFileReader.ReadUInt32(),
                    ResourceOffset = trbFileReader.ReadUInt32(),
                    ResourceSize = trbFileReader.ReadUInt32(),
                    ResourceType = trbFileReader.ReadUInt32()
                };

                resourceInfoTable[i] = currentEntry;
            }

            return resourceInfoTable;
        }

        private static string[] LoadResourceIDTable(uint resourceIDsCount, BinaryReader trbFileReader, uint idsStartOffset)
        {
            var resourceIDTable = new string[resourceIDsCount];

            _ = trbFileReader.BaseStream.Position += idsStartOffset;

            for (int i = 0; i < resourceIDsCount; i++)
            {
                resourceIDTable[i] = trbFileReader.ReadStringTillNull();
            }

            return resourceIDTable;
        }

        private static string[] LoadResourceTypeTable(uint resourceIDsCount, BinaryReader trbFileReader, uint typesStartOffset)
        {
            var resourceTypesCount = resourceIDsCount - 2;
            var resourceTypeTable = new string[resourceTypesCount];

            _ = trbFileReader.BaseStream.Position += typesStartOffset;

            for (int i = 0; i < resourceTypesCount; i++)
            {
                resourceTypeTable[i] = trbFileReader.ReadBytesString(4, true);
            }

            return resourceTypeTable;
        }
    }
}