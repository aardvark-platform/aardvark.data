using System;
using System.IO;
using System.Collections.Generic;
using FreeImageAPI;
using Aardvark.Base;

namespace Aardvark.Data
{
    public static class PixImageFreeImage
    {
        [OnAardvarkInit]
        public static void Init()
        {
            FreeImageEngine.LastErrorMessage = null; // Invoke static constructor to register callback
            PixImage.AddLoader(Loader);
        }

        #region Static Tables and Methods

        private static readonly Dictionary<PixFileFormat, FREE_IMAGE_FORMAT> s_fileFormats = new()
        {
            { PixFileFormat.Unknown, FREE_IMAGE_FORMAT.FIF_UNKNOWN   },
            { PixFileFormat.Bmp,     FREE_IMAGE_FORMAT.FIF_BMP       },
            { PixFileFormat.Ico,     FREE_IMAGE_FORMAT.FIF_ICO       },
            { PixFileFormat.Jpeg,    FREE_IMAGE_FORMAT.FIF_JPEG      },
            { PixFileFormat.Jng,     FREE_IMAGE_FORMAT.FIF_JNG       },
            { PixFileFormat.Koala,   FREE_IMAGE_FORMAT.FIF_KOALA     },
            { PixFileFormat.Lbm,     FREE_IMAGE_FORMAT.FIF_LBM       },
            { PixFileFormat.Iff,     FREE_IMAGE_FORMAT.FIF_IFF       },
            { PixFileFormat.Mng,     FREE_IMAGE_FORMAT.FIF_MNG       },
            { PixFileFormat.Pbm,     FREE_IMAGE_FORMAT.FIF_PBM       },
            { PixFileFormat.PbmRaw,  FREE_IMAGE_FORMAT.FIF_PBMRAW    },
            { PixFileFormat.Pcd,     FREE_IMAGE_FORMAT.FIF_PCD       },
            { PixFileFormat.Pcx,     FREE_IMAGE_FORMAT.FIF_PCX       },
            { PixFileFormat.Pgm,     FREE_IMAGE_FORMAT.FIF_PGM       },
            { PixFileFormat.PgmRaw,  FREE_IMAGE_FORMAT.FIF_PGMRAW    },
            { PixFileFormat.Png,     FREE_IMAGE_FORMAT.FIF_PNG       },
            { PixFileFormat.Ppm,     FREE_IMAGE_FORMAT.FIF_PPM       },
            { PixFileFormat.PpmRaw,  FREE_IMAGE_FORMAT.FIF_PPMRAW    },
            { PixFileFormat.Ras,     FREE_IMAGE_FORMAT.FIF_RAS       },
            { PixFileFormat.Targa,   FREE_IMAGE_FORMAT.FIF_TARGA     },
            { PixFileFormat.Tiff,    FREE_IMAGE_FORMAT.FIF_TIFF      },
            { PixFileFormat.Wbmp,    FREE_IMAGE_FORMAT.FIF_WBMP      },
            { PixFileFormat.Psd,     FREE_IMAGE_FORMAT.FIF_PSD       },
            { PixFileFormat.Cut,     FREE_IMAGE_FORMAT.FIF_CUT       },
            { PixFileFormat.Xbm,     FREE_IMAGE_FORMAT.FIF_XBM       },
            { PixFileFormat.Xpm,     FREE_IMAGE_FORMAT.FIF_XPM       },
            { PixFileFormat.Dds,     FREE_IMAGE_FORMAT.FIF_DDS       },
            { PixFileFormat.Gif,     FREE_IMAGE_FORMAT.FIF_GIF       },
            { PixFileFormat.Hdr,     FREE_IMAGE_FORMAT.FIF_HDR       },
            { PixFileFormat.Sgi,     FREE_IMAGE_FORMAT.FIF_SGI       },
            { PixFileFormat.Exr,     FREE_IMAGE_FORMAT.FIF_EXR       },
            { PixFileFormat.J2k,     FREE_IMAGE_FORMAT.FIF_J2K       },
            { PixFileFormat.Jp2,     FREE_IMAGE_FORMAT.FIF_JP2       },
            { PixFileFormat.Pfm,     FREE_IMAGE_FORMAT.FIF_PFM       },
            { PixFileFormat.Pict,    FREE_IMAGE_FORMAT.FIF_PICT      },
            { PixFileFormat.Raw,     FREE_IMAGE_FORMAT.FIF_RAW       },
            { PixFileFormat.Webp,    FREE_IMAGE_FORMAT.FIF_WEBP      },
        };

