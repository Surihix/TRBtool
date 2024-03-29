﻿using System;
using System.IO;
using System.Security.Cryptography;

namespace TRBtool
{
    internal class Core
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                CmnMethods.ErrorExit("Error: Enough arguments not specified\n" +
                    "\nFor Unpacking: TRBtool.exe -u \"TRB file\" " +
                    "\nFor Repacking: TRBtool.exe -r \"unpacked TRB folder\"");
            }

            var toolAction = args[0].Replace("-", "");
            var inTRBfileOrDir = args[1];


            // Dll check
            if (File.Exists("IMGBlibrary.dll"))
            {
                using (var dllStream = new FileStream("IMGBlibrary.dll", FileMode.Open, FileAccess.Read))
                {
                    using (var dllHash = SHA256.Create())
                    {
                        var hashArray = dllHash.ComputeHash(dllStream);
                        var computedHash = BitConverter.ToString(hashArray).Replace("-", "").ToLower();

                        if (!computedHash.Equals("7201f9319a94a3d8cb618e1a8379af1324e0b9433f6a286cb590718e376ef55e"))
                        {
                            CmnMethods.ErrorExit("Error: 'IMGBlibrary.dll' file is corrupt. please check if the dll file is valid.");
                        }
                    }
                }
            }
            else
            {
                CmnMethods.ErrorExit("Error: Missing 'IMGBlibrary.dll' file. please ensure that the dll file exists next to the program.");
            }


            try
            {
                var convertedToolAction = new ActionSwitches();
                if (Enum.TryParse(toolAction, false, out ActionSwitches convertedActionSwitch))
                {
                    convertedToolAction = convertedActionSwitch;
                }
                else
                {
                    CmnMethods.ErrorExit("Error: Proper tool action is not specified\nMust be -u for unpacking or -r for repacking.");
                }

                switch (convertedToolAction)
                {
                    case ActionSwitches.u:
                        if (!File.Exists(inTRBfileOrDir))
                        {
                            CmnMethods.ErrorExit("Error: Specified TRB file does not exist.");
                        }
                        TRB.UnpackTRB(inTRBfileOrDir);
                        break;

                    case ActionSwitches.r:
                        if (!Directory.Exists(inTRBfileOrDir))
                        {
                            CmnMethods.ErrorExit("Error: Specified unpacked directory to repack, does not exist.");
                        }
                        TRB.RepackTRB(inTRBfileOrDir);
                        break;
                }
            }
            catch (Exception ex)
            {
                CmnMethods.ErrorExit("" + ex);
            }
        }

        enum ActionSwitches
        {
            u,
            r
        }
    }
}