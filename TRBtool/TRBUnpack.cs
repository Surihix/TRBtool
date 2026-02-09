using IMGBlibrary.Support;
using IMGBlibrary.Unpack;
using System;
using System.IO;
using TRBtool.Support;

namespace TRBtool
{
    internal class TRBUnpack
    {
        public static void InitiateUnpack(string inTRBfile)
        {
            var inTRBfileDir = Path.GetDirectoryName(inTRBfile);
            var inTRBfileName = Path.GetFileName(inTRBfile);
            var extractTRBdir = Path.Combine(inTRBfileDir, "_" + inTRBfileName);

            var platform = TRBMethods.GetPlatform(inTRBfileName);

            DeleteDirIfExists(extractTRBdir);

            var inIMGBfileName = Path.GetFileNameWithoutExtension(inTRBfile) + ".imgb";
            var inTRBimgbFile = Path.Combine(inTRBfileDir, inIMGBfileName);
            var extractIMGBdir = Path.Combine(inTRBfileDir, "_" + inIMGBfileName);

            if (File.Exists(inTRBimgbFile))
            {
                DeleteDirIfExists(extractIMGBdir);
            }

            Console.WriteLine("");

            var trb = new TRB();

            using (var trbReader = new BinaryReader(new FileStream(inTRBfile, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                // Parse header
                var sedbResMagic = trbReader.ReadBytesString(8, false);

                if (sedbResMagic != "SEDBRES ")
                {
                    TRBMethods.ErrorExit("Error: Not a valid TRB file");
                }

                var version = trbReader.ReadUInt32();
                var endiannessFlag = trbReader.ReadByte();
                var mainType = trbReader.ReadByte();
                var headerSize = trbReader.ReadUInt16();
                var trbDataSize = trbReader.ReadUInt32();

                _ = trbReader.BaseStream.Position += 28;
                var resourceCount = trbReader.ReadUInt32();
                var resourceIDsOffset = trbReader.ReadUInt32();
                var resourceCount2 = trbReader.ReadUInt32();
                var trbExtn = trbReader.ReadBytesString(4, true);

                trb.Version = version;
                trb.MainType = mainType;
                trb.ResourceCount = resourceCount;

                var resourceInfoTableSize = resourceCount * 16;

                // Get RESOURCE_IDs
                var resourceIDsBuffer = new string[(int)resourceCount];
                _ = trbReader.BaseStream.Position = (resourceIDsOffset + headerSize) + (resourceCount * 16);

                for (int i = 0; i < resourceCount; i++)
                {
                    var currentResourceID = trbReader.ReadStringTillNull();
                    resourceIDsBuffer[i] = currentResourceID;
                }

                // Get RESOURCE_TYPEs
                var resourceTypeSectionIndex = resourceCount - 2;
                var resourceIDSectionIndex = resourceCount - 1;

                var resourceTypesBuffer = new string[(int)resourceCount];
                _ = trbReader.BaseStream.Position = (headerSize + resourceInfoTableSize) - 28;

                var resourceTypeSectionOffset = trbReader.ReadUInt32();
                _ = trbReader.BaseStream.Position = (headerSize + resourceInfoTableSize) + resourceTypeSectionOffset;

                for (int i = 0; i < resourceCount; i++)
                {
                    if (i == resourceTypeSectionIndex || i == resourceIDSectionIndex)
                    {
                        break;
                    }

                    var currentType = trbReader.ReadBytesString(4, true);
                    resourceTypesBuffer[i] = currentType;
                }

                // Unpack Resources
                long lastInfoPos = headerSize;
                uint offsetAdjust = headerSize + resourceInfoTableSize;
                var resInfoBuffer = new (uint, uint)[resourceCount];

                for (int i = 0; i < resourceCount; i++)
                {
                    _ = trbReader.BaseStream.Position = lastInfoPos;
                    var resourceIndex = trbReader.ReadUInt32();
                    var resourceDataOffset = trbReader.ReadUInt32();
                    var resourceDataSize = trbReader.ReadUInt32();
                    var resourceType = trbReader.ReadUInt32();
                    resInfoBuffer[i] = (resourceIndex, resourceType);

                    var currentResourceId = resourceIDsBuffer[i];
                    var currentResourceType = resourceTypesBuffer[i];

                    var currentFile = Path.Combine(extractTRBdir, currentResourceId + "." + currentResourceType);
                    var currentFileDir = Path.GetDirectoryName(currentFile);

                    if (!Directory.Exists(currentFileDir))
                    {
                        Directory.CreateDirectory(currentFileDir);
                    }

                    TRBMethods.IfFileExistsDel(currentFile);

                    _ = trbReader.BaseStream.Position = offsetAdjust + resourceDataOffset;

                    if (currentResourceId == TRBMethods.TRBResourceTypeFile)
                    {
                        currentFile = currentFile + "txt";

                        using (var resTypeStreamWriter = new StreamWriter(currentFile, true, System.Text.Encoding.ASCII))
                        {
                            foreach (var item in resourceTypesBuffer)
                            {
                                if (string.IsNullOrEmpty(item))
                                {
                                    continue;
                                }

                                resTypeStreamWriter.WriteLine(item);
                            }
                        }

                        lastInfoPos += 16;
                        continue;
                    }

                    if (currentResourceId == TRBMethods.TRBResourceIDFile)
                    {
                        currentFile = currentFile + "txt";

                        using (var resIDsStreamWriter = new StreamWriter(currentFile, true, System.Text.Encoding.ASCII))
                        {
                            for (int j = 0; j < resourceCount; j++)
                            {
                                var resourceIDread = trbReader.ReadBytesString(16, false);

                                if (string.IsNullOrEmpty(resourceIDread))
                                {
                                    resIDsStreamWriter.WriteLine("|-null-|");
                                    continue;
                                }

                                resIDsStreamWriter.WriteLine(resourceIDread);
                            }

                            foreach (var item in resourceIDsBuffer)
                            {
                                resIDsStreamWriter.WriteLine(item);
                            }
                        }

                        lastInfoPos += 16;
                        continue;
                    }

                    using (var resourceStream = new FileStream(currentFile, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        trbReader.BaseStream.CopyStreamTo(resourceStream, resourceDataSize, false);
                    }

                    Console.WriteLine("Unpacked " + currentFile);

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

                    lastInfoPos += 16;
                }

                trb.ResourceInfo = resInfoBuffer;
            }

            var resInfoTxt = Path.Combine(extractTRBdir, $"{TRBMethods.TRBResourceInfoFile}.txt");

            TRBMethods.IfFileExistsDel(resInfoTxt);

            using (var resourceInfoStream = new StreamWriter(resInfoTxt, true))
            {
                resourceInfoStream.WriteLine($"Version = {trb.Version}");
                resourceInfoStream.WriteLine($"MainType = {trb.MainType}");

                foreach (var item in trb.ResourceInfo)
                {
                    resourceInfoStream.WriteLine($"Index = {item.Item1} | Type = {item.Item2}");
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