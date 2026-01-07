using IMGBlibrary.Support;
using IMGBlibrary.Unpack;
using System;
using System.IO;

namespace TRBtool.TRBtool
{
    internal class TRB
    {
        #region Unpack TRB
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

            using (var trbReader = new BinaryReader(new FileStream(inTRBfile, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                // Parse header
                var sedbResMagic = trbReader.ReadBytesString(8, false);

                if (sedbResMagic != "SEDBRES ")
                {
                    SharedMethods.ErrorExit("Error: Not a valid TRB file");
                }

                var version = trbReader.ReadUInt32();
                var endiannessFlag = trbReader.ReadByte();
                var trbType = trbReader.ReadByte();
                var headerSize = trbReader.ReadUInt16();
                var trbDataSize = trbReader.ReadUInt32();

                _ = trbReader.BaseStream.Position += 28;
                var resourceCount = trbReader.ReadUInt32();
                var resourceIDsOffset = trbReader.ReadUInt32();
                var resourceCount2 = trbReader.ReadUInt32();
                var trbExtn = trbReader.ReadBytesString(4, true);

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

                    _ = trbReader.BaseStream.Position = offsetAdjust + resourceDataOffset;

                    if (currentResourceId == "RESOURCE_TYPE")
                    {
                        using (var resTypeStreamWriter = new StreamWriter(currentFile + "txt", true, System.Text.Encoding.UTF8))
                        {
                            foreach (var item in resourceTypesBuffer)
                            {
                                resTypeStreamWriter.WriteLine(item);
                            }
                        }

                        lastInfoPos += 16;
                        continue;
                    }

                    if (currentResourceId == "RESOURCE_ID")
                    {
                        using (var resIDsStreamWriter = new StreamWriter(currentFile + "txt", true, System.Text.Encoding.UTF8))
                        {
                            for (int j = 0; j < resourceCount; j++)
                            {
                                var resourceIDread = trbReader.ReadBytesString(16, false);

                                if (string.IsNullOrEmpty(resourceIDread))
                                {
                                    resIDsStreamWriter.WriteLine("null");
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

                    if (Enum.TryParse(currentResourceType, false, out IMGBEnums.FileExtensions fileExtension) == true)
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

                using (var resourceInfoStream = new StreamWriter(Path.Combine(extractTRBdir, "RESOURCE_INFO.txt"), true))
                {
                    foreach (var item in resInfoBuffer)
                    {
                        resourceInfoStream.WriteLine($"Index = {item.Item1} | Type = {item.Item2}");
                    }
                }
            }
        }

        private static void DeleteDirIfExists(string directoryName)
        {
            if (Directory.Exists(directoryName))
            {
                Directory.Delete(directoryName, true);
            }
        }
        #endregion


        #region Repack TRB
        public static void RepackTRB(string inExtractedTRBdir)
        {

        }
        #endregion
    }
}