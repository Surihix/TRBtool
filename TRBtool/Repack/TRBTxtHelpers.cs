using System;
using System.Collections.Generic;
using System.IO;
using TRBtool.Structures;
using TRBtool.Support;

namespace TRBtool.Repack
{
    internal class TRBTxtHelpers
    {
        public static TRBPackData GetTRBPackDataFromTxts(string inExtractedTRBdir)
        {
            var trbHeader = new TRBHeader()
            {
                Magic = "SEDBRES ",
                HeaderSize = 64,
                FileType = "brt\0"
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
            trbHeader.ResourceIDsCount = trbHeader.ResourceCount;

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

        public static void CheckFileExists(string fileToCheck, string errorMsg)
        {
            if (!File.Exists(fileToCheck))
            {
                SharedMethods.ErrorExit(errorMsg);
            }
        }
    }
}