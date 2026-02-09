using IMGBlibrary.Support;
using System;
using System.IO;

namespace TRBtool.Support
{
    internal class TRBMethods
    {
        public static string TRBOffsetsFile = "SEDBRES_OFFSETS";

        public static string TRBResourceInfoFile = "RESOURCE_INFO";

        public static string TRBResourceTypeFile = "RESOURCE_TYPE";

        public static string TRBResourceIDFile = "RESOURCE_ID";

        public static void ErrorExit(string errorMsg)
        {
            Console.WriteLine(errorMsg);
            Console.ReadLine();
            Environment.Exit(0);
        }

        public static void IfFileExistsDel(string fileToDelete)
        {
            if (File.Exists(fileToDelete))
            {
                File.Delete(fileToDelete);
            }
        }

        public static IMGBFlags.Platforms GetPlatform(string trbFileName)
        {
            var platform = IMGBFlags.Platforms.win32;

            if (trbFileName.EndsWith("ps3.trb"))
            {
                platform = IMGBFlags.Platforms.ps3;
            }
            else if (trbFileName.EndsWith("x360.trb"))
            {
                platform = IMGBFlags.Platforms.x360;
            }

            return platform;
        }
    }
}