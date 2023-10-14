﻿using System;
using System.IO;

namespace TRBtool
{
    internal class Core
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                CmnMethods.ErrorExit("Error: Enough arguments not specified\nMust be: TRBtool.exe '-u' or '-r' and 'TRB file or unpacked TRB folder'.");
            }

            var toolAction = args[0].Replace("-", "");
            var inTRBfileOrDir = args[1];


            try
            {
                var convertedToolAction = new ActionSwitches();
                if (Enum.TryParse(toolAction, false, out ActionSwitches convertedActionSwitch))
                {
                    convertedToolAction = convertedActionSwitch;
                }
                else
                {
                    CmnMethods.ErrorExit("Error: Proper tool action is not specified\nMust be '-u' for unpacking or '-r' for repacking.");
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

        public enum ActionSwitches
        {
            u,
            r
        }
    }
}