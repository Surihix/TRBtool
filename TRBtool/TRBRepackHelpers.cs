using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TRBtool.Structures;
using TRBtool.Support;

namespace TRBtool
{
    internal class TRBRepackHelpers
    {
        public static TRBPackData GetTRBPackDataFromTxts(string inExtractedTRBdir)
        {
            var trbHeader = new TRBHeader()
            {
                Magic = "SEDBRES ",
                HeaderSize = 64,
                Reserved = new byte[28],
                FileType = "brt"
            };

            var resourceInfoTxtFile = Path.Combine(inExtractedTRBdir, $"{SharedMethods.TRBResourceInfoFileString}.txt");
            CheckFileExists(resourceInfoTxtFile, $"Error: Missing file '{SharedMethods.TRBResourceInfoFileString}.txt' in the extracted directory.");

            var resourceTypeTxtFile = Path.Combine(inExtractedTRBdir, $"{SharedMethods.TRBResourceTypeString}.txt");
            CheckFileExists(resourceTypeTxtFile, $"Error: Missing file '{SharedMethods.TRBResourceTypeString}.txt' in the extracted directory.");

            var resourceIdTxtFile = Path.Combine(inExtractedTRBdir, $"{SharedMethods.TRBResourceIDString}.txt");
            CheckFileExists(resourceIdTxtFile, $"Error: Missing file '{SharedMethods.TRBResourceIDString}.txt' in the extracted directory.");

            var resInfoList = new List<TRBResourceInfoEntry>();

            Console.WriteLine($"Getting data from {SharedMethods.TRBResourceInfoFileString}.txt file....");
            Console.WriteLine("");

            using (var resInfoReader = new StreamReader(resourceInfoTxtFile))
            {
                string readLine;
                uint lineCounter = 0;
                trbHeader.ResourceCount = 0;

                while ((readLine = resInfoReader.ReadLine()) != default)
                {
                    if (readLine.StartsWith("Version"))
                    {
                        var splitData = readLine.Split('=');

                        if (splitData.Length < 2)
                        {
                            SharedMethods.ErrorExit($"Version info is not specified properly. occured at line {lineCounter}!");
                        }

                        if (!uint.TryParse(splitData[1].Trim(), out trbHeader.Version))
                        {
                            SharedMethods.ErrorExit($"Version value is not specified properly. occured at line {lineCounter}!");
                        }

                        lineCounter++;
                        continue;
                    }

                    if (readLine.StartsWith("MainType"))
                    {
                        var splitData = readLine.Split('=');

                        if (splitData.Length < 2)
                        {
                            SharedMethods.ErrorExit($"MainType info is not specified properly. occured at line {lineCounter}!");
                        }

                        if (!byte.TryParse(splitData[1].Trim(), out trbHeader.MainType))
                        {
                            SharedMethods.ErrorExit($"MainType id is not specified properly. occured at line {lineCounter}!");
                        }

                        lineCounter++;
                        continue;
                    }

                    if (readLine.StartsWith("Index"))
                    {
                        var mainSplitData = readLine.Split('|');

                        if (mainSplitData.Length < 2)
                        {
                            SharedMethods.ErrorExit($"Index and Type data is not specified properly. occured at line {lineCounter}!");
                        }

                        var splitData = mainSplitData[0].Split('=');

                        if (splitData.Length < 2)
                        {
                            SharedMethods.ErrorExit($"Index data is not specified properly. occured at line {lineCounter}!");
                        }

                        if (!uint.TryParse(splitData[1].Trim(), out uint currentIndex))
                        {
                            SharedMethods.ErrorExit($"Index id is not specified properly. occured at line {lineCounter}!");
                        }

                        splitData = mainSplitData[1].Split('=');

                        if (splitData.Length < 2)
                        {
                            SharedMethods.ErrorExit($"Type data is not specified properly. occured at line {lineCounter}!");
                        }

                        if (!uint.TryParse(splitData[1].Trim(), out uint currentType))
                        {
                            SharedMethods.ErrorExit($"Index id is not specified properly. occured at line {lineCounter}!");
                        }

                        resInfoList.Add(new TRBResourceInfoEntry() { ResourceIndex = currentIndex, ResourceOffset = 0, ResourceSize = 0, ResourceType = currentType });

                        lineCounter++;
                        trbHeader.ResourceCount++;
                        continue;
                    }
                }
            }

            Console.WriteLine($"Getting data from {SharedMethods.TRBResourceIDString}.txt file....");
            Console.WriteLine("");
            var resourceIDs = DeserializeResourceIDs(trbHeader.ResourceCount, resourceIdTxtFile);

            Console.WriteLine($"Getting data from {SharedMethods.TRBResourceTypeString}.txt file....");
            Console.WriteLine("");
            var resourceTypes = DeserializeResourceTypes(trbHeader.ResourceCount, resourceTypeTxtFile);

            var trbPackData = new TRBPackData()
            {
                Header = trbHeader,
                ResourceInfoTable = resInfoList.ToArray(),
                ResourceIDTable = resourceIDs,
                ResourceTypesTable = resourceTypes
            };

            return trbPackData;
        }


