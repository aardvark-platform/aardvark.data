![](https://img.shields.io/nuget/dt/FreeImage.Standard.svg)

This is a clone of the FreeImage .NET project (http://freeimage.sourceforge.net/) modified to work with .NET Standard 2.0.

FreeImage native binaries are included in the nuget package for Windows x86/x64, Linux x64/armhf, and OSX x64 (^10.10).

For other platforms they will have to be installed separately. Note that the native function calls require the library filename to be "FreeImage," so symlinking may be required (eg. `sudo ln -s /usr/lib/x86_64-linux-gnu/libfreeimage.so /usr/lib/FreeImage`).

nuget package: https://www.nuget.org/packages/FreeImage.Standard

`Install-Package FreeImage.Standard`

#### FreeImage Version

This is for FreeImage version 3.17.0 

##### The version number of this package no longer matches the FreeImage native library version!

#### License

The license is the same as the FreeImage license available at the link above
