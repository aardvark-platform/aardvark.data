using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace FreeImageAPI.IO
{
    /// <summary>
    /// Internal class wrapping stream io functions.
    /// </summary>
    /// <remarks>
    /// FreeImage can read files from a disk or a network drive but also allows the user to
    /// implement their own loading or saving functions to load them directly from an ftp or web
    /// server for example.
    /// <para/>
    /// In .NET streams are a common way to handle data. The <b>FreeImageStreamIO</b> class handles
    /// the loading and saving from and to streams. It implements the funtions FreeImage needs
    /// to load data from an an arbitrary source.
    /// <para/>
    /// The class is for internal use only.
    /// </remarks>
    public static class SpanStreamIO
    {
        private const int SharedArrayPoolMaxBufferSize = 1024 * 1024;

        /// <summary>
        /// <see cref="FreeImageAPI.IO.FreeImageIO"/> structure that can be used to read from streams via
        /// <see cref="FreeImageAPI.FreeImage.LoadFromHandle(FREE_IMAGE_FORMAT, ref FreeImageIO, fi_handle, FREE_IMAGE_LOAD_FLAGS)"/>.
        /// </summary>
        public static readonly FreeImageIO IO;

        /// <summary>
        /// Initializes a new instances which can be used to
        /// create a FreeImage compatible <see cref="FreeImageAPI.IO.FreeImageIO"/> structure.
        /// </summary>
        static SpanStreamIO()
        {
            IO.readProc = new ReadProc(streamRead);
            IO.writeProc = new WriteProc(streamWrite);
            IO.seekProc = new SeekProc(streamSeek);
            IO.tellProc = new TellProc(streamTell);
        }

        /// <summary>
        /// Reads the requested data from the stream and writes it to the given address.
        /// </summary>
        static unsafe uint streamRead(IntPtr buffer, uint size, uint count, fi_handle handle)
        {
            Stream stream = handle.GetObject() as Stream;
            if ((stream == null) || (!stream.CanRead))
            {
                return 0;
            }

            var arrayPool = ArrayPool<byte>.Shared;
            byte[] bufferTemp = arrayPool.Rent(
                size < SharedArrayPoolMaxBufferSize ? (int)size : SharedArrayPoolMaxBufferSize);

            int readSize = (int)Math.Min(bufferTemp.Length, size);

            int copyIterations = (int)(size / readSize);

            try
            {
                uint readCount = 0;
                int read;

                while (readCount < count * copyIterations)
                {
                    read = stream.Read(bufferTemp, 0, readSize);
                    if (read != readSize)
                    {
                        stream.Seek(-read, SeekOrigin.Current);
                        break;
                    }

                    Span<byte> source = new Span<byte>(bufferTemp, 0, read);
                    Span<byte> dest = new Span<byte>(buffer.ToPointer(), read);
                    source.CopyTo(dest);
                    buffer += read;

                    readCount++;
                }

                return (uint)readCount;
            }
            finally
            {
                arrayPool.Return(bufferTemp);
            }
        }

        /// <summary>
        /// Reads the given data and writes it into the stream.
        /// </summary>
        static unsafe uint streamWrite(IntPtr buffer, uint size, uint count, fi_handle handle)
        {
            Stream stream = handle.GetObject() as Stream;
            if ((stream == null) || (!stream.CanWrite))
            {
                return 0;
            }

            var arrayPool = ArrayPool<byte>.Shared;
            byte[] managedBuffer = arrayPool.Rent(
                size < SharedArrayPoolMaxBufferSize ? (int)size : SharedArrayPoolMaxBufferSize);

            int writeSize = (int)Math.Min(managedBuffer.Length, size);

            int copyIterations = (int)Math.Ceiling(size / (float)writeSize);

            byte* ptr = (byte*)buffer.ToPointer();
            uint writeCount = 0;

            int bytesWritten = 0;

            try {
                while (writeCount < count * copyIterations)  {
                    int actualWriteSize = Math.Min((int)(size - bytesWritten), writeSize);

                    ReadOnlySpan<byte> source = new ReadOnlySpan<byte>(ptr, actualWriteSize);
                    ptr += actualWriteSize;

                    source.CopyTo(managedBuffer);

                    stream.Write(managedBuffer, 0, actualWriteSize);

                    writeCount++;
                    bytesWritten += actualWriteSize;
                }

                return writeCount;
            } finally {
                arrayPool.Return(managedBuffer);
            }
        }

        /// <summary>
        /// Moves the streams position.
        /// </summary>
        static int streamSeek(fi_handle handle, int offset, SeekOrigin origin)
        {
            Stream stream = handle.GetObject() as Stream;
            if (stream == null)
            {
                return 1;
            }

            stream.Seek((long)offset, origin);
            return 0;
        }

        /// <summary>
        /// Returns the streams current position
        /// </summary>
        static int streamTell(fi_handle handle)
        {
            Stream stream = handle.GetObject() as Stream;
            if (stream == null)
            {
                return -1;
            }

            return (int)stream.Position;
        }
    }
}