        private static string[] DeserializeResourceIDs(uint resourceCount, string resourceIdTxtFile)
        {
            var resourceIDTable = new string[resourceCount * 2];

            using (var resIDReader = new StreamReader(resourceIdTxtFile))
            {
                string readLine;
                int index = 0;

                while ((readLine = resIDReader.ReadLine()) != default)
                {
                    resourceIDTable[index] = readLine;
                    index++;
                }
            }

            return resourceIDTable;
        }

        private static string[] DeserializeResourceTypes(uint resourceCount, string resourceTypeTxtFile)
        {
            var resourceTypeTable = new string[resourceCount];

            using (var resTypeReader = new StreamReader(resourceTypeTxtFile))
            {
                string readLine;
                int index = 0;

                while ((readLine = resTypeReader.ReadLine()) != default)
                {
                    resourceTypeTable[index] = readLine;

                    if (readLine.Length > 4)
                    {
                        SharedMethods.ErrorExit($"Specified type '{readLine}' is more than 4 bytes in size. occured at line_{index}!");
                    }

                    index++;
                }

                resourceTypeTable[index] = "";
                resourceTypeTable[index + 1] = "";
            }

            return resourceTypeTable;
        }

        private static byte[] BuildResourceIDsSection(string[] resourceIDTable, uint resourceCount)
        {
            var resIDsData = Array.Empty<byte>();

            using (var resIdsStream = new MemoryStream())
            {
                using (var resIdsWriter = new BinaryWriter(resIdsStream))
                {
                    var isFullPath = false;

                    for (int i = 0; i < resourceIDTable.Length; i++)
                    {
                        if (!isFullPath && i == resourceCount)
                        {
                            isFullPath = true;
                        }

                        var id = resourceIDTable[i];
                        var currentIDData = Encoding.ASCII.GetBytes(id);
                        var currentIDLength = id.Length;

                        if (isFullPath)
                        {
                            resIdsWriter.Write(currentIDData);
                            resIdsWriter.Write(byte.MinValue);
                            continue;
                        }

                        if (id == "|-undefined-|")
                        {
                            resIdsWriter.Write(new byte[16]);
                            continue;
                        }

                        resIdsWriter.Write(currentIDData);

                        uint padSize = 0;

                        if (DeterminePadding(16, (uint)currentIDLength, ref padSize))
                        {
                            resIdsWriter.Write(new byte[padSize]);
                        }
                    }
                }

                resIdsStream.Position = 0;
                resIDsData = resIdsStream.ToArray();
            }

            return resIDsData;
        }