        private static readonly Dictionary<PixFormat, Func<PixImage, FIBITMAP>> s_bitmapCreators = new()
        {
            { PixFormat.ByteBW,     pi => PixImageToBitmapBW((PixImage<byte>)pi) },
            { PixFormat.ByteGray,   pi => PixImageToBitmap((PixImage<byte>)pi, FREE_IMAGE_TYPE.FIT_BITMAP) },
            { PixFormat.ByteRGB,    pi => PixImageToBitmap((PixImage<byte>)pi, FREE_IMAGE_TYPE.FIT_BITMAP) },
            { PixFormat.ByteRGBA,   pi => PixImageToBitmap((PixImage<byte>)pi, FREE_IMAGE_TYPE.FIT_BITMAP) },
            { PixFormat.ByteRGBP,   pi => PixImageToBitmap((PixImage<byte>)pi, FREE_IMAGE_TYPE.FIT_BITMAP) },
            { PixFormat.ByteBGR,    pi => PixImageToBitmap((PixImage<byte>)pi, FREE_IMAGE_TYPE.FIT_BITMAP) },
            { PixFormat.ByteBGRA,   pi => PixImageToBitmap((PixImage<byte>)pi, FREE_IMAGE_TYPE.FIT_BITMAP) },
            { PixFormat.ByteBGRP,   pi => PixImageToBitmap((PixImage<byte>)pi, FREE_IMAGE_TYPE.FIT_BITMAP) },

            { PixFormat.ShortGray,  pi => PixImageToBitmap((PixImage<short>)pi,  FREE_IMAGE_TYPE.FIT_INT16) },
            { PixFormat.UShortGray, pi => PixImageToBitmap((PixImage<ushort>)pi, FREE_IMAGE_TYPE.FIT_UINT16) },
            { PixFormat.UShortRGB,  pi => PixImageToBitmap((PixImage<ushort>)pi, FREE_IMAGE_TYPE.FIT_RGB16) },
            { PixFormat.UShortBGR,  pi => PixImageToBitmap((PixImage<ushort>)pi, FREE_IMAGE_TYPE.FIT_RGB16) },
            { PixFormat.UShortRGBA, pi => PixImageToBitmap((PixImage<ushort>)pi, FREE_IMAGE_TYPE.FIT_RGBA16) },
            { PixFormat.UShortBGRA, pi => PixImageToBitmap((PixImage<ushort>)pi, FREE_IMAGE_TYPE.FIT_RGBA16) },
            { PixFormat.UShortRGBP, pi => PixImageToBitmap((PixImage<ushort>)pi, FREE_IMAGE_TYPE.FIT_RGBA16) },
            { PixFormat.UShortBGRP, pi => PixImageToBitmap((PixImage<ushort>)pi, FREE_IMAGE_TYPE.FIT_RGBA16) },

            { PixFormat.IntGray,    pi => PixImageToBitmap((PixImage<int>)pi,  FREE_IMAGE_TYPE.FIT_INT32) },
            { PixFormat.UIntGray,   pi => PixImageToBitmap((PixImage<uint>)pi, FREE_IMAGE_TYPE.FIT_UINT32) },

            { PixFormat.FloatGray,  pi => PixImageToBitmap((PixImage<float>)pi, FREE_IMAGE_TYPE.FIT_FLOAT) },
            { PixFormat.FloatRGB,   pi => PixImageToBitmap((PixImage<float>)pi, FREE_IMAGE_TYPE.FIT_RGBF) },
            { PixFormat.FloatBGR,   pi => PixImageToBitmap((PixImage<float>)pi, FREE_IMAGE_TYPE.FIT_RGBF) },
            { PixFormat.FloatRGBA,  pi => PixImageToBitmap((PixImage<float>)pi, FREE_IMAGE_TYPE.FIT_RGBAF) },
            { PixFormat.FloatBGRA,  pi => PixImageToBitmap((PixImage<float>)pi, FREE_IMAGE_TYPE.FIT_RGBAF) },
            { PixFormat.FloatRGBP,  pi => PixImageToBitmap((PixImage<float>)pi, FREE_IMAGE_TYPE.FIT_RGBAF) },
            { PixFormat.FloatBGRP,  pi => PixImageToBitmap((PixImage<float>)pi, FREE_IMAGE_TYPE.FIT_RGBAF) },

            { PixFormat.DoubleGray, pi => PixImageToBitmap((PixImage<double>)pi, FREE_IMAGE_TYPE.FIT_DOUBLE) },
            { PixFormat.DoubleRG,   pi => PixImageToBitmap((PixImage<double>)pi, FREE_IMAGE_TYPE.FIT_COMPLEX) },
        };

