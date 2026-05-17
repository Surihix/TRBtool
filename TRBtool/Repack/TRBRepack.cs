using IMGBlibrary.Repack;
using IMGBlibrary.Support;
using System;
using System.IO;
using System.Text;
using TRBtool.Support;

namespace TRBtool.Repack
{
    internal class TRBRepack
    {
        public static void InitiateRepack(string inExtractedTRBdir)
        {
            var outTRBfileName = Path.GetFileName(inExtractedTRBdir);

            if (outTRBfileName.StartsWith("_"))
            {
                outTRBfileName = Path.GetFileName(inExtractedTRBdir).Remove(0, 1);
            }

            var outTRBfileDir = Path.GetDirectoryName(inExtractedTRBdir);
            var outTRBfile = Path.Combine(outTRBfileDir, outTRBfileName);
            var tmpDataFile = Path.Combine(inExtractedTRBdir, "_tempData");

            var outIMGBfileName = Path.GetFileNameWithoutExtension(outTRBfileName) + ".imgb";
            var outIMGBfile = Path.Combine(outTRBfileDir, outIMGBfileName);
            var extractedIMGBdir = Path.Combine(outTRBfileDir, "_" + outIMGBfileName);

            var oldTRBfile = Path.Combine(outTRBfileDir, Path.GetFileName(outTRBfile) + ".old");
            var oldIMGBfile = Path.Combine(outTRBfileDir, Path.GetFileName(outIMGBfile) + ".old");

            SharedMethods.IfFileExistsDel(oldTRBfile);
            SharedMethods.IfFileExistsDel(oldIMGBfile);

            if (File.Exists(outTRBfile))
            {
                File.Move(outTRBfile, oldTRBfile);
            }
            if (File.Exists(outIMGBfile))
            {
                File.Move(outIMGBfile, oldIMGBfile);
            }

            var platform = SharedMethods.GetPlatform(outTRBfileName);

            Console.WriteLine("");

            var trbPackData = TRBTxtHelpers.GetTRBPackDataFromTxts(inExtractedTRBdir);

            var trbHeader = trbPackData.Header;
            var trbResourceInfoTable = trbPackData.ResourceInfoTable;
            var trbResourceIDTable = trbPackData.ResourceIDTable;
            var trbResourceTypeTable = trbPackData.ResourceTypesTable;

            var resourceInfoTableDataSize = trbHeader.ResourceCount * 16;
            File.WriteAllBytes(outTRBfile, new byte[64 + resourceInfoTableDataSize]);

            using (var resInfoWriter = new BinaryWriter(File.Open(outTRBfile, FileMode.Open, FileAccess.Write)))
            {
                _ = resInfoWriter.BaseStream.Position = 0;
                resInfoWriter.Write(Encoding.ASCII.GetBytes(trbHeader.Magic));
                resInfoWriter.Write(trbHeader.Version);

                _ = resInfoWriter.BaseStream.Position += 1;
                resInfoWriter.Write(trbHeader.MainType);
                resInfoWriter.Write(trbHeader.HeaderSize);

                _ = resInfoWriter.BaseStream.Position += 32;
                resInfoWriter.Write(trbHeader.ResourceIDsCount);

                _ = resInfoWriter.BaseStream.Position += 4;
                resInfoWriter.Write(trbHeader.ResourceCount);
                resInfoWriter.Write(Encoding.ASCII.GetBytes(trbHeader.FileType));

                using (var mainDataStream = new FileStream(tmpDataFile, FileMode.Append, FileAccess.Write))
                {
                    var pathIndex = (int)trbHeader.ResourceIDsCount;
                    uint currentResourceOffset = 0;
                    uint currentResourceSize = 0;
                    long writePos = 64;

                    Console.WriteLine("Repacking resources....");
                    Console.WriteLine("");

                    for (int i = 0; i < trbHeader.ResourceCount; i++)
                    {
                        var currentResInfoIndex = trbResourceInfoTable[i].ResourceIndex;
                        var currentResInfoType = trbResourceInfoTable[i].ResourceType;

                        var currentID = trbResourceIDTable[pathIndex + i];
                        var currentType = trbResourceTypeTable[i];

                        var currentFile = Path.Combine(inExtractedTRBdir, $"{currentID}.{currentType}");

                        if (currentID == SharedMethods.TRBResourceTypeString)
                        {
                            var resourceTypeData = TRBRepackHelpers.BuildResourceTypesSection(trbResourceTypeTable);
                            mainDataStream.Write(resourceTypeData, 0, resourceTypeData.Length);

                            uint resourceTypeMemSize = 64 + (trbHeader.ResourceCount * 20);
                            TRBRepackHelpers.UpdateOffset(resInfoWriter, writePos, currentResInfoIndex, currentResourceOffset, resourceTypeMemSize, currentResInfoType);

                            mainDataStream.PadStream(16);
                            currentResourceOffset = (uint)mainDataStream.Position;

                            writePos += 16;
                            continue;
                        }

                        if (currentID == SharedMethods.TRBResourceIDString)
                        {
                            var resourceIDData = TRBRepackHelpers.BuildResourceIDsSection(trbResourceIDTable, trbHeader.ResourceCount);
                            mainDataStream.Write(resourceIDData, 0, resourceIDData.Length);

                            _ = resInfoWriter.BaseStream.Position = 52;
                            uint allPathsOffset = currentResourceOffset + (trbHeader.ResourceCount * 16);
                            resInfoWriter.Write(allPathsOffset);

                            uint resourceIDMemSize = 64 + (trbHeader.ResourceCount * 32);
                            TRBRepackHelpers.UpdateOffset(resInfoWriter, writePos, currentResInfoIndex, currentResourceOffset, resourceIDMemSize, currentResInfoType);
                            continue;
                        }

                        if (!File.Exists(currentFile))
                        {
                            Console.WriteLine($"Missing '{currentID}.{currentType}' file. skipped to next file.");

                            _ = resInfoWriter.BaseStream.Position = writePos;
                            TRBRepackHelpers.UpdateOffset(resInfoWriter, writePos, currentResInfoIndex, 0, 0, currentResInfoType);
                            continue;
                        }

                        var currentFileOG = currentFile + ".og";
                        SharedMethods.IfFileExistsDel(currentFileOG);

                        File.Move(currentFile, currentFileOG);
                        File.Copy(currentFileOG, currentFile);

                        if (Enum.TryParse(currentType, false, out IMGBFlags.FileExtensions fileExtension) == true)
                        {
                            if (Directory.Exists(extractedIMGBdir))
                            {
                                Console.WriteLine("Detected Image header file");
                                IMGBRepack2.RepackIMGBType2(currentFile, outIMGBfile, extractedIMGBdir, platform);
                            }
                        }

                        currentResourceSize = (uint)new FileInfo(currentFile).Length;

                        using (var currentFileStream = new FileStream(currentFile, FileMode.Open, FileAccess.Read))
                        {
                            currentFileStream.CopyTo(mainDataStream);
                        }

                        File.Delete(currentFile);
                        File.Move(currentFileOG, currentFile);

                        TRBRepackHelpers.UpdateOffset(resInfoWriter, writePos, currentResInfoIndex, currentResourceOffset, currentResourceSize, currentResInfoType);

                        if (trbResourceIDTable[pathIndex + i + 1] != SharedMethods.TRBResourceTypeString)
                        {
                            mainDataStream.PadStream(16);
                        }

                        currentResourceOffset = (uint)mainDataStream.Position;
                        writePos += 16;

                        Console.WriteLine("Repacked " + currentFile);
                        Console.WriteLine("");
                    }
                }

                resInfoWriter.BaseStream.Position = 16;
                var trbSize = (uint)new FileInfo(tmpDataFile).Length + 64 + resourceInfoTableDataSize;
                resInfoWriter.Write(trbSize);
            }

            Console.WriteLine("");
            Console.WriteLine("Assembling final TRB file....");

            using (var finalTRBstream = new FileStream(outTRBfile, FileMode.Append, FileAccess.Write))
            {
                var tmpData = File.ReadAllBytes(tmpDataFile);
                finalTRBstream.Write(tmpData, 0, tmpData.Length);
            }

            SharedMethods.IfFileExistsDel(tmpDataFile);

            Console.WriteLine("Assembled final TRB file");
            Console.WriteLine("");
            Console.WriteLine("");

            Console.WriteLine("Finished repacking files to " + "\"" + Path.GetFileName(outTRBfile) + "\"");
        }
    }
}