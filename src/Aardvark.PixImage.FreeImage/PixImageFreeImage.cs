using System;
using System.IO;
using System.Collections.Generic;
using FreeImageAPI;

namespace Aardvark.Base
{
    public static class PixImageFreeImage
    {
        [OnAardvarkInit]
        public static void Init()
        {
            PixImage.AddLoader(Loader);
        }

        #region Static Tables and Methods

        private static readonly Dictionary<PixFileFormat, FREE_IMAGE_FORMAT> s_fileFormats =
            new Dictionary<PixFileFormat, FREE_IMAGE_FORMAT>
        {
            { PixFileFormat.Unknown,    FREE_IMAGE_FORMAT.FIF_UNKNOWN   },
            { PixFileFormat.Bmp,        FREE_IMAGE_FORMAT.FIF_BMP       },
            { PixFileFormat.Ico,        FREE_IMAGE_FORMAT.FIF_ICO       },
            { PixFileFormat.Jpeg,       FREE_IMAGE_FORMAT.FIF_JPEG      },
            { PixFileFormat.Jng,        FREE_IMAGE_FORMAT.FIF_JNG       },
            { PixFileFormat.Koala,      FREE_IMAGE_FORMAT.FIF_KOALA     },
            { PixFileFormat.Lbm,        FREE_IMAGE_FORMAT.FIF_LBM       },
            { PixFileFormat.Iff,        FREE_IMAGE_FORMAT.FIF_IFF       },
            { PixFileFormat.Mng,        FREE_IMAGE_FORMAT.FIF_MNG       },
            { PixFileFormat.Pbm,        FREE_IMAGE_FORMAT.FIF_PBM       },
            { PixFileFormat.PbmRaw,     FREE_IMAGE_FORMAT.FIF_PBMRAW    },
            { PixFileFormat.Pcd,        FREE_IMAGE_FORMAT.FIF_PCD       },
            { PixFileFormat.Pcx,        FREE_IMAGE_FORMAT.FIF_PCX       },
            { PixFileFormat.Pgm,        FREE_IMAGE_FORMAT.FIF_PGM       },
            { PixFileFormat.PgmRaw,     FREE_IMAGE_FORMAT.FIF_PGMRAW    },
            { PixFileFormat.Png,        FREE_IMAGE_FORMAT.FIF_PNG       },
            { PixFileFormat.Ppm,        FREE_IMAGE_FORMAT.FIF_PPM       },
            { PixFileFormat.PpmRaw,     FREE_IMAGE_FORMAT.FIF_PPMRAW    },
            { PixFileFormat.Ras,        FREE_IMAGE_FORMAT.FIF_RAS       },
            { PixFileFormat.Targa,      FREE_IMAGE_FORMAT.FIF_TARGA     },
            { PixFileFormat.Tiff,       FREE_IMAGE_FORMAT.FIF_TIFF      },
            { PixFileFormat.Wbmp,       FREE_IMAGE_FORMAT.FIF_WBMP      },
            { PixFileFormat.Psd,        FREE_IMAGE_FORMAT.FIF_PSD       },
            { PixFileFormat.Cut,        FREE_IMAGE_FORMAT.FIF_CUT       },
            { PixFileFormat.Xbm,        FREE_IMAGE_FORMAT.FIF_XBM       },
            { PixFileFormat.Xpm,        FREE_IMAGE_FORMAT.FIF_XPM       },
            { PixFileFormat.Dds,        FREE_IMAGE_FORMAT.FIF_DDS       },
            { PixFileFormat.Gif,        FREE_IMAGE_FORMAT.FIF_GIF       },
            { PixFileFormat.Hdr,        FREE_IMAGE_FORMAT.FIF_HDR       },
            { PixFileFormat.Faxg3,      FREE_IMAGE_FORMAT.FIF_FAXG3     },
            { PixFileFormat.Sgi,        FREE_IMAGE_FORMAT.FIF_SGI       },
            { PixFileFormat.Exr,        FREE_IMAGE_FORMAT.FIF_EXR       },
            { PixFileFormat.J2k,        FREE_IMAGE_FORMAT.FIF_J2K       },
            { PixFileFormat.Jp2,        FREE_IMAGE_FORMAT.FIF_JP2       },
            { PixFileFormat.Pfm,        FREE_IMAGE_FORMAT.FIF_PFM       },
            { PixFileFormat.Pict,       FREE_IMAGE_FORMAT.FIF_PICT      },
            { PixFileFormat.Raw,        FREE_IMAGE_FORMAT.FIF_RAW       },
        };