        private static readonly Dictionary<FREE_IMAGE_TYPE, Func<FIBITMAP, PixImage>> s_pixImageCreators = new()
        {
            { FREE_IMAGE_TYPE.FIT_BITMAP,  BitmapToPixImage },

            { FREE_IMAGE_TYPE.FIT_INT16,   bitmap => BitmapToPixImage<short>(bitmap, Col.Format.Gray) },
            { FREE_IMAGE_TYPE.FIT_UINT16,  bitmap => BitmapToPixImage<ushort>(bitmap, Col.Format.Gray) },
            { FREE_IMAGE_TYPE.FIT_RGB16,   bitmap => BitmapToPixImage<ushort>(bitmap, Col.Format.BGR) },
            { FREE_IMAGE_TYPE.FIT_RGBA16,  bitmap => BitmapToPixImage<ushort>(bitmap, Col.Format.BGRA) },

            { FREE_IMAGE_TYPE.FIT_INT32,   bitmap => BitmapToPixImage<int>(bitmap, Col.Format.Gray) },
            { FREE_IMAGE_TYPE.FIT_UINT32,  bitmap => BitmapToPixImage<uint>(bitmap, Col.Format.Gray) },

            { FREE_IMAGE_TYPE.FIT_FLOAT,   bitmap => BitmapToPixImage<float>(bitmap, Col.Format.Gray) },
            { FREE_IMAGE_TYPE.FIT_RGBF,    bitmap => BitmapToPixImage<float>(bitmap, Col.Format.BGR) },
            { FREE_IMAGE_TYPE.FIT_RGBAF,   bitmap => BitmapToPixImage<float>(bitmap, Col.Format.BGRA) },

            { FREE_IMAGE_TYPE.FIT_DOUBLE,  bitmap => BitmapToPixImage<double>(bitmap, Col.Format.Gray) },
            { FREE_IMAGE_TYPE.FIT_COMPLEX, bitmap => BitmapToPixImage<double>(bitmap, Col.Format.RG) },
        };

        private static Col.Format SwapRGB(this Col.Format format)
        {
            return format switch
            {
                Col.Format.RGB => Col.Format.BGR,
                Col.Format.BGR => Col.Format.RGB,
                Col.Format.RGBA => Col.Format.BGRA,
                Col.Format.BGRA => Col.Format.RGBA,
                Col.Format.RGBP => Col.Format.BGRP,
                Col.Format.BGRP => Col.Format.RGBP,
                _ => format
            };
        }

        private static Col.Format WithoutAlpha(this Col.Format format)
            => format switch
            {
                Col.Format.BGRA => Col.Format.BGR,
                Col.Format.BGRP => Col.Format.BGR,
                Col.Format.RGBA => Col.Format.RGB,
                Col.Format.RGBP => Col.Format.RGB,
                _ => format
            };

