using System;
using System.IO;
using System.Text;

/// <summary>
/// Provides endianness for BinaryReader methods.
/// </summary>
public static class BinaryReaderHelpers
{
    /// <summary>
    /// Reads the specified number of bytes from the current stream and builds a string. then it advances the
    /// current position of the stream by the number of bytes read.
    /// </summary>
    /// <returns>
    /// A string built from the bytes read from the current stream. encoding of the string will be UTF8.
    /// </returns>
    /// <param name="readCount">The number of bytes to read.</param>
    /// <param name="shouldReverse">Indicates whether the bytes should be reversed.</param>
    public static string ReadBytesString(this BinaryReader reader, int readCount, bool shouldReverse)
    {
        var readValueBuffer = reader.ReadBytes(readCount);
        ReverseBuffer(shouldReverse, readValueBuffer);

        return Encoding.UTF8.GetString(readValueBuffer).Replace("\0", "");
    }

    /// <summary>
    /// Reads bytes until a null byte is encountered and builds a string 
    /// with the bytes read from the current stream. 
    /// then it advances the current position of the stream by the number of bytes read.
    /// </summary>
    /// <returns>
    /// A string built from the bytes read from the current stream. encoding of the string will be 
    /// similar to the encoding used in the BinaryReader.
    /// </returns>
    public static string ReadStringTillNull(this BinaryReader reader)
    {
        var sb = new StringBuilder();
        char chars;
        while ((chars = reader.ReadChar()) != default)
        {
            sb.Append(chars);
        }
        return sb.ToString();
    }

    private static void ReverseBuffer(bool isBigEndian, byte[] readValueBuffer)
    {
        if (isBigEndian)
        {
            Array.Reverse(readValueBuffer);
        }
    }
}