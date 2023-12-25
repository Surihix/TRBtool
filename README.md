# TRBtool
This tool allows you to unpack and repack the TRB IMGB files from the FINAL FANTASY XIII trilogy.

The program should be launched from command prompt with any one of these following argument switches along with the input file:
<br>``-u`` Unpacks the TRB file
<br>``-r`` Repacks the unpacked TRB folder to a TRB file

Commandline usage examples:
<br>``TRBtool.exe -u "c201.win32.trb" ``
<br>``TRBtool.exe -r "_c201.win32.trb" ``

Note: For the ``-r`` switch, the unpacked TRB folder name is specified in the example. the ``_`` in the name indicates the name of the unpacked folder.

### Important
- Repacking is supported only for the PC version TRB IMGB files.
- If you are replacing DDS images, then make sure that the DDS compression/Pixel format used in your image file is the same as what it was in the original file.
- The Xbox 360 version image data is swizzled. due to this swizzled format, this tool will not unpack them correctly.

## For Developers
- This tool makes use of this following reference library:
<br>**IMGBlibrary** - https://github.com/Surihix/IMGBlibrary

- Refer to this [page](https://github.com/LR-Research-Team/Datalog/wiki/TRB) for information about the the TRB's file structure.
