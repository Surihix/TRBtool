using BinaryReaderEx;
using BinaryWriterEx;
using IMGB;
using StreamExtension;
using System;
using System.IO;
using System.Linq;
using System.Text;
using TRBtool.SupportClasses;

namespace TRBtool
{
    internal partial class TRB
    {
        public static void RepackTRB(string inExtractedTRBdir)
        {
            var outTRBfileName = Path.GetFileName(inExtractedTRBdir).Remove(0, 1);
            var outTRBfileDir = Path.GetDirectoryName(inExtractedTRBdir);
            var outTRBfile = Path.Combine(outTRBfileDir, outTRBfileName);

            var trbOffsetsFile = Path.Combine(inExtractedTRBdir, CmnMethods.TRBOffsetsFile);
            var resourceTypeFile = Path.Combine(inExtractedTRBdir, "RESOURCE_TYPE");
            var resourceIdFile = Path.Combine(inExtractedTRBdir, "RESOURCE_ID");
            var tmpDataFile = Path.Combine(inExtractedTRBdir, "_tempData");

            CheckFileExists(trbOffsetsFile, $"Error: Missing file + '{CmnMethods.TRBOffsetsFile}' in the extracted directory.");
            CheckFileExists(resourceTypeFile, "Error: Missing file 'RESOURCE_TYPE' in the extracted directory.");
            CheckFileExists(resourceIdFile, "Error: Missing file 'RESOURCE_ID' in the extracted directory.");

            if (File.Exists(tmpDataFile))
            {
                File.Delete(tmpDataFile);
            }

            var outIMGBfileName = Path.GetFileNameWithoutExtension(outTRBfileName) + ".imgb";
            var outIMGBfile = Path.Combine(outTRBfileDir, outIMGBfileName);
            var extractedIMGBdir = Path.Combine(outTRBfileDir, "_" + outIMGBfileName);

            var oldTRBfile = Path.Combine(outTRBfileDir, Path.GetFileName(outTRBfile) + ".old");
            var oldIMGBfile = Path.Combine(outTRBfileDir, Path.GetFileName(outIMGBfile) + ".old");

            File.Move(outTRBfile, oldTRBfile);

            if (File.Exists(outIMGBfile))
            {
                File.Move(outIMGBfile, oldIMGBfile);
            }


            Console.WriteLine("");

            using (var trbOffsets = new FileStream(trbOffsetsFile, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var trbOffsetsReader = new BinaryReader(trbOffsets))
                {
                    using (var trbOffsetsWriter = new BinaryWriter(trbOffsets))
                    {
                        trbOffsetsReader.BaseStream.Position = 56;
                        var resourceCount = trbOffsetsReader.ReadUInt32();
                        var resourceDataStart = 64 + (resourceCount * 16);

                        using (var resTypeStream = new FileStream(resourceTypeFile, FileMode.Open, FileAccess.Read))
                        {
                            using (var resTypeReader = new BinaryReader(resTypeStream))
                            {

                                using (var resIds = new FileStream(resourceIdFile, FileMode.Open, FileAccess.Read))
                                {
                                    using (var resIdsReader = new BinaryReader(resIds))
                                    {

                                        using (var tmpDataStream = new FileStream(tmpDataFile, FileMode.OpenOrCreate, FileAccess.Write))
                                        {

                                            uint resOffsetWritePos = 68;
                                            uint resIdsPathsReadPos = resourceCount * 16;
                                            uint nextResIdPathStart = 0;
                                            uint resTypeReadPos = 0;
                                            var trbRresourceIndex = 1;

                                            var repackIndex = resourceCount - 2;
                                            var currentResIdPath = "";
                                            var currentResType = "";
                                            uint currentResStart = 0;
                                            uint currentResSize = 0;

                                            while (trbRresourceIndex <= repackIndex)
                                            {
                                                resIdsReader.BaseStream.Position = resIdsPathsReadPos;
                                                currentResIdPath = resIdsReader.ReadStringTillNull();
                                                nextResIdPathStart = (uint)resIdsReader.BaseStream.Position;

                                                resTypeReader.BaseStream.Position = resTypeReadPos;
                                                var resTypeArray = resTypeReader.ReadBytes(4);
                                                Array.Reverse(resTypeArray);
                                                currentResType = "." + Encoding.ASCII.GetString(resTypeArray).Replace("\0", "");

                                                var currentFile = Path.Combine(inExtractedTRBdir, currentResIdPath + currentResType);

                                                if (File.Exists(currentFile))
                                                {
                                                    // IMGB stuff
                                                    if (ImageMethods.ImgHeaderBlockFileExtensions.Contains(currentResType))
                                                    {
                                                        if (Directory.Exists(extractedIMGBdir))
                                                        {
                                                            Console.WriteLine("Detected Image header file");
                                                            ImageMethods.RepackIMGBType2(currentFile, outIMGBfile, extractedIMGBdir);
                                                        }
                                                    }

                                                    currentResSize = (uint)new FileInfo(currentFile).Length;

                                                    using (var fileToPack = new FileStream(currentFile, FileMode.Open, FileAccess.Read))
                                                    {
                                                        currentResStart = (uint)tmpDataStream.Length;
                                                        fileToPack.ExCopyTo(tmpDataStream, 0, currentResSize);

                                                        // Pad null bytes to make the next
                                                        // start position divisible by 4
                                                        var currentPos = tmpDataStream.Length;
                                                        var padValueToCheck = 4;
                                                        if (currentPos % padValueToCheck != 0)
                                                        {
                                                            var remainder = currentPos % padValueToCheck;
                                                            var increaseBytes = padValueToCheck - remainder;
                                                            var newPos = currentPos + increaseBytes;
                                                            var nullBytesAmount = newPos - currentPos;

                                                            tmpDataStream.Seek(currentPos, SeekOrigin.Begin);
                                                            for (int p = 0; p < nullBytesAmount; p++)
                                                            {
                                                                tmpDataStream.WriteByte(0);
                                                            }
                                                        }
                                                    }

                                                    trbOffsetsWriter.BaseStream.Position = resOffsetWritePos;
                                                    trbOffsetsWriter.WriteBytesUInt32(currentResStart, false);

                                                    trbOffsetsWriter.BaseStream.Position = resOffsetWritePos + 4;
                                                    trbOffsetsWriter.WriteBytesUInt32(currentResSize, false);

                                                    Console.WriteLine("Repacked " + currentFile);
                                                    Console.WriteLine("");
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Missing " + currentResIdPath + currentResType + " file. skipping to next file.");

                                                    trbOffsetsWriter.BaseStream.Position = resOffsetWritePos;
                                                    trbOffsetsWriter.WriteBytesUInt32(0, false);

                                                    trbOffsetsWriter.BaseStream.Position = resOffsetWritePos + 4;
                                                    trbOffsetsWriter.WriteBytesUInt32(0, false);
                                                }

                                                resOffsetWritePos += 16;
                                                resIdsPathsReadPos = nextResIdPathStart;
                                                resTypeReadPos += 4;
                                                trbRresourceIndex++;
                                            }
                                        }
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
                }
            }

            Console.WriteLine("");
            Console.WriteLine("Assembling final TRB file....");


            using (var finalTRBstream = new FileStream(outTRBfile, FileMode.Append, FileAccess.Write))
            {
                using (var updTRBoffsetsStream = new FileStream(trbOffsetsFile, FileMode.Open, FileAccess.Read))
                {
                    updTRBoffsetsStream.Seek(0, SeekOrigin.Begin);
                    updTRBoffsetsStream.CopyTo(finalTRBstream);
                }

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

            Console.WriteLine("Finished Repacking " + Path.GetFileName(outTRBfile));
            Console.ReadLine();
        }


        static void CheckFileExists(string fileToCheck, string errorMsg)
        {
            if (!File.Exists(fileToCheck))
            {
                CmnMethods.ErrorExit(errorMsg);
            }
        }
    }
}