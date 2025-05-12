using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FreeImageAPI;
using FreeImageAPI.IO;
using NUnit.Framework;

namespace FreeImageNETUnitTest.TestFixtures
{
    [TestFixture]
    public class IOTest
    {
        [Test]
        public void WriteStreamBmp()
        {
            FIBITMAP dib = FreeImage.Allocate(1000, 800, 24, 0xFF0000, 0xFF00, 0xFF);
            Assert.IsFalse(dib.IsNull);

            using (MemoryStream stream1 = new MemoryStream())
            using (MemoryStream stream2 = new MemoryStream())
            {
                FreeImage.IO = FreeImageStreamIO.IO;
                FreeImage.SaveToStream(dib, stream1, FREE_IMAGE_FORMAT.FIF_BMP);
                Assert.Greater(stream1.Position, 0);

                FreeImage.IO = SpanStreamIO.IO;
                FreeImage.SaveToStream(dib, stream2, FREE_IMAGE_FORMAT.FIF_BMP);
                Assert.Greater(stream2.Position, 0);

                Assert.IsTrue(Enumerable.SequenceEqual(stream1.ToArray(), stream2.ToArray()));
            }

            FreeImage.UnloadEx(ref dib);
        }

        [Test]
        public void WriteStreamJpeg()
        {
            FIBITMAP dib = FreeImage.Allocate(1000, 800, 24, 0xFF0000, 0xFF00, 0xFF);
            Assert.IsFalse(dib.IsNull);

            using (MemoryStream stream1 = new MemoryStream())
            using (MemoryStream stream2 = new MemoryStream())
            {
                FreeImage.IO = FreeImageStreamIO.IO;
                FreeImage.SaveToStream(dib, stream1, FREE_IMAGE_FORMAT.FIF_JPEG);
                Assert.Greater(stream1.Position, 0);

                FreeImage.IO = SpanStreamIO.IO;
                FreeImage.SaveToStream(dib, stream2, FREE_IMAGE_FORMAT.FIF_JPEG);
                Assert.Greater(stream2.Position, 0);

                Assert.IsTrue(Enumerable.SequenceEqual(stream1.ToArray(), stream2.ToArray()));
            }

            FreeImage.UnloadEx(ref dib);
        }

        [Test]
        public void ReadStreamBmp()
        {
            FIBITMAP dib = FreeImage.Allocate(1000, 800, 24, 0xFF0000, 0xFF00, 0xFF);
            Assert.IsFalse(dib.IsNull);

            using (MemoryStream stream = new MemoryStream())
            {
                FreeImage.IO = FreeImageStreamIO.IO;
                FreeImage.SaveToStream(dib, stream, FREE_IMAGE_FORMAT.FIF_BMP);
                Assert.Greater(stream.Position, 0);

                stream.Seek(0, SeekOrigin.Begin);

                FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_UNKNOWN;
                FIBITMAP dib2 = FreeImage.LoadFromStream(stream, ref format);
                Assert.IsFalse(dib2.IsNull);
                Assert.IsTrue(FreeImage.Compare(dib, dib2, FREE_IMAGE_COMPARE_FLAGS.COMPLETE));
                FreeImage.UnloadEx(ref dib2);
                Assert.IsTrue(dib2.IsNull);

                FreeImage.IO = SpanStreamIO.IO;
                stream.Seek(0, SeekOrigin.Begin);

                dib2 = FreeImage.LoadFromStream(stream, ref format);
                Assert.IsFalse(dib2.IsNull);
                Assert.IsTrue(FreeImage.Compare(dib, dib2, FREE_IMAGE_COMPARE_FLAGS.COMPLETE));
                FreeImage.UnloadEx(ref dib2);
                Assert.IsTrue(dib2.IsNull);
            }

            FreeImage.UnloadEx(ref dib);
        }

        [Test]
        public void ReadStreamJpeg()
        {
            FIBITMAP dib = FreeImage.Allocate(1000, 800, 24, 0xFF0000, 0xFF00, 0xFF);
            Assert.IsFalse(dib.IsNull);

            using (MemoryStream stream = new MemoryStream())
            {
                FreeImage.IO = FreeImageStreamIO.IO;
                FreeImage.SaveToStream(dib, stream, FREE_IMAGE_FORMAT.FIF_JPEG);
                Assert.Greater(stream.Position, 0);

                stream.Seek(0, SeekOrigin.Begin);

                FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_UNKNOWN;
                FIBITMAP dib2 = FreeImage.LoadFromStream(stream, ref format);
                Assert.IsFalse(dib2.IsNull);
                Assert.IsTrue(FreeImage.Compare(dib, dib2, FREE_IMAGE_COMPARE_FLAGS.COMPLETE));
                FreeImage.UnloadEx(ref dib2);
                Assert.IsTrue(dib2.IsNull);

                FreeImage.IO = SpanStreamIO.IO;
                stream.Seek(0, SeekOrigin.Begin);

                dib2 = FreeImage.LoadFromStream(stream, ref format);
                Assert.IsFalse(dib2.IsNull);
                Assert.IsTrue(FreeImage.Compare(dib, dib2, FREE_IMAGE_COMPARE_FLAGS.COMPLETE));
                FreeImage.UnloadEx(ref dib2);
                Assert.IsTrue(dib2.IsNull);
            }

            FreeImage.UnloadEx(ref dib);
        }

        private static unsafe void Write<T>(FIBITMAP dib, T value) where T : unmanaged
        {
            var ptr = FreeImage.GetBits(dib);
            var data = (T*)ptr;
            data[0] = value;
        }

        private static unsafe T Read<T>(FIBITMAP dib) where T : unmanaged
        {
            var ptr = FreeImage.GetBits(dib);
            var data = (T*)ptr;
            return data[0];
        }

        [Test]
        public void SaveAndLoadExr()
        {
            FIBITMAP image = FreeImage.AllocateT(FREE_IMAGE_TYPE.FIT_FLOAT, 1, 1, 32);
            const float value = 42.0f;

            try
            {
                Write(image, value);

                using var ms = new MemoryStream();
                var success = FreeImage.SaveToStream(image, ms, FREE_IMAGE_FORMAT.FIF_EXR, FREE_IMAGE_SAVE_FLAGS.EXR_FLOAT);
                Assert.IsTrue(success);

                ms.Seek(0, SeekOrigin.Begin);
                var loaded = FreeImage.LoadFromStream(ms);

                try
                {
                    var result = Read<float>(loaded);
                    Assert.AreEqual(value, result);
                }
                finally
                {
                    FreeImage.UnloadEx(ref loaded);
                }
            }
            finally
            {
                FreeImage.UnloadEx(ref image);
            }
        }

        [Test]
        public void SaveAndLoadWebp()
        {
            FIBITMAP image = FreeImage.AllocateT(FREE_IMAGE_TYPE.FIT_BITMAP, 1, 1, 24);
            const byte value = 42;

            try
            {
                Write(image, value);

                using var ms = new MemoryStream();
                var success = FreeImage.SaveToStream(image, ms, FREE_IMAGE_FORMAT.FIF_WEBP, FREE_IMAGE_SAVE_FLAGS.WEBP_LOSSLESS);
                Assert.IsTrue(success);

                ms.Seek(0, SeekOrigin.Begin);
                var loaded = FreeImage.LoadFromStream(ms);

                try
                {
                    var result = Read<byte>(loaded);
                    Assert.AreEqual(value, result);
                }
                finally
                {
                    FreeImage.UnloadEx(ref loaded);
                }
            }
            finally
            {
                FreeImage.UnloadEx(ref image);
            }
        }
    }
}
