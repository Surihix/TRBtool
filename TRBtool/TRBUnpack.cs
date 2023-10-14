using BinaryReaderEx;
using IMGBlibrary;
using StreamExtension;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace TRBtool
{
    internal partial class TRB
    {
        public static void UnpackTRB(string inTRBfile)
        {
            var inTRBfileDir = Path.GetDirectoryName(inTRBfile);
            var extractTRBdir = Path.Combine(inTRBfileDir, "_" + Path.GetFileName(inTRBfile));

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
                    var trbSize = (uint)trbStream.Length;

                    trbReader.BaseStream.Position = 52;
                    var resourceIdsPathsStart = trbReader.ReadUInt32();
                    var resourceCount = trbReader.ReadUInt32();
                    var resourceDataStart = 64 + (resourceCount * 16);

                    var resourceBeforeTypeIndex = resourceCount - 2;
                    var resourceTypeIndex = resourceCount - 1;

                    trbReader.BaseStream.Position = 64 + (resourceBeforeTypeIndex * 16) + 4;
                    var resourceTypeStart = trbReader.ReadUInt32();

                    trbReader.BaseStream.Position = 64 + (resourceTypeIndex * 16) + 4;
                    var resourceIdsStart = trbReader.ReadUInt32();


                    uint resourceOffsetReadPos = 68;
                    var resourceIdsPathsReadPos = resourceDataStart + resourceIdsPathsStart;
                    var resourceTypeReadPos = resourceDataStart + resourceTypeStart;
                    var trbRresourceIndex = 1;

                    uint currentResourceStart = 0;
                    uint currentResourceSize = 0;

                    while (trbRresourceIndex <= resourceCount)
                    {
                        trbReader.BaseStream.Position = resourceIdsPathsReadPos;
                        var currentResourceIdPath = trbReader.ReadStringTillNull();
                        var nextResourceIdPathStart = (uint)trbReader.BaseStream.Position;

                        var currentResourceType = "";
                        if (trbRresourceIndex >= resourceTypeIndex)
                        {
                            if (trbRresourceIndex == resourceTypeIndex)
                            {
                                trbReader.BaseStream.Position = resourceOffsetReadPos;
                                currentResourceStart = trbReader.ReadUInt32() + resourceDataStart;
                                currentResourceSize = (resourceIdsStart + resourceDataStart) - currentResourceStart;
                            }

                            if (trbRresourceIndex > resourceTypeIndex)
                            {
                                trbReader.BaseStream.Position = resourceOffsetReadPos;
                                currentResourceStart = trbReader.ReadUInt32() + resourceDataStart;
                                currentResourceSize = trbSize - currentResourceStart;
                            }
                        }
                        else
                        {
                            trbReader.BaseStream.Position = resourceOffsetReadPos;
                            currentResourceStart = trbReader.ReadUInt32() + resourceDataStart;
                            currentResourceSize = trbReader.ReadUInt32();

                            trbReader.BaseStream.Position = resourceTypeReadPos;
                            var resourceTypeByteArray = trbReader.ReadBytes(4);
                            Array.Reverse(resourceTypeByteArray);
                            currentResourceType = "." + Encoding.ASCII.GetString(resourceTypeByteArray).Replace("\0", "");
                        }

                        var extractFilePath = Path.Combine(extractTRBdir, currentResourceIdPath + currentResourceType);

                        var currentFileDir = Path.GetDirectoryName(extractFilePath);
                        if (!Directory.Exists(currentFileDir))
                        {
                            Directory.CreateDirectory(currentFileDir);
                        }

                        using (var ofs = new FileStream(extractFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            trbStream.ExCopyTo(ofs, currentResourceStart, currentResourceSize);
                        }

                        Console.WriteLine("Unpacked " + extractFilePath);

                        if (ImageMethods.ImgHeaderBlockFileExtensions.Contains(currentResourceType))
                        {
                            if (File.Exists(inTRBimgbFile))
                            {
                                if (!Directory.Exists(extractIMGBdir))
                                {
                                    Directory.CreateDirectory(extractIMGBdir);
                                }

                                Console.WriteLine("Detected Image header file");
                                ImageMethods.UnpackIMGB(extractFilePath, inTRBimgbFile, extractIMGBdir);
                            }
                        }

                        Console.WriteLine("");

                        resourceOffsetReadPos += 16;
                        resourceIdsPathsReadPos = nextResourceIdPathStart;
                        resourceTypeReadPos += 4;
                        trbRresourceIndex++;
                    }

                    var trbOffsetsFile = Path.Combine(extractTRBdir, CmnMethods.TRBOffsetsFile);

                    using (var trbOffsets = new FileStream(trbOffsetsFile, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        trbStream.ExCopyTo(trbOffsets, 0, resourceDataStart);
                    }

                    Console.WriteLine("Copied resource offsets to '" + trbOffsetsFile + "'");
                    Console.WriteLine("");
                }
            }


            Console.WriteLine("");
            Console.WriteLine("Finished unpacking " + Path.GetFileName(inTRBfile));
            Console.ReadLine();
        }


        static void DeleteDirIfExists(string directoryName)
        {
            if (Directory.Exists(directoryName))
            {
                Directory.Delete(directoryName, true);
            }
        }
    }
}