using IMGBlibrary.Support;
using IMGBlibrary.Unpack;
using System;
using System.IO;

namespace TRBtool
{
    internal partial class TRB
    {
        public static void UnpackTRB(string inTRBfile)
        {
            var inTRBfileDir = Path.GetDirectoryName(inTRBfile);
            var inTRBfileName = Path.GetFileName(inTRBfile);
            var extractTRBdir = Path.Combine(inTRBfileDir, "_" + inTRBfileName);

            var platform = IMGBEnums.Platforms.win32;

            if (inTRBfileName.EndsWith("ps3.trb"))
            {
                platform = IMGBEnums.Platforms.ps3;
            }
            else if (inTRBfileName.EndsWith("x360.trb"))
            {
                platform = IMGBEnums.Platforms.x360;
            }

            DeleteDirIfExists(extractTRBdir);

            var inIMGBfileName = Path.GetFileNameWithoutExtension(inTRBfile) + ".imgb";
            var inTRBimgbFile = Path.Combine(inTRBfileDir, inIMGBfileName);
            var extractIMGBdir = Path.Combine(inTRBfileDir, "_" + inIMGBfileName);

            if (File.Exists(inTRBimgbFile))
            {
                DeleteDirIfExists(extractIMGBdir);
            }


            Console.WriteLine("");

            using (var trbStream = new FileStream(inTRBfile, FileMode.Open, FileAccess.Read))
            {
                using (var trbReader = new BinaryReader(trbStream))
                {
                    trbReader.BaseStream.Position = 0;
                    var trbHeader = trbReader.ReadBytesString(8, false);

                    if (trbHeader != "SEDBRES ")
                    {
                        SharedMethods.ErrorExit("Error: Not a valid TRB file");
                    }

                    var trbSize = (uint)trbStream.Length;

                    trbReader.BaseStream.Position = 52;
                    var resourceIdsPathsStart = trbReader.ReadUInt32();
                    var resourceCount = trbReader.ReadUInt32();
                    var dataStart = 64 + (resourceCount * 16);
                    var resourceTypeIndex = resourceCount - 1;

                    trbReader.BaseStream.Position = 64 + ((resourceCount - 2) * 16) + 4;
                    var resourceTypeStart = trbReader.ReadUInt32();

                    trbReader.BaseStream.Position = 64 + (resourceTypeIndex * 16) + 4;
                    var resourceIdsStart = trbReader.ReadUInt32();


                    uint resourceOffsetReadPos = 68;
                    var resourceIdsPathsReadPos = dataStart + resourceIdsPathsStart;
                    var resourceTypeReadPos = dataStart + resourceTypeStart;

                    var currentResourceIdPath = string.Empty;
                    var currentResourceType = string.Empty;
                    uint currentResourceStart = 0;
                    uint currentResourceSize = 0;

                    for (int i = 1; i < resourceCount + 1; i++)
                    {
                        trbReader.BaseStream.Position = resourceIdsPathsReadPos;
                        currentResourceIdPath = trbReader.ReadStringTillNull();
                        resourceIdsPathsReadPos = (uint)trbReader.BaseStream.Position;

                        if (i < resourceTypeIndex)
                        {
                            trbReader.BaseStream.Position = resourceOffsetReadPos;
                            currentResourceStart = trbReader.ReadUInt32() + dataStart;
                            currentResourceSize = trbReader.ReadUInt32();

                            trbReader.BaseStream.Position = resourceTypeReadPos;
                            currentResourceType = trbReader.ReadBytesString(4, true);
                        }
                        else
                        {
                            if (i == resourceTypeIndex)
                            {
                                trbReader.BaseStream.Position = resourceOffsetReadPos;
                                currentResourceStart = trbReader.ReadUInt32() + dataStart;
                                currentResourceSize = (resourceIdsStart + dataStart) - currentResourceStart;
                            }
                            else
                            {
                                trbReader.BaseStream.Position = resourceOffsetReadPos;
                                currentResourceStart = trbReader.ReadUInt32() + dataStart;
                                currentResourceSize = (uint)trbStream.Length - currentResourceStart;
                            }
                        }

                        var extractFilePath = Path.Combine(extractTRBdir, currentResourceIdPath + "." + currentResourceType);
                        var currentFileDir = Path.GetDirectoryName(extractFilePath);

                        if (!Directory.Exists(currentFileDir))
                        {
                            Directory.CreateDirectory(currentFileDir);
                        }

                        using (var ofs = new FileStream(extractFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            trbStream.Position = currentResourceStart;
                            trbStream.CopyStreamTo(ofs, currentResourceSize, false);
                        }

                        Console.WriteLine("Unpacked " + extractFilePath);

                        if (Enum.TryParse(currentResourceType, false, out IMGBEnums.FileExtensions fileExtension) == true)
                        {
                            if (File.Exists(inTRBimgbFile))
                            {
                                if (!Directory.Exists(extractIMGBdir))
                                {
                                    Directory.CreateDirectory(extractIMGBdir);
                                }

                                Console.WriteLine("Detected Image header file");
                                IMGBUnpack.UnpackIMGB(extractFilePath, inTRBimgbFile, extractIMGBdir, platform, true);
                            }
                        }

                        Console.WriteLine("");

                        resourceOffsetReadPos += 16;
                        resourceTypeReadPos += 4;
                        currentResourceIdPath = string.Empty;
                        currentResourceType = string.Empty;
                    }


                    var trbOffsetsFile = Path.Combine(extractTRBdir, SharedMethods.TRBOffsetsFile);

                    using (var trbOffsets = new FileStream(trbOffsetsFile, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        trbStream.Position = 0;
                        trbStream.CopyStreamTo(trbOffsets, dataStart, false);
                    }

                    Console.WriteLine("Copied resource offsets to '" + trbOffsetsFile + "'");
                    Console.WriteLine("");
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