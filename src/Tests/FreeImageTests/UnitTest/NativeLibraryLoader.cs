using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FreeImageNETUnitTest
{
    internal class NativeLibraryLoader
    {
        public static void CopyFreeImageNativeDll()
        {
            string solutionFolder = GetSolutionFolder();
            string runtimesFolder = Path.Combine(solutionFolder, @"lib\Native\Aardvark.PixImage.FreeImage");

            const string freeImageLibraryName = "FreeImage";

            string libraryPath = GetPlatformLibraryPath(runtimesFolder);
            string libraryFileExtension = Path.GetExtension(libraryPath);

            if (false == File.Exists(libraryPath))
            {
                throw new FileNotFoundException(libraryPath);
            }

            string executingFolder = GetExecutingFolder();
            string targetLibraryPath = Path.Combine(executingFolder, $"{freeImageLibraryName}{libraryFileExtension}");

            if (File.Exists(targetLibraryPath))
            {
                File.Delete(targetLibraryPath);
            }

            File.Copy(libraryPath, targetLibraryPath, false);
        }

        private static string GetPlatformLibraryPath(string runtimesFolder)
        {
            string runtimeFolderName;
            string libraryFileName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                runtimeFolderName = Path.Combine("windows", "AMD64");
                libraryFileName = "FreeImage.dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                runtimeFolderName = Path.Combine("linux", "AMD64");
                libraryFileName = "libfreeimage-3.18.0.so";
            }
            else
            {
                throw new Exception($"Unsupported platform");
            }

            return Path.Combine(runtimesFolder, runtimeFolderName, libraryFileName);
        }

        private static string GetExecutingFolder()
        {
            return Path.GetDirectoryName(typeof(NativeLibraryLoader).Assembly.Locati‌​on);
        }

        public static string GetSolutionFolder()
        {
            string currentFolder = GetExecutingFolder();

            while (Path.GetFileName(currentFolder) != "src")
            {
                currentFolder = Path.GetFullPath(Path.Combine(currentFolder, ".."));
            }

            return Path.GetFullPath(Path.Combine(currentFolder, ".."));
        }
    }
}
