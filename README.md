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
- Please be careful when editing any of the 'RESOURCE' text files. if the data inside them, is not specified properly, then this tool will fail to repack the extracted TRB folder.
- If you are replacing the DDS images with the repack function, then make sure that the DDS compression/Pixel format used in your image file, is supported by the game. refer this [page](https://github.com/LR-Research-Team/Datalog/wiki/TRB#texture-format) for a list of supported formats.
- The Xbox 360 version image data is swizzled. due to this swizzled format, this tool will not unpack them correctly.

## For Developers
- This tool makes use of this following reference library:
<br>**IMGBlibrary** - https://github.com/Surihix/IMGBlibrary

- Refer to this [page](https://github.com/LR-Research-Team/Datalog/wiki/TRB) for information about the the TRB's file structure.
