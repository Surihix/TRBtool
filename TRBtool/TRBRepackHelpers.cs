using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TRBtool.Support;

namespace TRBtool
{
    internal class TRBRepackHelpers
    {
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
                            TRBMethods.ErrorExit($"Version info is not specified properly. occured at line {lineCounter}!");
                        }

                        if (!uint.TryParse(splitData[1].Trim(), out trb.Version))
                        {
                            TRBMethods.ErrorExit($"Version id is not specified properly. occured at line {lineCounter}!");
                        }

                        lineCounter++;
                        continue;
                    }

                    if (readLine.StartsWith("MainType"))
                    {
                        var splitData = readLine.Split('=');

                        if (splitData.Length < 2)
                        {
                            TRBMethods.ErrorExit($"MainType info is not specified properly. occured at line {lineCounter}!");
                        }

                        if (!byte.TryParse(splitData[1].Trim(), out trb.MainType))
                        {
                            TRBMethods.ErrorExit($"MainType id is not specified properly. occured at line {lineCounter}!");
                        }

                        lineCounter++;
                        continue;
                    }

                    if (readLine.StartsWith("Index"))
                    {
                        var mainSplitData = readLine.Split('|');

                        if (mainSplitData.Length < 2)
                        {
                            TRBMethods.ErrorExit($"Index and Type data is not specified properly. occured at line {lineCounter}!");
                        }

                        var splitData = mainSplitData[0].Split('=');

                        if (splitData.Length < 2)
                        {
                            TRBMethods.ErrorExit($"Index data is not specified properly. occured at line {lineCounter}!");
                        }

                        if (!uint.TryParse(splitData[1].Trim(), out uint currentIndex))
                        {
                            TRBMethods.ErrorExit($"Index id is not specified properly. occured at line {lineCounter}!");
                        }

                        splitData = mainSplitData[1].Split('=');

                        if (splitData.Length < 2)
                        {
                            TRBMethods.ErrorExit($"Type data is not specified properly. occured at line {lineCounter}!");
                        }

                        if (!uint.TryParse(splitData[1].Trim(), out uint currentType))
                        {
                            TRBMethods.ErrorExit($"Index id is not specified properly. occured at line {lineCounter}!");
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
                TRBMethods.ErrorExit(errorMsg);
            }
        }
    }
}