        private static byte[] BuildResourceTypesSection(string[] resourceTypes)
        {
            var resTypesData = Array.Empty<byte>();

            using (var resTypesStream = new MemoryStream())
            {
                using (var resTypesWriter = new BinaryWriter(resTypesStream))
                {
                    for (int i = 0; i < resourceTypes.Length; i++)
                    {
                        var type = resourceTypes[i];

                        if (string.IsNullOrEmpty(type))
                        {
                            resTypesWriter.Write(BitConverter.GetBytes(uint.MaxValue));
                            continue;
                        }

                        var currentTypeData = Encoding.ASCII.GetBytes(type);
                        var currentTypeLength = type.Length;
                        Array.Reverse(currentTypeData);

                        resTypesWriter.Write(currentTypeData);

                        if (currentTypeLength < 4)
                        {
                            resTypesWriter.Write(new byte[4 - currentTypeLength]);
                        }
                    }
                }

                resTypesStream.Position = 0;
                resTypesData = resTypesStream.ToArray();
            }

            return resTypesData;
        }

        public static TRB DeserializeResourceInfo(string resourceInfoTxtFile)
        {
            var trb = new TRB();
            var resInfoList = new List<(uint, uint)>();

            using (var resInfoReader = new StreamReader(resourceInfoTxtFile))
            {
                string readLine;
                uint lineCounter = 0;
                trb.ResourceCount = 0;

                while ((readLine = resInfoReader.ReadLine()) != default)
                {
                    if (readLine.StartsWith("Version"))
                    {
                        var splitData = readLine.Split('=');

                        if (splitData.Length < 2)
                        {
                            SharedMethods.ErrorExit($"Version info is not specified properly. occured at line {lineCounter}!");
                        }

                        if (!uint.TryParse(splitData[1].Trim(), out trb.Version))
                        {
                            SharedMethods.ErrorExit($"Version id is not specified properly. occured at line {lineCounter}!");
                        }

                        lineCounter++;
                        continue;
                    }

                    if (readLine.StartsWith("MainType"))
                    {
                        var splitData = readLine.Split('=');

                        if (splitData.Length < 2)
                        {
                            SharedMethods.ErrorExit($"MainType info is not specified properly. occured at line {lineCounter}!");
                        }

                        if (!byte.TryParse(splitData[1].Trim(), out trb.MainType))
                        {
                            SharedMethods.ErrorExit($"MainType id is not specified properly. occured at line {lineCounter}!");
                        }

                        lineCounter++;
                        continue;
                    }

                    if (readLine.StartsWith("Index"))
                    {
                        var mainSplitData = readLine.Split('|');

                        if (mainSplitData.Length < 2)
                        {
                            SharedMethods.ErrorExit($"Index and Type data is not specified properly. occured at line {lineCounter}!");
                        }

                        var splitData = mainSplitData[0].Split('=');

                        if (splitData.Length < 2)
                        {
                            SharedMethods.ErrorExit($"Index data is not specified properly. occured at line {lineCounter}!");
                        }

                        if (!uint.TryParse(splitData[1].Trim(), out uint currentIndex))
                        {
                            SharedMethods.ErrorExit($"Index id is not specified properly. occured at line {lineCounter}!");
                        }

                        splitData = mainSplitData[1].Split('=');

                        if (splitData.Length < 2)
                        {
                            SharedMethods.ErrorExit($"Type data is not specified properly. occured at line {lineCounter}!");
                        }

                        if (!uint.TryParse(splitData[1].Trim(), out uint currentType))
                        {
                            SharedMethods.ErrorExit($"Index id is not specified properly. occured at line {lineCounter}!");
                        }

                        resInfoList.Add((currentIndex, currentType));
                        lineCounter++;
                        trb.ResourceCount++;
                        continue;
                    }
                }
            }

            trb.ResourceInfo = new (uint, uint)[trb.ResourceCount];
            trb.ResourceInfo = resInfoList.ToArray();

            return trb;
        }