        private static bool SupportsAlpha(this PixFileFormat format)
            => format switch
            {
                PixFileFormat.Jpeg => false,
                PixFileFormat.Koala => false,
                PixFileFormat.Ras => false,
                PixFileFormat.Wbmp => false,
                PixFileFormat.Cut => false,
                PixFileFormat.Xbm => false,
                PixFileFormat.Hdr => false,
                PixFileFormat.Raw => false,
                _ => true
            };

        #endregion

        #region Bitmap to PixImage

        private static PixImage<T> BitmapToPixImage<T>(FIBITMAP bitmap, Col.Format format) where T : unmanaged
        {
            var sx = (int)FreeImage.GetWidth(bitmap);
            var sy = (int)FreeImage.GetHeight(bitmap);
            var delta = (int)FreeImage.GetPitch(bitmap);
            var bits = FreeImage.GetBits(bitmap) + sy * delta;

            var pi = new PixImage<T>(format, sx, sy);
            var data = pi.Volume.Data;
            long i = 0;

            var channelOrder = pi.Format.SwapRGB().ChannelOrder(); // FreeImage uses BGR layout while Aardvark assumes RGB as default

            for (var y = 0; y < sy; y++)
            {
                bits -= delta;
                unsafe
                {
                    T* pixel = (T*)bits;
                    for (var x = 0; x < sx; x++)
                    {
                        for (var c = 0; c < pi.ChannelCount; c++)
                        {
                            data[i++] = pixel[channelOrder[c]];
                        }
                        pixel += pi.ChannelCount;
                    }
                }
            }
            return pi;
        }

