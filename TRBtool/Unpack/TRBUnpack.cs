using IMGBlibrary.Support;
using IMGBlibrary.Unpack;
using System;
using System.IO;
using System.Text;
using TRBtool.Support;

namespace TRBtool.Unpack
{
    internal class TRBUnpack
    {
        public static void InitiateUnpack(string inTRBfile)
        {
            var inTRBfileDir = Path.GetDirectoryName(inTRBfile);
            var inTRBfileName = Path.GetFileName(inTRBfile);
            var extractTRBdir = Path.Combine(inTRBfileDir, "_" + inTRBfileName);

            var platform = SharedMethods.GetPlatform(inTRBfileName);

            DeleteDirIfExists(extractTRBdir);

            var inIMGBfileName = Path.GetFileNameWithoutExtension(inTRBfile) + ".imgb";
            var inTRBimgbFile = Path.Combine(inTRBfileDir, inIMGBfileName);
            var extractIMGBdir = Path.Combine(inTRBfileDir, "_" + inIMGBfileName);

            if (File.Exists(inTRBimgbFile))
            {
                DeleteDirIfExists(extractIMGBdir);
            }

            Console.WriteLine("");

            var trbLoadData = TRBFileLoader.LoadTRBFile(inTRBfile);

            var trbHeader = trbLoadData.Header;
            var trbResourceInfoTable = trbLoadData.ResourceInfoTable;
            var trbResourceIDTable = trbLoadData.ResourceIDTable;
            var trbResourceTypeTable = trbLoadData.ResourceTypeTable;

            Console.WriteLine("Unpacking resources....");
            Console.WriteLine("");

            using (var trbFileReader = new BinaryReader(new FileStream(inTRBfile, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                for (int i = 0; i < trbHeader.ResourceCount; i++)
                {
                    var currentResourceID = trbResourceIDTable[i];

                    string currentResourceType;

                    if (currentResourceID == SharedMethods.TRBResourceTypeString || currentResourceID == SharedMethods.TRBResourceIDString)
                    {
                        currentResourceType = "txt";
                    }
                    else
                    {
                        currentResourceType = trbResourceTypeTable[i];
                    }

                    var currentFile = Path.Combine(extractTRBdir, $"{currentResourceID}.{currentResourceType}");
                    var currentFileDir = Path.GetDirectoryName(currentFile);

                    if (!Directory.Exists(currentFileDir))
                    {
                        Directory.CreateDirectory(currentFileDir);
                    }

                    if (currentResourceType == "txt")
                    {
                        using (var txtTypeWriter = new StreamWriter(currentFile, true, new UTF8Encoding(false)))
                        {
                            if (currentResourceID == SharedMethods.TRBResourceTypeString)
                            {
                                for (int j = 0; j < trbResourceTypeTable.Length; j++)
                                {
                                    txtTypeWriter.WriteLine(trbResourceTypeTable[j]);
                                }
                            }
                            else
                            {
                                _ = trbFileReader.BaseStream.Position = trbLoadData.ResourcesDataStartOffset + trbResourceInfoTable[i].ResourceOffset;

                                for (int j = 0; j < trbHeader.ResourceIDsCount; j++)
                                {
                                    var currentID = trbFileReader.ReadBytesString(16, false);

                                    if (string.IsNullOrEmpty(currentID))
                                    {
                                        txtTypeWriter.WriteLine("|-null-|");
                                        continue;
                                    }

                                    txtTypeWriter.WriteLine(currentID);
                                }

                                foreach (var id in trbResourceIDTable)
                                {
                                    txtTypeWriter.WriteLine(id);
                                }
                            }
                        }

                        Console.WriteLine($"Unpacked {currentFile}");
                        continue;
                    }

                    using (var resourceStream = new FileStream(currentFile, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        _ = trbFileReader.BaseStream.Position = trbLoadData.ResourcesDataStartOffset + trbResourceInfoTable[i].ResourceOffset;
                        trbFileReader.BaseStream.CopyStreamTo(resourceStream, trbResourceInfoTable[i].ResourceSize, false);
                    }

                    Console.WriteLine($"Unpacked {currentFile}");

                    if (Enum.TryParse(currentResourceType, false, out IMGBFlags.FileExtensions fileExtension) == true)
                    {
                        if (File.Exists(inTRBimgbFile))
                        {
                            if (!Directory.Exists(extractIMGBdir))
                            {
                                Directory.CreateDirectory(extractIMGBdir);
                            }

                            Console.WriteLine("Detected Image header file");
                            IMGBUnpack.UnpackIMGB(currentFile, inTRBimgbFile, extractIMGBdir, platform, true);
                            Console.WriteLine("");
                        }
                    }
                }
            }

            var resInfoTxt = Path.Combine(extractTRBdir, $"{SharedMethods.TRBResourceInfoFileString}.txt");
            SharedMethods.IfFileExistsDel(resInfoTxt);

            using (var resourceInfoStream = new StreamWriter(resInfoTxt, true, new UTF8Encoding(false)))
            {
                resourceInfoStream.WriteLine($"Version = {trbHeader.Version}");
                resourceInfoStream.WriteLine($"MainType = {trbHeader.MainType}");

                for (int i = 0; i < trbResourceInfoTable.Length; i++)
                {
                    resourceInfoStream.WriteLine($"Index = {trbResourceInfoTable[i].ResourceIndex} | Type = {trbResourceInfoTable[i].ResourceType}");
                }
            }

            Console.WriteLine("");
            Console.WriteLine("Finished unpacking file " + "\"" + Path.GetFileName(inTRBfile) + "\"");
        }

        private static void DeleteDirIfExists(string directoryName)
        {
            if (Directory.Exists(directoryName))
            {
                Directory.Delete(directoryName, true);
            }
        }
    }
}