        public static List<string> DeserializeResourceType(string resourceTypeTxtFile)
        {
            var resourceTypeList = new List<string>();

            using (var resTypeReader = new StreamReader(resourceTypeTxtFile))
            {
                string readLine;

                while ((readLine = resTypeReader.ReadLine()) != default)
                {
                    resourceTypeList.Add(readLine);
                }

                resourceTypeList.Add("");
                resourceTypeList.Add("");
            }

            return resourceTypeList;
        }

        public static List<string> DeserializeResourceID(string resourceIdTxtFile)
        {
            var resourceIDlist = new List<string>();

            using (var resIDReader = new StreamReader(resourceIdTxtFile))
            {
                string readLine;

                while ((readLine = resIDReader.ReadLine()) != default)
                {
                    if (readLine == "|-null-|")
                    {
                        resourceIDlist.Add("");
                        continue;
                    }

                    resourceIDlist.Add(readLine);
                }
            }

            return resourceIDlist;
        }

        public static byte[] BuildResourceType(List<string> resourceTypelist)
        {
            var resTypeDataList = new List<byte>();

            foreach (var type in resourceTypelist)
            {
                if (string.IsNullOrEmpty(type))
                {
                    resTypeDataList.AddRange(BitConverter.GetBytes(uint.MaxValue));
                    continue;
                }

                var currentTypeData = Encoding.ASCII.GetBytes(type);
                var currentTypeLength = type.Length;
                Array.Reverse(currentTypeData);

                resTypeDataList.AddRange(currentTypeData);

                if (currentTypeLength < 4)
                {
                    resTypeDataList.AddRange(new byte[4 - currentTypeLength]);
                }
            }

            var resourceTypeData = resTypeDataList.ToArray();

            return resourceTypeData;
        }

        public static byte[] BuildResourceID(List<string> resourceIDList, uint resourceCount)
        {
            var resIDDataList = new List<byte>();
            var isFullPath = false;

            for (int i = 0; i < resourceIDList.Count; i++)
            {
                if (!isFullPath && i == resourceCount)
                {
                    isFullPath = true;
                }

                var id = resourceIDList[i];
                var currentIDData = Encoding.ASCII.GetBytes(id);
                var currentIDLength = id.Length;

                if (isFullPath)
                {
                    resIDDataList.AddRange(currentIDData);
                    resIDDataList.Add(byte.MinValue);

                    continue;
                }

                if (string.IsNullOrEmpty(id))
                {
                    resIDDataList.AddRange(new byte[16]);
                    continue;
                }

                resIDDataList.AddRange(currentIDData);

                uint padSize = 0;

                if (DeterminePadding(16, (uint)currentIDLength, ref padSize))
                {
                    resIDDataList.AddRange(new byte[padSize]);
                }
            }

            var resIDData = resIDDataList.ToArray();

            return resIDData;
        }

        private static bool DeterminePadding(int padValue, uint size, ref uint padSize)
        {
            var remainder = size % padValue;

            if (remainder != 0)
            {
                padSize = (uint)(padValue - remainder);
                return true;
            }

            return false;
        }

        public static void UpdateOffset(BinaryWriter resInfoWriter, long writePos, uint currentIndex, uint currentResourceOffset, uint currentResourceSize, uint currentTypeValue)
        {
            _ = resInfoWriter.BaseStream.Position = writePos;
            resInfoWriter.Write(currentIndex);
            resInfoWriter.Write(currentResourceOffset);
            resInfoWriter.Write(currentResourceSize);
            resInfoWriter.Write(currentTypeValue);
        }

        public static void DoPadding(int padValue, FileStream mainDataStream)
        {
            uint padSize = 0;
            uint position = (uint)mainDataStream.Length;

            if (DeterminePadding(padValue, position, ref padSize))
            {
                var paddingData = new byte[padSize];
                mainDataStream.Write(paddingData, 0, (int)padSize);
            }
        }

        public static void CheckFileExists(string fileToCheck, string errorMsg)
        {
            if (!File.Exists(fileToCheck))
            {
                SharedMethods.ErrorExit(errorMsg);
            }
        }
    }
}