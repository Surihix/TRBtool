using System;

namespace TRBtool
{
    internal static class CmnMethods
    {
        public static void ErrorExit(string errorMsg)
        {
            Console.WriteLine(errorMsg);
            Console.ReadLine();
            Environment.Exit(0);
        }

        public static string TRBOffsetsFile = "SEDBRES_OFFSETS";
    }
}