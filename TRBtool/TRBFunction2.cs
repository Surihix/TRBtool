using IMGBlibrary.Repack;
using IMGBlibrary.Support;
using System;
using System.IO;

namespace TRBtool
{
    internal partial class TRB
    {
        public static void RepackTRB(string inExtractedTRBdir)
        {
            var outTRBfileName = Path.GetFileName(inExtractedTRBdir);

            if (outTRBfileName.StartsWith("_"))
            {
                outTRBfileName = Path.GetFileName(inExtractedTRBdir).Remove(0, 1);
            }

            var outTRBfileDir = Path.GetDirectoryName(inExtractedTRBdir);
            var outTRBfile = Path.Combine(outTRBfileDir, outTRBfileName);

            var trbOffsetsFile = Path.Combine(inExtractedTRBdir, SharedMethods.TRBOffsetsFile);
            var trbOffsetsFileTmp = Path.Combine(inExtractedTRBdir, SharedMethods.TRBOffsetsFile + ".tmp");

            var resourceTypeFile = Path.Combine(inExtractedTRBdir, "RESOURCE_TYPE");
            var resourceIdFile = Path.Combine(inExtractedTRBdir, "RESOURCE_ID");
            var tmpDataFile = Path.Combine(inExtractedTRBdir, "_tempData");

            CheckFileExists(trbOffsetsFile, $"Error: Missing file '{SharedMethods.TRBOffsetsFile}' in the extracted directory.");
            CheckFileExists(resourceTypeFile, "Error: Missing file 'RESOURCE_TYPE' in the extracted directory.");
            CheckFileExists(resourceIdFile, "Error: Missing file 'RESOURCE_ID' in the extracted directory.");

            IfFileExistsDel(trbOffsetsFileTmp);
            IfFileExistsDel(tmpDataFile);

            var outIMGBfileName = Path.GetFileNameWithoutExtension(outTRBfileName) + ".imgb";
            var outIMGBfile = Path.Combine(outTRBfileDir, outIMGBfileName);
            var extractedIMGBdir = Path.Combine(outTRBfileDir, "_" + outIMGBfileName);

            var oldTRBfile = Path.Combine(outTRBfileDir, Path.GetFileName(outTRBfile) + ".old");
            var oldIMGBfile = Path.Combine(outTRBfileDir, Path.GetFileName(outIMGBfile) + ".old");

            IfFileExistsDel(oldTRBfile);
            IfFileExistsDel(oldIMGBfile);

            if (File.Exists(outTRBfile))
            {
                File.Move(outTRBfile, oldTRBfile);
            }
            if (File.Exists(outIMGBfile))
            {
                File.Move(outIMGBfile, oldIMGBfile);
            }

            var platform = IMGBEnums.Platforms.win32;

            if (outTRBfileName.EndsWith("ps3.trb"))
            {
                platform = IMGBEnums.Platforms.ps3;
            }
            else if (outTRBfileName.EndsWith("x360.trb"))
            {
                platform = IMGBEnums.Platforms.x360;
            }


            Console.WriteLine("");

            uint resourceCount = 0;
            uint resourceDataStart = 0;

            using (var trbOffsetsReader = new BinaryReader(File.Open(trbOffsetsFile, FileMode.Open, FileAccess.Read)))
            {
                trbOffsetsReader.BaseStream.Position = 56;
                resourceCount = trbOffsetsReader.ReadUInt32();
                resourceDataStart = 64 + (resourceCount * 16);
            }

            File.Copy(trbOffsetsFile, trbOffsetsFileTmp);

            using (var trbOffsetsWriter = new BinaryWriter(File.Open(trbOffsetsFileTmp, FileMode.Open, FileAccess.Write)))
            {

                using (var resTypeReader = new BinaryReader(File.Open(resourceTypeFile, FileMode.Open, FileAccess.Read)))
                {
                    using (var resIdsReader = new BinaryReader(File.Open(resourceIdFile, FileMode.Open, FileAccess.Read)))
                    {

                        using (var tmpDataStream = new FileStream(tmpDataFile, FileMode.Append, FileAccess.Write))
                        {

                            uint resOffsetWritePos = 68;
                            uint resourceIdsPathsReadPos = resourceCount * 16;
                            uint resourceTypeReadPos = 0;

                            var currentResourceIdPath = string.Empty;
                            var currentResourceType = string.Empty;
                            uint currentResourceSize = 0;
                            uint currentResourceStart = 0;

                            for (int i = 1; i < resourceCount - 1; i++)
                            {
                                resIdsReader.BaseStream.Position = resourceIdsPathsReadPos;
                                currentResourceIdPath = resIdsReader.ReadStringTillNull();
                                resourceIdsPathsReadPos = (uint)resIdsReader.BaseStream.Position;

                                resTypeReader.BaseStream.Position = resourceTypeReadPos;
                                currentResourceType = resTypeReader.ReadBytesString(4, true);

                                var currentFile = Path.Combine(inExtractedTRBdir, currentResourceIdPath + "." + currentResourceType);

                                if (File.Exists(currentFile))
                                {
                                    var currentFileTmp = currentFile + ".tmp";

                                    IfFileExistsDel(currentFileTmp);
                                    File.Copy(currentFile, currentFileTmp);

                                    if (Enum.TryParse(currentResourceType, false, out IMGBEnums.FileExtensions fileExtension) == true)
                                    {
                                        if (Directory.Exists(extractedIMGBdir))
                                        {
                                            Console.WriteLine("Detected Image header file");
                                            IMGBRepack2.RepackIMGBType2(currentFileTmp, Path.GetFileName(currentFile), outIMGBfile, extractedIMGBdir, platform, true);
                                        }
                                    }

                                    currentResourceSize = (uint)new FileInfo(currentFileTmp).Length;

                                    using (var fileToPack = new FileStream(currentFileTmp, FileMode.Open, FileAccess.Read))
                                    {
                                        currentResourceStart = (uint)tmpDataStream.Length;

                                        fileToPack.Position = 0;
                                        fileToPack.CopyStreamTo(tmpDataStream, currentResourceSize, false);

                                        // Pad null bytes to make the next
                                        // start position divisible by a 
                                        // pad value
                                        var currentPos = tmpDataStream.Length;
                                        var padValue = 4;
                                        if (currentPos % padValue != 0)
                                        {
                                            var remainder = currentPos % padValue;
                                            var increaseBytes = padValue - remainder;
                                            var newPos = currentPos + increaseBytes;
                                            var nullBytesAmount = newPos - currentPos;

                                            tmpDataStream.Seek(currentPos, SeekOrigin.Begin);
                                            tmpDataStream.PadNull((int)nullBytesAmount);
                                        }
                                    }

                                    File.Delete(currentFileTmp);

                                    trbOffsetsWriter.BaseStream.Position = resOffsetWritePos;
                                    trbOffsetsWriter.WriteBytesUInt32(currentResourceStart, false);

                                    trbOffsetsWriter.BaseStream.Position = resOffsetWritePos + 4;
                                    trbOffsetsWriter.WriteBytesUInt32(currentResourceSize, false);

                                    Console.WriteLine("Repacked " + currentFile);
                                    Console.WriteLine("");
                                }
                                else
                                {
                                    Console.WriteLine("Missing " + currentResourceIdPath + "." + currentResourceType + " file. skipping to next file.");

                                    trbOffsetsWriter.BaseStream.Position = resOffsetWritePos;
                                    trbOffsetsWriter.WriteBytesUInt32(0, false);

                                    trbOffsetsWriter.BaseStream.Position = resOffsetWritePos + 4;
                                    trbOffsetsWriter.WriteBytesUInt32(0, false);
                                }

                                resOffsetWritePos += 16;
                                resourceTypeReadPos += 4;
                            }
                        }
                    }
                }


                // Update 'RESOURCE_TYPE',
                // 'RESOURCE_ID' and SEDB
                // header offsets
                var offsetWriterPos = (uint)trbOffsetsWriter.BaseStream.Position + 8;
                var packedDataSize = (uint)new FileInfo(tmpDataFile).Length;

                var resTypeStartPos = packedDataSize;
                var resTypeSize = (uint)new FileInfo(resourceTypeFile).Length;

                var resIdsStartPos = resTypeStartPos + resTypeSize;
                var resIdsSize = (uint)new FileInfo(resourceIdFile).Length;


                // Use a formulae to 
                // compute the sizes
                var resTypeMemSize = 64 + (20 * resourceCount);
                var resIdsMemSize = 64 + (32 * resourceCount);

                trbOffsetsWriter.BaseStream.Position = offsetWriterPos;
                trbOffsetsWriter.WriteBytesUInt32(resTypeStartPos, false);

                trbOffsetsWriter.BaseStream.Position = offsetWriterPos + 4;
                trbOffsetsWriter.WriteBytesUInt32(resTypeMemSize, false);

                offsetWriterPos += 16;

                trbOffsetsWriter.BaseStream.Position = offsetWriterPos;
                trbOffsetsWriter.WriteBytesUInt32(resIdsStartPos, false);

                trbOffsetsWriter.BaseStream.Position = offsetWriterPos + 4;
                trbOffsetsWriter.WriteBytesUInt32(resIdsMemSize, false);

                // Write header related
                // offsets
                var resIdsPathsStart = resIdsStartPos + (resourceCount * 16);
                trbOffsetsWriter.BaseStream.Position = 52;
                trbOffsetsWriter.WriteBytesUInt32(resIdsPathsStart, false);

                var totalTRBsize = resourceDataStart + packedDataSize + resTypeSize + resIdsSize;
                trbOffsetsWriter.BaseStream.Position = 16;
                trbOffsetsWriter.WriteBytesUInt32(totalTRBsize, false);
            }

            Console.WriteLine("");
            Console.WriteLine("Assembling final TRB file....");


            using (var finalTRBstream = new FileStream(outTRBfile, FileMode.Append, FileAccess.Write))
            {
                using (var updTRBoffsetsStream = new FileStream(trbOffsetsFileTmp, FileMode.Open, FileAccess.Read))
                {
                    updTRBoffsetsStream.Seek(0, SeekOrigin.Begin);
                    updTRBoffsetsStream.CopyTo(finalTRBstream);
                }
                File.Delete(trbOffsetsFileTmp);

                using (var trbDataStream = new FileStream(tmpDataFile, FileMode.Open, FileAccess.Read))
                {
                    trbDataStream.Seek(0, SeekOrigin.Begin);
                    trbDataStream.CopyTo(finalTRBstream);
                }
                File.Delete(tmpDataFile);

                using (var trbResTypeStream = new FileStream(resourceTypeFile, FileMode.Open, FileAccess.Read))
                {
                    trbResTypeStream.Seek(0, SeekOrigin.Begin);
                    trbResTypeStream.CopyTo(finalTRBstream);
                }

                using (var trbResIdsStream = new FileStream(resourceIdFile, FileMode.Open, FileAccess.Read))
                {
                    trbResIdsStream.Seek(0, SeekOrigin.Begin);
                    trbResIdsStream.CopyTo(finalTRBstream);
                }
            }

            Console.WriteLine("Assembled final TRB file");
            Console.WriteLine("");
            Console.WriteLine("");

            Console.WriteLine("Finished repacking files to " + "\"" + Path.GetFileName(outTRBfile) + "\"");
        }


        private static void CheckFileExists(string fileToCheck, string errorMsg)
        {
            if (!File.Exists(fileToCheck))
            {
                SharedMethods.ErrorExit(errorMsg);
            }
        }


        private static void IfFileExistsDel(string fileToDelete)
        {
            if (File.Exists(fileToDelete))
            {
                File.Delete(fileToDelete);
            }
        }
    }
}