using System;

namespace TRBtool
{
    internal class SharedMethods
    {
        public static void ErrorExit(string errorMsg)
        {
            Console.WriteLine(errorMsg);
            Console.ReadLine();
            Environment.Exit(0);
        }

        public static readonly string TRBOffsetsFile = "SEDBRES_OFFSETS";
    }
}