        private static PixImage BitmapToPixImage(FIBITMAP bitmap)
        {
            var bpp = FreeImage.GetBPP(bitmap);

            if (bpp == 1)
            {
                var sx = (int)FreeImage.GetWidth(bitmap);
                var sy = (int)FreeImage.GetHeight(bitmap);
                var delta = (int)FreeImage.GetPitch(bitmap);
                var bits = FreeImage.GetBits(bitmap) + sy * delta;
                var palette = FreeImage.GetPaletteEx(bitmap);
                var pi = new PixImage<byte>(Col.Format.BW, sx, sy);
                var data = pi.Volume.Data;
                int i = 0;
                if (palette != null && palette[0].rgbRed + palette[0].rgbGreen + palette[0].rgbBlue >= 384)
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

            var format = bpp switch
            {
                8 => Col.Format.Gray,
                24 => Col.Format.BGR,
                32 => Col.Format.BGRA,
                _ => throw new NotSupportedException($"Bit depth {bpp} is not supported.")
            };

            return BitmapToPixImage<byte>(bitmap, format);
        }

        #endregion

        #region PixImage to Bitmap

        private static PixImage ToDenseLayout(this PixImage pi, bool removeAlpha)
        {
            // PixImage -> Bitmap copy implementation requires a dense layout
            var isDenseLayout = pi.VolumeInfo.DZ == 1L && pi.VolumeInfo.SZ == pi.VolumeInfo.DX;

            // Trying to save an RGBA picture as JPEG fails because of the alpha channel
            // => make sub volume and copy to dense layout again...
            // Suboptimal but an easy workaround
            var format = removeAlpha ? pi.Format.WithoutAlpha() : pi.Format;

            if (isDenseLayout && pi.Format == format) return pi;
            return pi.ToPixImage(format);
        }

        private static FIBITMAP PixImageToBitmap<T>(PixImage<T> pi, FREE_IMAGE_TYPE imageType) where T : unmanaged
        {
            var sx = pi.Size.X;
            var sy = pi.Size.Y;
            var bpp = pi.ChannelCount * typeof(T).GetCLRSize() * 8;
            var data = pi.Volume.Data;
            long i = pi.Volume.FirstIndex;
            long j = pi.Volume.JY;

            var bitmap = FreeImage.AllocateT(imageType, sx, sy, bpp);
            var delta = (int)FreeImage.GetPitch(bitmap);
            var bits = FreeImage.GetBits(bitmap) + sy * delta;

            var channelOrder = pi.Format.SwapRGB().ChannelOrder(); // FreeImage uses BGR layout while Aardvark assumes RGB as default

            for (var y = 0; y < sy; y++)
            {
                bits -= delta;
                unsafe
                {
                    T* pixel = (T*)bits;
                    for (var x = 0; x < sx; x++)
                    {
                        for (var c = 0; c < pi.ChannelCount; c++)
                        {
                            pixel[channelOrder[c]] = data[i++];
                        }
                        pixel += pi.ChannelCount;
                    }
                }
                i += j;
            }

            return bitmap;
        }

        private static FIBITMAP PixImageToBitmapBW(PixImage<byte> pi)
        {
            var sx = pi.Size.X;
            var sy = pi.Size.Y;
            var data = pi.Volume.Data;
            long i = pi.Volume.FirstIndex;
            long j = pi.Volume.JY;

            var dib = FreeImage.Allocate(sx, sy, 1);
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

        #endregion

        #region Loader

        private static ImageLoadException InternalError(string message)
        {
            var error = FreeImageEngine.LastErrorMessage;
            FreeImageEngine.LastErrorMessage = null;
            return new ImageLoadException(string.IsNullOrEmpty(error) ? message : $"{message}: {error}");
        }

        private class PixLoader : IPixLoader
        {
            public string Name => "FreeImage";

            public bool CanEncode => true;

            public bool CanDecode => true;

            #region Load

            private static PixImage Load(string loadMethod, Func<FIBITMAP> loadBitmap)
            {
                var bitmap = loadBitmap();
                if (bitmap.IsNull) throw InternalError($"FreeImage.{loadMethod}() failed");

                try
                {
                    var imageType = FreeImage.GetImageType(bitmap);

                    if (!s_pixImageCreators.TryGetValue(imageType, out var creator))
                    {
                        throw new NotSupportedException($"Image type {imageType} is not supported.");
                    }

                    return creator(bitmap);
                }
                finally
                {
                    FreeImage.Unload(bitmap);
                }
            }

            public PixImage LoadFromFile(string filename)
                => Load("LoadEx", () => FreeImage.LoadEx(filename));

            public PixImage LoadFromStream(Stream stream)
                => Load("LoadFromStream", () => FreeImage.LoadFromStream(stream));

            #endregion

            #region Save

            private static FREE_IMAGE_SAVE_FLAGS GetPngSaveFlags(PixPngSaveParams png)
                => (png.CompressionLevel > 0)
                    ? (FREE_IMAGE_SAVE_FLAGS)png.CompressionLevel
                    : FREE_IMAGE_SAVE_FLAGS.PNG_Z_NO_COMPRESSION;

            private static FREE_IMAGE_SAVE_FLAGS GetJpegSaveFlags(PixJpegSaveParams jpeg)
                => (FREE_IMAGE_SAVE_FLAGS)jpeg.Quality;

            private static FREE_IMAGE_SAVE_FLAGS GetWebpSaveFlags(PixWebpSaveParams webp)
                => webp.Lossless
                    ? FREE_IMAGE_SAVE_FLAGS.WEBP_LOSSLESS   // FreeImage does not support the quality parameter for lossless
                    : (FREE_IMAGE_SAVE_FLAGS)webp.Quality;

            private static FREE_IMAGE_SAVE_FLAGS GetExrSaveFlags(PixExrSaveParams exr, int channelCount)
            {
                var compression =
                    exr.Compression switch
                    {
                        PixExrCompression.None => FREE_IMAGE_SAVE_FLAGS.EXR_NONE,
                        PixExrCompression.Zip => FREE_IMAGE_SAVE_FLAGS.EXR_ZIP,
                        PixExrCompression.Piz => FREE_IMAGE_SAVE_FLAGS.EXR_PIZ,
                        PixExrCompression.Pxr24 => FREE_IMAGE_SAVE_FLAGS.EXR_PXR24,
                        PixExrCompression.B44 => FREE_IMAGE_SAVE_FLAGS.EXR_B44,
                        _ => FREE_IMAGE_SAVE_FLAGS.DEFAULT,
                    };

                if (channelCount > 1 && exr.LuminanceChroma)
                {
                    Report.Warn($"Luminance chroma compression is not supported");
                }

                return compression;
            }

            private static FREE_IMAGE_SAVE_FLAGS GetTiffSaveFlags(PixTiffSaveParams tiff)
                => tiff.Compression switch
                {
                    PixTiffCompression.None => FREE_IMAGE_SAVE_FLAGS.TIFF_NONE,
                    PixTiffCompression.Ccitt3 => FREE_IMAGE_SAVE_FLAGS.TIFF_CCITTFAX3,
                    PixTiffCompression.Ccitt4 => FREE_IMAGE_SAVE_FLAGS.TIFF_CCITTFAX4,
                    PixTiffCompression.Lzw => FREE_IMAGE_SAVE_FLAGS.TIFF_LZW,
                    PixTiffCompression.Jpeg => FREE_IMAGE_SAVE_FLAGS.TIFF_JPEG,
                    PixTiffCompression.Deflate => FREE_IMAGE_SAVE_FLAGS.TIFF_DEFLATE,
                    PixTiffCompression.PackBits => FREE_IMAGE_SAVE_FLAGS.TIFF_PACKBITS,
                    _ => FREE_IMAGE_SAVE_FLAGS.DEFAULT
                };

            private static void Save(PixImage pi, PixSaveParams saveParams, string saveMethod, Func<FIBITMAP, FREE_IMAGE_FORMAT, FREE_IMAGE_SAVE_FLAGS, bool> saveBitmap)
            {
                if (!s_fileFormats.TryGetValue(saveParams.Format, out FREE_IMAGE_FORMAT format))
                    throw new NotSupportedException($"Unsupported PixImage file format {saveParams.Format}.");

                // WebP plugin crashes if format is not supported.
                if (format == FREE_IMAGE_FORMAT.FIF_WEBP)
                {
                    if (pi.PixFormat.Type != typeof(byte))
                        throw new NotSupportedException($"WebP only supports 24-bit and 32-bit depths (Format = {pi.PixFormat}).");

                    if (pi.Format == Col.Format.Gray)
                        pi = pi.ToPixImage(Col.Format.BGR);
                }

                if (!s_bitmapCreators.TryGetValue(pi.PixFormat, out var creator))
                {
                    var supportedFormats = Environment.NewLine + string.Concat(s_bitmapCreators.Keys, Environment.NewLine);
                    throw new NotSupportedException($"Cannot save PixImage with pixel format {pi.PixFormat}. Supported formats:{supportedFormats}");
                }

                var removeAlpha = !saveParams.Format.SupportsAlpha();
                var bitmap = creator(pi.ToDenseLayout(removeAlpha));

                try
                {
                    var flags =
                        saveParams switch
                        {
                            PixPngSaveParams png => GetPngSaveFlags(png),
                            PixJpegSaveParams jpeg => GetJpegSaveFlags(jpeg),
                            PixWebpSaveParams webp => GetWebpSaveFlags(webp),
                            PixExrSaveParams exr => GetExrSaveFlags(exr, pi.ChannelCount),
                            PixTiffSaveParams tiff => GetTiffSaveFlags(tiff),
                            _ => FREE_IMAGE_SAVE_FLAGS.DEFAULT
                        };

                    if (saveParams.Format == PixFileFormat.Exr && pi.PixFormat.Type == typeof(float))
                    {
                        flags |= FREE_IMAGE_SAVE_FLAGS.EXR_FLOAT;
                    }

                    if (!saveBitmap(bitmap, format, flags))
                        throw InternalError($"FreeImage.{saveMethod}() failed");
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