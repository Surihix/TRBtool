using System;
using System.IO;
using System.Security.Cryptography;
using TRBtool.Repack;
using TRBtool.Support;
using TRBtool.Unpack;

namespace TRBtool
{
    internal class Core
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (args.Length < 2)
            {
                SharedMethods.ErrorExit("Error: Enough arguments not specified\n" +
                    "\nFor Unpacking: TRBtool.exe -u \"TRB file\" " +
                    "\nFor Repacking: TRBtool.exe -r \"unpacked TRB folder\"");
            }


            // Dll check
            #if !DEBUG
            if (File.Exists("IMGBlibrary.dll"))
            {
                using (var dllStream = new FileStream("IMGBlibrary.dll", FileMode.Open, FileAccess.Read))
                {
                    using (var dllHash = SHA256.Create())
                    {
                        var hashArray = dllHash.ComputeHash(dllStream);
                        var computedHash = BitConverter.ToString(hashArray).Replace("-", "").ToLower();

                        if (computedHash != "285847a4f1515bca78d7ab0b7d78c0042acb4c92f3a651046ff9d7bef9101265")
                        {
                            SharedMethods.ErrorExit("Error: 'IMGBlibrary.dll' file is corrupt. please check if the dll file is valid.");
                        }
                    }
                }
            }
            else
            {
                SharedMethods.ErrorExit("Error: Missing 'IMGBlibrary.dll' file. please ensure that the dll file exists next to the program.");
            }
            #endif


            try
            {
                if (Enum.TryParse(args[0].Replace("-", ""), false, out ToolActions toolAction) == false)
                {
                    SharedMethods.ErrorExit("Error: Proper tool action is not specified\nMust be '-u' for unpacking or '-r' for repacking.");
                }

                switch (toolAction)
                {
                    case ToolActions.u:
                        if (!File.Exists(args[1]))
                        {
                            SharedMethods.ErrorExit("Error: Specified TRB file does not exist.");
                        }
                        TRBUnpack.InitiateUnpack(args[1]);
                        break;

                    case ToolActions.r:
                        if (!Directory.Exists(args[1]))
                        {
                            SharedMethods.ErrorExit("Error: Specified unpacked directory to repack, does not exist.");
                        }
                        TRBRepack.InitiateRepack(args[1]);
                        break;
                }
            }
            catch (Exception ex)
            {
                SharedMethods.ErrorExit("" + ex);
            }
        }

        private enum ToolActions
        {
            u,
            r
        }
    }
}