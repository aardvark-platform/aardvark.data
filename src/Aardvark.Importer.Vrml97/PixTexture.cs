using Aardvark.Base;
using System;

namespace Aardvark.Importer.Vrml97
{
    public enum TexRepeatMode
    {
        Clamped,
        Cyclic,
        Mirrored,
    }

    public struct TexInfo2d
    {
        public (TexRepeatMode, TexRepeatMode) RepeatMode;
        public bool DoMipMap;

        public TexInfo2d(TexRepeatMode repeatModeU, TexRepeatMode repeatModeV, bool doMipMap)
        {
            RepeatMode = (repeatModeU, repeatModeV);
            DoMipMap = doMipMap;
        }
        public TexInfo2d((TexRepeatMode, TexRepeatMode) repeatMode, bool doMipMap)
        {
            RepeatMode = repeatMode;
            DoMipMap = doMipMap;
        }

        public static readonly TexInfo2d CyclicMipMap =
                new TexInfo2d(TexRepeatMode.Cyclic, TexRepeatMode.Cyclic, true);

        public static readonly TexInfo2d ClampedMipMap =
                new TexInfo2d(TexRepeatMode.Clamped, TexRepeatMode.Clamped, true);

        public static readonly TexInfo2d MirroredMipMap =
                new TexInfo2d(TexRepeatMode.Mirrored, TexRepeatMode.Mirrored, true);

        public static readonly TexInfo2d CyclicNoMipMap =
                new TexInfo2d(TexRepeatMode.Cyclic, TexRepeatMode.Cyclic, false);

        public static readonly TexInfo2d ClampedNoMipMap =
                new TexInfo2d(TexRepeatMode.Clamped, TexRepeatMode.Clamped, false);

        public static readonly TexInfo2d MirroredNoMipMap =
                new TexInfo2d(TexRepeatMode.Mirrored, TexRepeatMode.Mirrored, false);

        public static TexInfo2d Create(TexRepeatMode mode, bool doMipMap = true)
        {
            return new TexInfo2d(mode, mode, doMipMap);
        }
    }

    public struct TexInfo3d
    {
        public (TexRepeatMode, TexRepeatMode, TexRepeatMode) RepeatMode;
        public bool DoMipMap;

        public TexInfo3d(TexRepeatMode repeatModeU, TexRepeatMode repeatModeV, TexRepeatMode repeatModeW, bool doMipMap)
        {
            RepeatMode = (repeatModeU, repeatModeV, repeatModeW);
            DoMipMap = doMipMap;
        }
        public TexInfo3d((TexRepeatMode, TexRepeatMode, TexRepeatMode) repeatMode, bool doMipMap)
        {
            RepeatMode = repeatMode;
            DoMipMap = doMipMap;
        }
    }

    //[Flags]
    //public enum CubeSide
    //{
    //    PositiveX = 0,
    //    NegativeX = 1,
    //    PositiveY = 2,
    //    NegativeY = 3,
    //    PositiveZ = 4,
    //    NegativeZ = 5,
    //}

    public interface IPixTexture
    {
    }

    public class Texture2d : IPixTexture
    {
        public readonly Func<IPixMipMap2d> PixMipMapFun;
        public readonly TexInfo2d TexInfo;

        #region Constructor

        public Texture2d(Func<IPixMipMap2d> pixMipMapFun, TexInfo2d texInfo)
        {
            PixMipMapFun = pixMipMapFun;
            TexInfo = texInfo;
        }

        #endregion
    }

    public class FileTexture2d : Texture2d
    {
        public readonly string Path;

        #region Constructor

        public FileTexture2d(string path, TexInfo2d texInfo)
            : base(() => LoadFun(path), texInfo)
        {
            Path = path;
        }

        private static volatile Func<string, IPixMipMap2d>[] s_loadFunArray = null;
        private static object s_loadFunArrayLock = new object();

        public static void RegisterLoadFun(Func<string, IPixMipMap2d> loadFun)
        {
            lock (s_loadFunArrayLock)
            {
                s_loadFunArray = s_loadFunArray.WithAdded(loadFun);
            }
        }

        public static IPixMipMap2d LoadFun(string s)
        {
            var loadFunArray = s_loadFunArray;
            int funIndex = loadFunArray.Length;
            while (--funIndex >= 0)
            {
                var result = loadFunArray[funIndex](s);
                if (result != null) return result;
            }
            throw new ArgumentException(String.Format("could not load image \"{0}\"", s));
        }

        #endregion

    }


    public class Texture3d : IPixTexture
    {
        public readonly Func<IPixImage3d> PixMipMapFun;
        public readonly TexInfo3d TexInfo;

        #region Constructor

        public Texture3d(Func<IPixImage3d> pixMipMapFun, TexInfo3d texInfo)
        {
            PixMipMapFun = pixMipMapFun;
            TexInfo = texInfo;
        }

        #endregion
    }


    public class TextureCube2d : IPixTexture
    {
        public Func<IPixCube> PixCubeFun;
        public TexInfo2d TexInfo;

        #region Constructor

        public TextureCube2d(Func<IPixCube> pixCubeFun, TexInfo2d texInfo)
        {
            PixCubeFun = pixCubeFun;
            TexInfo = texInfo;
        }

        #endregion
    }

}
