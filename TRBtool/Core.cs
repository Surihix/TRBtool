using System;
using System.IO;
using System.Security.Cryptography;
using TRBtool.Support;

namespace TRBtool
{
    internal class Core
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (args.Length < 2)
            {
                TRBMethods.ErrorExit("Error: Enough arguments not specified\n" +
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

                        if (computedHash != "4fb1654ab6da60cf0ad8b3663c82de38d5d049c691f69daa589f8120be9a4d35")
                        {
                            TRBMethods.ErrorExit("Error: 'IMGBlibrary.dll' file is corrupt. please check if the dll file is valid.");
                        }
                    }
                }
            }
            else
            {
                TRBMethods.ErrorExit("Error: Missing 'IMGBlibrary.dll' file. please ensure that the dll file exists next to the program.");
            }
            #endif


            try
            {
                if (Enum.TryParse(args[0].Replace("-", ""), false, out ToolActions toolAction) == false)
                {
                    TRBMethods.ErrorExit("Error: Proper tool action is not specified\nMust be '-u' for unpacking or '-r' for repacking.");
                }

                switch (toolAction)
                {
                    case ToolActions.u:
                        if (!File.Exists(args[1]))
                        {
                            TRBMethods.ErrorExit("Error: Specified TRB file does not exist.");
                        }
                        TRBUnpack.InitiateUnpack(args[1]);
                        break;

                    case ToolActions.r:
                        if (!Directory.Exists(args[1]))
                        {
                            TRBMethods.ErrorExit("Error: Specified unpacked directory to repack, does not exist.");
                        }
                        TRBRepack.InitiateRepack(args[1]);
                        break;
                }
            }
            catch (Exception ex)
            {
                TRBMethods.ErrorExit("" + ex);
            }
        }

        private enum ToolActions
        {
            u,
            r
        }
    }
}