        #endregion

        #region Bitmap to PixImage

        private static PixImage BitmapToPixImage(FIBITMAP dib)
        {
            var sx = (int)FreeImage.GetWidth(dib);
            var sy = (int)FreeImage.GetHeight(dib);
            var delta = (int)FreeImage.GetPitch(dib);
            var ftype = FreeImage.GetImageType(dib);
            var bpp = FreeImage.GetBPP(dib);

            var bits = FreeImage.GetBits(dib) + sy * delta;

            switch (ftype)
            {
                case FREE_IMAGE_TYPE.FIT_BITMAP:
                    switch (bpp)
                    {
                        case 1:
                            {
                                var palette = FreeImage.GetPaletteEx(dib);
                                var pi = new PixImage<byte>(Col.Format.BW, sx, sy, 1);
                                var data = pi.Volume.Data;
                                int i = 0;
                                if (palette != null &&
                                    palette[0].rgbRed + palette[0].rgbGreen + palette[0].rgbBlue >= 384)
                                {
                                    for (var y = 0; y < sy; y++)
                                    {
                                        bits -= delta;
                                        byte bit = 0x80; int bi = 0;
                                        unsafe
                                        {
                                            byte* pixel = (byte*)bits;
                                            for (var x = 0; x < sx; x++)
                                            {
                                                data[i++] = ((pixel[bi] & bit) == 0) ? (byte)255 : (byte)0;
                                                bit >>= 1; if (bit == 0) { bit = 0x80; bi++; }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    for (var y = 0; y < sy; y++)
                                    {
                                        bits -= delta;
                                        byte bit = 0x80; int bi = 0;
                                        unsafe
                                        {
                                            byte* pixel = (byte*)bits;
                                            for (var x = 0; x < sx; x++)
                                            {
                                                data[i++] = ((pixel[bi] & bit) != 0) ? (byte)255 : (byte)0;
                                                bit >>= 1; if (bit == 0) { bit = 0x80; bi++; }
                                            }
                                        }
                                    }
                                }
                                return pi;
                            }
                        case 8:
                            {
                                var pi = new PixImage<byte>(sx, sy, 1);
                                var data = pi.Volume.Data;
                                long i = 0;
                                for (var y = 0; y < sy; y++)
                                {
                                    bits -= delta;
                                    unsafe
                                    {
                                        Byte* pixel = (Byte*)bits;
                                        for (var x = 0; x < sx; x++)
                                            data[i++] = pixel[x];
                                    }
                                }
                                return pi;
                            }
                        case 24:
                            {
                                var pi = new PixImage<byte>(sx, sy, 3);
                                var data = pi.Volume.Data;
                                long i = 0;
                                for (var y = 0; y < sy; y++)
                                {
                                    bits -= delta;
                                    unsafe
                                    {
                                        Byte* pixel = (Byte*)bits;
                                        for (var x = 0; x < sx; x++)
                                        {
                                            data[i++] = pixel[FreeImage.FI_RGBA_BLUE];
                                            data[i++] = pixel[FreeImage.FI_RGBA_GREEN];
                                            data[i++] = pixel[FreeImage.FI_RGBA_RED];
                                            pixel += 3;
                                        }
                                    }
                                }
                                return pi;
                            }
                        case 32:
                            {
                                var pi = new PixImage<byte>(sx, sy, 4);
                                var data = pi.Volume.Data;
                                long i = 0;
                                for (var y = 0; y < sy; y++)
                                {
                                    bits -= delta;
                                    unsafe
                                    {
                                        Byte* pixel = (Byte*)bits;
                                        for (var x = 0; x < sx; x++)
                                        {
                                            data[i++] = pixel[FreeImage.FI_RGBA_BLUE];
                                            data[i++] = pixel[FreeImage.FI_RGBA_GREEN];
                                            data[i++] = pixel[FreeImage.FI_RGBA_RED];
                                            data[i++] = pixel[FreeImage.FI_RGBA_ALPHA];
                                            pixel += 4;
                                        }
                                    }
                                }
                                return pi;
                            }
                    }
                    break;
                case FREE_IMAGE_TYPE.FIT_UINT16:
                    {
                        var pi = new PixImage<ushort>(sx, sy, 1);
                        var data = pi.Volume.Data;
                        long i = 0;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                ushort* pixel = (ushort*)bits;
                                for (var x = 0; x < sx; x++)
                                    data[i++] = pixel[x];
                            }
                        }
                        return pi;
                    }
                case FREE_IMAGE_TYPE.FIT_INT16:
                    {
                        var pi = new PixImage<short>(sx, sy, 1);
                        var data = pi.Volume.Data;
                        long i = 0;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                short* pixel = (short*)bits;
                                for (var x = 0; x < sx; x++)
                                    data[i++] = pixel[x];
                            }
                        }
                        return pi;
                    }
                case FREE_IMAGE_TYPE.FIT_UINT32:
                    {
                        var pi = new PixImage<uint>(sx, sy, 1);
                        var data = pi.Volume.Data;
                        long i = 0;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                uint* pixel = (uint*)bits;
                                for (var x = 0; x < sx; x++)
                                    data[i++] = pixel[x];
                            }
                        }
                        return pi;
                    }
                case FREE_IMAGE_TYPE.FIT_INT32:
                    {
                        var pi = new PixImage<int>(sx, sy, 1);
                        var data = pi.Volume.Data;
                        long i = 0;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                int* pixel = (int*)bits;
                                for (var x = 0; x < sx; x++)
                                    data[i++] = pixel[x];
                            }
                        }
                        return pi;
                    }
                case FREE_IMAGE_TYPE.FIT_FLOAT:
                    {
                        var pi = new PixImage<float>(sx, sy, 1);
                        var data = pi.Volume.Data;
                        long i = 0;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                float* pixel = (float*)bits;
                                for (var x = 0; x < sx; x++)
                                    data[i++] = pixel[x];
                            }
                        }
                        return pi;
                    }
                case FREE_IMAGE_TYPE.FIT_DOUBLE:
                    {
                        var pi = new PixImage<double>(sx, sy, 1);
                        var data = pi.Volume.Data;
                        long i = 0;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                double* pixel = (double*)bits;
                                for (var x = 0; x < sx; x++)
                                    data[i++] = pixel[x];
                            }
                        }
                    }
                    break;
                case FREE_IMAGE_TYPE.FIT_COMPLEX:
                    {
                        var pi = new PixImage<double>(sx, sy, 2);
                        var data = pi.Volume.Data;
                        long i = 0;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                FICOMPLEX* pixel = (FICOMPLEX*)bits;
                                for (var x = 0; x < sx; x++)
                                {
                                    data[i++] = pixel[x].real;
                                    data[i++] = pixel[x].imag;
                                }
                            }
                        }
                        return pi;
                    }
                case FREE_IMAGE_TYPE.FIT_RGB16:
                    {
                        var pi = new PixImage<ushort>(sx, sy, 3);
                        var data = pi.Volume.Data;
                        long i = 0;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                FIRGB16* pixel = (FIRGB16*)bits;
                                for (var x = 0; x < sx; x++)
                                {
                                    data[i++] = pixel[x].red;
                                    data[i++] = pixel[x].green;
                                    data[i++] = pixel[x].blue;
                                }
                            }
                        }
                        return pi;
                    }
                case FREE_IMAGE_TYPE.FIT_RGBF:
                    {
                        var pi = new PixImage<float>(sx, sy, 3);
                        var data = pi.Volume.Data;
                        long i = 0;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                FIRGBF* pixel = (FIRGBF*)bits;
                                for (var x = 0; x < sx; x++)
                                {
                                    data[i++] = pixel[x].red;
                                    data[i++] = pixel[x].green;
                                    data[i++] = pixel[x].blue;
                                }
                            }
                        }
                        return pi;
                    }
                case FREE_IMAGE_TYPE.FIT_RGBA16:
                    {
                        var pi = new PixImage<ushort>(sx, sy, 4);
                        var data = pi.Volume.Data;
                        long i = 0;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                FIRGBA16* pixel = (FIRGBA16*)bits;
                                for (var x = 0; x < sx; x++)
                                {
                                    data[i++] = pixel[x].red;
                                    data[i++] = pixel[x].green;
                                    data[i++] = pixel[x].blue;
                                    data[i++] = pixel[x].alpha;
                                }
                            }
                        }
                        return pi;
                    }
                case FREE_IMAGE_TYPE.FIT_RGBAF:
                    {
                        var pi = new PixImage<float>(sx, sy, 4);
                        var data = pi.Volume.Data;
                        long i = 0;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                FIRGBAF* pixel = (FIRGBAF*)bits;
                                for (var x = 0; x < sx; x++)
                                {
                                    data[i++] = pixel[x].red;
                                    data[i++] = pixel[x].green;
                                    data[i++] = pixel[x].blue;
                                    data[i++] = pixel[x].alpha;
                                }
                            }
                        }
                        return pi;
                    }
            }

            throw new NotSupportedException($"Unsupported image type {ftype}");
        }

        #endregion

        #region PixImage to Bitmap

        private static void CheckLayout(VolumeInfo vi)
        {
            if (vi.DZ != 1L)
                throw new ArgumentException($"Volume must have DZ = 1 (is {vi.DZ}");

            if (vi.SZ != vi.DX)
                throw new ArgumentException($"Volume must have SZ = DX");
        }

        private static FIBITMAP PixImageToBitmap(PixImage<byte> pi)
        {
            CheckLayout(pi.Volume.Info);
            var sx = pi.Size.X;
            var sy = pi.Size.Y;
            var bpp = pi.ChannelCount == 1 && pi.Format == Col.Format.BW ? 1 : pi.ChannelCount * 8;
            var data = pi.Volume.Data;
            long i = pi.Volume.FirstIndex;
            long j = pi.Volume.JY;

            switch (bpp)
            {
                case 1:
                    {
                        var dib = FreeImage.Allocate(sx, sy, bpp);
                        var delta = (int)FreeImage.GetPitch(dib);
                        var bits = FreeImage.GetBits(dib) + sy * delta;
                        var palette = FreeImage.GetPaletteEx(dib);
                        if (palette != null) // should alway be != null
                        {
                            palette[0] = new RGBQUAD { rgbRed = 0, rgbGreen = 0, rgbBlue = 0 };
                            palette[1] = new RGBQUAD { rgbRed = 255, rgbGreen = 255, rgbBlue = 255 };
                        }
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            byte bit = 0x80;
                            int bi = 0;
                            unsafe
                            {
                                byte* pixel = (byte*)bits;
                                for (var x = 0; x < sx; x++)
                                {
                                    if ((data[i++] & 0x80) != 0) pixel[bi] |= bit;
                                    bit >>= 1; if (bit == 0) { bit = 0x80; bi++; }
                                }
                            }
                            i += j;
                        }

                        return dib;
                    }
                case 8:
                    {
                        var dib = FreeImage.Allocate(sx, sy, bpp);
                        var delta = (int)FreeImage.GetPitch(dib);
                        var bits = FreeImage.GetBits(dib) + sy * delta;

                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                byte* pixel = (byte*)bits;
                                for (var x = 0; x < sx; x++)
                                    pixel[x] = data[i++];
                            }
                            i += j;
                        }

                        return dib;
                    }
                case 24:
                    {
                        var dib = FreeImage.Allocate(sx, sy, bpp);
                        var delta = (int)FreeImage.GetPitch(dib);
                        var bits = FreeImage.GetBits(dib) + sy * delta;
                        var channelOrder = new[] { FreeImage.FI_RGBA_BLUE, FreeImage.FI_RGBA_GREEN, FreeImage.FI_RGBA_RED };
                        if (pi.Format == Col.Format.RGB) channelOrder = new[] { FreeImage.FI_RGBA_RED, FreeImage.FI_RGBA_GREEN, FreeImage.FI_RGBA_BLUE };

                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                byte* pixel = (byte*)bits;
                                for (var x = 0; x < sx; x++)
                                {
                                    pixel[channelOrder[0]] = data[i++];
                                    pixel[channelOrder[1]] = data[i++];
                                    pixel[channelOrder[2]] = data[i++];
                                    pixel += 3;
                                }
                            }
                            i += j;
                        }

                        return dib;
                    }
                case 32:
                    {
                        var dib = FreeImage.Allocate(sx, sy, bpp);
                        var delta = (int)FreeImage.GetPitch(dib);
                        var bits = FreeImage.GetBits(dib) + sy * delta;
                        var channelOrder = new[] { FreeImage.FI_RGBA_BLUE, FreeImage.FI_RGBA_GREEN, FreeImage.FI_RGBA_RED, FreeImage.FI_RGBA_ALPHA };
                        if (pi.Format == Col.Format.RGBA) channelOrder = new[] { FreeImage.FI_RGBA_RED, FreeImage.FI_RGBA_GREEN, FreeImage.FI_RGBA_BLUE, FreeImage.FI_RGBA_ALPHA };

                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                byte* pixel = (byte*)bits;
                                for (var x = 0; x < sx; x++)
                                {
                                    pixel[channelOrder[0]] = data[i++];
                                    pixel[channelOrder[1]] = data[i++];
                                    pixel[channelOrder[2]] = data[i++];
                                    pixel[channelOrder[3]] = data[i++];
                                    pixel += 4;
                                }
                            }
                            i += j;
                        }

                        return dib;
                    }
                default:
                    throw new ArgumentException($"Invalid channel count {pi.ChannelCount}");
            }
        }

        private static FIBITMAP PixImageToBitmap(PixImage<ushort> pi)
        {
            CheckLayout(pi.Volume.Info);
            var sx = pi.Size.X;
            var sy = pi.Size.Y;
            var data = pi.Volume.Data;
            long i = pi.Volume.FirstIndex;
            long j = pi.Volume.JY;

            switch (pi.ChannelCount)
            {
                case 1:
                    {
                        var dib = FreeImage.AllocateT(FREE_IMAGE_TYPE.FIT_UINT16, sx, sy, 16);
                        var delta = (int)FreeImage.GetPitch(dib);
                        var bits = FreeImage.GetBits(dib) + sy * delta;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                ushort* pixel = (ushort*)bits;
                                for (var x = 0; x < sx; x++)
                                    pixel[x] = data[i++];
                            }
                            i += j;
                        }
                        return dib;
                    }
                case 3:
                    {
                        var dib = FreeImage.AllocateT(FREE_IMAGE_TYPE.FIT_RGB16, sx, sy, 48);
                        var delta = (int)FreeImage.GetPitch(dib);
                        var bits = FreeImage.GetBits(dib) + sy * delta;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                FIRGB16* pixel = (FIRGB16*)bits;
                                for (var x = 0; x < sx; x++)
                                {
                                    pixel[x].red = data[i++];
                                    pixel[x].green = data[i++];
                                    pixel[x].blue = data[i++];
                                }
                            }
                            i += j;
                        }
                        return dib;
                    }
                case 4:
                    {
                        var dib = FreeImage.AllocateT(FREE_IMAGE_TYPE.FIT_RGBA16, sx, sy, 64);
                        var delta = (int)FreeImage.GetPitch(dib);
                        var bits = FreeImage.GetBits(dib) + sy * delta;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                FIRGBA16* pixel = (FIRGBA16*)bits;
                                for (var x = 0; x < sx; x++)
                                {
                                    pixel[x].red = data[i++];
                                    pixel[x].green = data[i++];
                                    pixel[x].blue = data[i++];
                                    pixel[x].alpha = data[i++];
                                }
                            }
                            i += j;
                        }
                        return dib;
                    }
                default:
                    throw new ArgumentException($"Invalid channel count {pi.ChannelCount}");
            }
        }

        private static FIBITMAP PixImageToBitmap(PixImage<float> pi)
        {
            CheckLayout(pi.Volume.Info);
            var sx = pi.Size.X;
            var sy = pi.Size.Y;
            var data = pi.Volume.Data;
            long i = pi.Volume.FirstIndex;
            long j = pi.Volume.JY;

            switch (pi.ChannelCount)
            {
                case 1:
                    {
                        var dib = FreeImage.AllocateT(FREE_IMAGE_TYPE.FIT_FLOAT, sx, sy, 32);
                        var delta = (int)FreeImage.GetPitch(dib);
                        var bits = FreeImage.GetBits(dib) + sy * delta;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                float* pixel = (float*)bits;
                                for (var x = 0; x < sx; x++)
                                    pixel[x] = data[i++];
                            }
                            i += j;
                        }
                        return dib;
                    }
                case 3:
                    {
                        var dib = FreeImage.AllocateT(FREE_IMAGE_TYPE.FIT_RGBF, sx, sy, 96);
                        var delta = (int)FreeImage.GetPitch(dib);
                        var bits = FreeImage.GetBits(dib) + sy * delta;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                FIRGBF* pixel = (FIRGBF*)bits;
                                for (var x = 0; x < sx; x++)
                                {
                                    pixel[x].red = data[i++];
                                    pixel[x].green = data[i++];
                                    pixel[x].blue = data[i++];
                                }
                            }
                            i += j;
                        }
                        return dib;
                    }
                case 4:
                    {
                        var dib = FreeImage.AllocateT(FREE_IMAGE_TYPE.FIT_RGBAF, sx, sy, 128);
                        var delta = (int)FreeImage.GetPitch(dib);
                        var bits = FreeImage.GetBits(dib) + sy * delta;
                        for (var y = 0; y < sy; y++)
                        {
                            bits -= delta;
                            unsafe
                            {
                                FIRGBAF* pixel = (FIRGBAF*)bits;
                                for (var x = 0; x < sx; x++)
                                {
                                    pixel[x].red = data[i++];
                                    pixel[x].green = data[i++];
                                    pixel[x].blue = data[i++];
                                    pixel[x].alpha = data[i++];
                                }
                            }
                            i += j;
                        }
                        return dib;
                    }
                default:
                    throw new ArgumentException($"Invalid channel count {pi.ChannelCount}");
            }
        }

        private static FIBITMAP PixImageToBitmap(PixImage image)
        {
            switch (image)
            {
                case PixImage<byte> pi:
                    return PixImageToBitmap(pi);

                case PixImage<ushort> pi:
                    return PixImageToBitmap(pi);

                case PixImage<float> pi:
                    return PixImageToBitmap(pi);

                default:
                    throw new NotSupportedException($"Cannot save PixImage of type {image.PixFormat.Type}");
            }
        }

        #endregion

        #region Loader

        private class PixLoader : IPixLoader
        {
            public string Name => "FreeImage";

            #region Load

            private static PixImage Load(Func<FIBITMAP> loadBitmap)
            {
                var bitmap = loadBitmap();
                if (!bitmap.IsNull)
                {
                    try
                    {
                        return BitmapToPixImage(bitmap);
                    }
                    finally
                    {
                        FreeImage.Unload(bitmap);
                    }
                }

                return null;
            }

            public PixImage LoadFromFile(string filename)
                => Load(() => FreeImage.LoadEx(filename));

            public PixImage LoadFromStream(Stream stream)
                => Load(() => FreeImage.LoadFromStream(stream));

            #endregion

            #region Save

            private static void Save(PixImage pi, PixSaveParams saveParams, string saveMethod, Func<FIBITMAP, FREE_IMAGE_FORMAT, FREE_IMAGE_SAVE_FLAGS, bool> saveBitmap)
            {
                if (!s_fileFormats.TryGetValue(saveParams.Format, out FREE_IMAGE_FORMAT format))
                    throw new NotSupportedException($"Unsupported PixImage file format {saveParams.Format}.");

                var bitmap = PixImageToBitmap(pi);

                try
                {
                    var flags = FREE_IMAGE_SAVE_FLAGS.DEFAULT;

                    if (saveParams is PixPngSaveParams png)
                    {
                        if (png.CompressionLevel > 0)
                            flags = (FREE_IMAGE_SAVE_FLAGS)png.CompressionLevel;
                        else
                            flags = FREE_IMAGE_SAVE_FLAGS.PNG_Z_NO_COMPRESSION;
                    }
                    else if (saveParams is PixJpegSaveParams jpeg)
                    {
                        flags = (FREE_IMAGE_SAVE_FLAGS)jpeg.Quality;
                    }
                    else if (saveParams.Format == PixFileFormat.Exr && pi.PixFormat.Type == typeof(float))
                    {
                        flags = FREE_IMAGE_SAVE_FLAGS.EXR_FLOAT;
                    }

                    if (!saveBitmap(bitmap, format, flags))
                        throw new ImageLoadException($"FreeImage.{saveMethod}() failed.");
                }
                finally
                {
                    FreeImage.UnloadEx(ref bitmap);
                }
            }

            public void SaveToFile(string filename, PixImage image, PixSaveParams saveParams)
                => Save(image, saveParams, "Save", (bitmap, format, flags) => FreeImage.Save(format, bitmap, filename, flags));

            public void SaveToStream(Stream stream, PixImage image, PixSaveParams saveParams)
                => Save(image, saveParams, "SaveToStream", (bitmap, format, flags) => FreeImage.SaveToStream(bitmap, stream, format, flags));

            #endregion

            #region GetInfo

            public PixImageInfo GetInfoFromFile(string filename)
                => throw new NotSupportedException($"{Name} loader does not support getting info.");

            public PixImageInfo GetInfoFromStream(Stream stream)
                => throw new NotSupportedException($"{Name} loader does not support getting info.");

            #endregion
        }

        public static readonly IPixLoader Loader = new PixLoader();

        #endregion
    }
}