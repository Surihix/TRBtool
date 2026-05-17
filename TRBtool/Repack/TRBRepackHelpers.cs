using System;
using System.IO;
using System.Text;

namespace TRBtool.Repack
{
    internal class TRBRepackHelpers
    {
        public static void UpdateOffset(BinaryWriter resInfoWriter, long writePos, uint currentIndex, uint currentResourceOffset, uint currentResourceSize, uint currentTypeValue)
        {
            _ = resInfoWriter.BaseStream.Position = writePos;
            resInfoWriter.Write(currentIndex);
            resInfoWriter.Write(currentResourceOffset);
            resInfoWriter.Write(currentResourceSize);
            resInfoWriter.Write(currentTypeValue);
        }

        public static byte[] BuildResourceIDsSection(string[] resourceIDTable, uint resourceCount)
        {
            var resIDsData = Array.Empty<byte>();

            using (var resIdsStream = new MemoryStream())
            {
                using (var resIdsWriter = new BinaryWriter(resIdsStream, Encoding.ASCII, true))
                {
                    var isFullPath = false;

                    for (int i = 0; i < resourceIDTable.Length; i++)
                    {
                        if (!isFullPath && i == resourceCount)
                        {
                            isFullPath = true;
                        }

                        var id = resourceIDTable[i];
                        var currentIDData = Encoding.ASCII.GetBytes(id);
                        var currentIDLength = id.Length;

                        if (isFullPath)
                        {
                            resIdsWriter.Write(currentIDData);
                            resIdsWriter.Write(byte.MinValue);
                            continue;
                        }

                        if (id == "|-null-|")
                        {
                            resIdsWriter.Write(new byte[16]);
                            continue;
                        }

                        resIdsWriter.Write(currentIDData);

                        uint padSize = 0;

                        if (DeterminePadding(16, (uint)currentIDLength, ref padSize))
                        {
                            resIdsWriter.Write(new byte[padSize]);
                        }
                    }
                }

                resIdsStream.Position = 0;
                resIDsData = resIdsStream.ToArray();
            }

            return resIDsData;
        }

        public static byte[] BuildResourceTypesSection(string[] resourceTypes)
        {
            var resTypesData = Array.Empty<byte>();

            using (var resTypesStream = new MemoryStream())
            {
                using (var resTypesWriter = new BinaryWriter(resTypesStream, Encoding.ASCII, true))
                {
                    for (int i = 0; i < resourceTypes.Length; i++)
                    {
                        var type = resourceTypes[i];

                        if (string.IsNullOrEmpty(type))
                        {
                            resTypesWriter.Write(BitConverter.GetBytes(uint.MaxValue));
                            continue;
                        }

                        var currentTypeData = Encoding.ASCII.GetBytes(type);
                        var currentTypeLength = type.Length;
                        Array.Reverse(currentTypeData);

                        resTypesWriter.Write(currentTypeData);

                        if (currentTypeLength < 4)
                        {
                            resTypesWriter.Write(new byte[4 - currentTypeLength]);
                        }
                    }
                }

                resTypesStream.Position = 0;
                resTypesData = resTypesStream.ToArray();
            }

            return resTypesData;
        }

        private static bool DeterminePadding(int padValue, uint size, ref uint padSize)
        {
            var remainder = size % padValue;

            if (remainder != 0)
            {
                padSize = (uint)(padValue - remainder);
                return true;
            }

            return false;
        }
    }
}