using IMGBlibrary.Repack;
using IMGBlibrary.Support;
using System;
using System.IO;
using System.Text;
using TRBtool.Support;

namespace TRBtool
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

            var resourceInfoTxtFile = Path.Combine(inExtractedTRBdir, $"{SharedMethods.TRBResourceInfoFileString}.txt");
            var resourceTypeTxtFile = Path.Combine(inExtractedTRBdir, $"{SharedMethods.TRBResourceTypeString}.txt");
            var resourceIdTxtFile = Path.Combine(inExtractedTRBdir, $"{SharedMethods.TRBResourceIDString}.txt");
            var tmpDataFile = Path.Combine(inExtractedTRBdir, "_tempData");

            TRBRepackHelpers.CheckFileExists(resourceInfoTxtFile, $"Error: Missing file '{SharedMethods.TRBResourceInfoFileString}.txt' in the extracted directory.");
            TRBRepackHelpers.CheckFileExists(resourceTypeTxtFile, $"Error: Missing file '{SharedMethods.TRBResourceTypeString}.txt' in the extracted directory.");
            TRBRepackHelpers.CheckFileExists(resourceIdTxtFile, $"Error: Missing file '{SharedMethods.TRBResourceIDString}.txt' in the extracted directory.");

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

            // Get RESOURCE_INFOs
            var trb = TRBRepackHelpers.DeserializeResourceInfo(resourceInfoTxtFile);

            // Get RESOURCE_TYPEs
            var resourceTypelist = TRBRepackHelpers.DeserializeResourceType(resourceTypeTxtFile);

            // Get RESOURCE_IDs
            var resourceIDList = TRBRepackHelpers.DeserializeResourceID(resourceIdTxtFile);

            // Build Resource info
            File.WriteAllBytes(outTRBfile, new byte[64 + (trb.ResourceCount * 16)]);

            // Repack files
            using (var resInfoWriter = new BinaryWriter(File.Open(outTRBfile, FileMode.Open, FileAccess.Write)))
            {
                _ = resInfoWriter.BaseStream.Position = 0;
                resInfoWriter.Write(Encoding.ASCII.GetBytes("SEDBRES "));
                resInfoWriter.Write(trb.Version);

                _ = resInfoWriter.BaseStream.Position += 1;
                resInfoWriter.Write(trb.MainType);
                resInfoWriter.Write((ushort)64);

                _ = resInfoWriter.BaseStream.Position += 32;
                resInfoWriter.Write(trb.ResourceCount);

                _ = resInfoWriter.BaseStream.Position += 4;
                resInfoWriter.Write(trb.ResourceCount);
                resInfoWriter.Write(Encoding.ASCII.GetBytes("brt"));

                using (var mainDataStream = new FileStream(tmpDataFile, FileMode.Append, FileAccess.Write))
                {
                    var pathIndex = (int)trb.ResourceCount;
                    uint currentResourceOffset = 0;
                    uint currentResourceSize = 0;
                    long writePos = 64;

                    for (int i = 0; i < trb.ResourceCount; i++)
                    {
                        var currentIndex = trb.ResourceInfo[i].Item1;
                        var currentID = resourceIDList[pathIndex + i];
                        var currentType = resourceTypelist[i];
                        var currentTypeValue = trb.ResourceInfo[i].Item2;

                        var currentFile = Path.Combine(inExtractedTRBdir, $"{currentID}.{currentType}");

                        if (currentID == SharedMethods.TRBResourceTypeString)
                        {
                            var resourceTypeData = TRBRepackHelpers.BuildResourceType(resourceTypelist);
                            mainDataStream.Write(resourceTypeData, 0, resourceTypeData.Length);

                            uint resourceTypeMemSize = 64 + (trb.ResourceCount * 20);
                            TRBRepackHelpers.UpdateOffset(resInfoWriter, writePos, currentIndex, currentResourceOffset, resourceTypeMemSize, currentTypeValue);

                            TRBRepackHelpers.DoPadding(16, mainDataStream);
                            currentResourceOffset = (uint)mainDataStream.Position;

                            writePos += 16;
                            continue;
                        }

                        if (currentID == SharedMethods.TRBResourceIDString)
                        {
                            var resourceIDData = TRBRepackHelpers.BuildResourceID(resourceIDList, trb.ResourceCount);
                            mainDataStream.Write(resourceIDData, 0, resourceIDData.Length);

                            _ = resInfoWriter.BaseStream.Position = 52;
                            uint allPathsOffset = currentResourceOffset + (trb.ResourceCount * 16);
                            resInfoWriter.Write(allPathsOffset);

                            uint resourceIDMemSize = 64 + (trb.ResourceCount * 32);
                            TRBRepackHelpers.UpdateOffset(resInfoWriter, writePos, currentIndex, currentResourceOffset, resourceIDMemSize, currentTypeValue);
                            continue;
                        }

                        if (!File.Exists(currentFile))
                        {
                            Console.WriteLine($"Missing '{currentID}.{currentType}' file. skipped to next file.");

                            _ = resInfoWriter.BaseStream.Position = writePos;
                            TRBRepackHelpers.UpdateOffset(resInfoWriter, writePos, currentIndex, 0, 0, currentTypeValue);
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

                        TRBRepackHelpers.UpdateOffset(resInfoWriter, writePos, currentIndex, currentResourceOffset, currentResourceSize, currentTypeValue);
                        mainDataStream.PadStream(16);

                        currentResourceOffset = (uint)mainDataStream.Position;
                        writePos += 16;

                        Console.WriteLine("Repacked " + currentFile);
                        Console.WriteLine("");
                    }
                }
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