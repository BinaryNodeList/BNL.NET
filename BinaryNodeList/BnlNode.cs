using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BinaryNodeList
{
    /// <summary>
    /// Storage class for a Binary Node List Node.
    /// </summary>
    public class BnlNode : IDisposable
    {
        private IntPtr buffer = IntPtr.Zero;

        /// <summary>
        /// Name of the node.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Array index of the node.
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// Type of the node.
        /// </summary>
        public BnlNodeType Type { get; set; }
        /// <summary>
        /// Size of the internal buffer in bytes.
        /// </summary>
        public int Size { get; private set; } = 0;

        /// <summary>
        /// Size of the internal buffer in shorts (words).
        /// </summary>
        public int SizeInShorts => Size / sizeof(short);
        /// <summary>
        /// Size of the internal buffer in ints (double words).
        /// </summary>
        public int SizeInInts => Size / sizeof(int);
        /// <summary>
        /// Size of the internal buffer in longs (quadruple words).
        /// </summary>
        public int SizeInLongs => Size / sizeof(long);

        /// <summary>
        /// Copy contents of the internal buffer to other unmanaged memory.
        /// </summary>
        /// <param name="destination">Pointer to destination.</param>
        /// <param name="srcOffset">Offset into internal buffer.</param>
        /// <param name="count">Number of bytes to copy.</param>
        public void ReadRaw(IntPtr destination, int srcOffset, int count)
        {
            CheckInternalIndex(srcOffset, count);

            unsafe
            {
                Buffer.MemoryCopy((buffer + srcOffset).ToPointer(), destination.ToPointer(), count, count);
            }
        }

        /// <summary>
        /// Copy contents of an unmanaged memory block to the internal buffer.
        /// </summary>
        /// <param name="source">Buffer to copy from.</param>
        /// <param name="dstOffset">Offset into internal buffer.</param>
        /// <param name="count">Number of bytes to copy.</param>
        /// <param name="expanding">Wether the write operation can expand the buffer or not.</param>
        public void WriteRaw(IntPtr source, int dstOffset, int count, bool expanding = false)
        {
            if (expanding)
            {
                CheckInsertIndex(dstOffset, count);

                if (dstOffset + count > Size)
                {
                    Realloc(dstOffset + count);
                }
            }
            else
                CheckInternalIndex(dstOffset, count);

            unsafe
            {
                Buffer.MemoryCopy(source.ToPointer(), (buffer + dstOffset).ToPointer(), count, count);
            }
        }

        /// <summary>
        /// Insert contents of an unmanaged memory block to the internal buffer.
        /// </summary>
        /// <param name="source">Buffer to copy from.</param>
        /// <param name="dstOffset">Offset into internal buffer.</param>
        /// <param name="count">Number of bytes to copy.</param>
        public void InsertRaw(IntPtr source, int dstOffset, int count)
        {
            CheckInsertIndex(dstOffset, count);

            // Calculate how many bytes need to be moved out.
            int movesize = Size - dstOffset;
            // Reallocate buffer with room for the inserted bytes.
            Realloc(Size + count);

            unsafe
            {
                // Copy bytes out of the insert area.
                Buffer.MemoryCopy((buffer + dstOffset).ToPointer(), (buffer + dstOffset + count).ToPointer(), movesize, movesize);
                // Insert bytes.
                Buffer.MemoryCopy(source.ToPointer(), (buffer + dstOffset).ToPointer(), count, count);
            }
        }

        /// <summary>
        /// Trim (delete) a section of the internal buffer.
        /// </summary>
        /// <param name="offset">Offset into internal buffer.</param>
        /// <param name="count">Number of bytes to trim.</param>
        public void TrimRaw(int offset, int count)
        {
            CheckInternalIndex(offset, count);

            int movesize = Size - offset - count;

            unsafe
            {
                Buffer.MemoryCopy((buffer + offset + count).ToPointer(), (buffer + offset).ToPointer(), movesize, movesize);
            }

            Realloc(Size - count);
        }

        /// <summary>
        /// Read a null terminated string from the internal buffer.
        /// </summary>
        /// <param name="srcOffset">Offset into internal buffer.</param>
        /// <param name="count">The length of the string in bytes (does not include null).</param>
        /// <returns>The read string.</returns>
        public string ReadStringNull(int srcOffset, out int count)
        {
            count = 0;
            for (int i = srcOffset; i < Size && Marshal.ReadByte(buffer + i) != 0; i++, count++)
            {
                // strlen
            }

            return ReadString(srcOffset, count);
        }
        /// <summary>
        /// Read a string from the internal buffer.
        /// </summary>
        /// <param name="srcOffset">Offset into internal buffer.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>The read string.</returns>
        public string ReadString(int srcOffset, int count)
        {
            CheckInternalIndex(srcOffset, count); // Call this early to reduce GC pressure.

            byte[] rawBytes = new byte[count];
            Read(rawBytes, 0, srcOffset, count);
            return Encoding.UTF8.GetString(rawBytes);
        }

        /// <summary>
        /// Copy contents of the internal buffer to an array.
        /// </summary>
        /// <typeparam name="T">Destination array type.</typeparam>
        /// <param name="destination">Destination array.</param>
        /// <param name="dstOffset">Array index of the destination array to start copying to.</param>
        /// <param name="srcOffset">Number of array elements to skip in the internal buffer.</param>
        /// <param name="count">Number of array elements to copy.</param>
        public void Read<T>(T[] destination, int dstOffset, int srcOffset, int count) where T : struct
        {
            int sz = Marshal.SizeOf<T>();
            int bytes = count * sz;

            CheckExternalIndex(destination.Length, dstOffset, count);

            var handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            try
            {
                ReadRaw(handle.AddrOfPinnedObject() + (dstOffset * sz), srcOffset * sz, bytes);
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// Copy contents of the internal buffer to an array.
        /// </summary>
        /// <typeparam name="T">Destination array type.</typeparam>
        /// <param name="destination">Destination array.</param>
        /// <param name="dstOffset">Array index of the destination array to start copying to.</param>
        /// <param name="srcOffset">Number of bytes to skip in the internal buffer.</param>
        /// <param name="count">Number of array elements to copy.</param>
        public void Read<T>(T[] destination, int dstOffset, IntPtr srcOffset, int count) where T : struct
        {
            int sz = Marshal.SizeOf<T>();
            int bytes = count * sz;

            CheckExternalIndex(destination.Length, dstOffset, count);

            var handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            try
            {
                ReadRaw(handle.AddrOfPinnedObject() + (dstOffset * sz), (int)srcOffset, bytes);
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// Copy contents of the source array into the internal buffer.
        /// </summary>
        /// <typeparam name="T">Source array type.</typeparam>
        /// <param name="source">The array to copy from.</param>
        /// <param name="srcOffset">Array index from source array to begin copying from.</param>
        /// <param name="dstOffset">Number of array indices to skip in the internal buffer.</param>
        /// <param name="count">Number of array indices to copy.</param>
        /// <param name="expanding">Wether the write operation can expand the internal buffer or not.</param>
        public void Write<T>(T[] source, int srcOffset, int dstOffset, int count, bool expanding = false)
        {
            int sz = Marshal.SizeOf<T>();
            int bytes = count * sz;

            CheckExternalIndex(source.Length, srcOffset, count);

            var handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try
            {
                WriteRaw(handle.AddrOfPinnedObject() + (srcOffset * sz), dstOffset * sz, bytes, expanding);
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// Copy contents of the source array into the internal buffer.
        /// </summary>
        /// <typeparam name="T">Source array type.</typeparam>
        /// <param name="source">The array to copy from.</param>
        /// <param name="srcOffset">Array index from source array to begin copying from.</param>
        /// <param name="dstOffset">Number of bytes to skip in the internal buffer.</param>
        /// <param name="count">Number of array indices to copy.</param>
        /// <param name="expanding">Wether the write operation can expand the internal buffer or not.</param>
        public void Write<T>(T[] source, int srcOffset, IntPtr dstOffset, int count, bool expanding = false)
        {
            int sz = Marshal.SizeOf<T>();
            int bytes = count * sz;

            CheckExternalIndex(source.Length, srcOffset, count);

            var handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try
            {
                WriteRaw(handle.AddrOfPinnedObject() + (srcOffset * sz), (int)dstOffset, bytes, expanding);
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// Insert bytes into the internal buffer from an array.
        /// </summary>
        /// <typeparam name="T">The source array type.</typeparam>
        /// <param name="source">Source array.</param>
        /// <param name="srcOffset">Array index of source array to copy from.</param>
        /// <param name="dstOffset">Array index of the internal buffer to insert into.</param>
        /// <param name="count">Number of array indices to insert.</param>
        public void Insert<T>(T[] source, int srcOffset, int dstOffset, int count) where T : struct
        {
            int sz = Marshal.SizeOf<T>();
            int bytes = count * sz;

            CheckExternalIndex(source.Length, srcOffset, count);

            var handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try
            {
                InsertRaw(handle.AddrOfPinnedObject() + (srcOffset * sz), dstOffset * sz, bytes);
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// Insert bytes into the internal buffer from an array.
        /// </summary>
        /// <typeparam name="T">The source array type.</typeparam>
        /// <param name="source">Source array.</param>
        /// <param name="srcOffset">Array index of source array to copy from.</param>
        /// <param name="dstOffset">Offset int the internal buffer to insert into.</param>
        /// <param name="count">Number of array indices to insert.</param>
        public void Insert<T>(T[] source, int srcOffset, IntPtr dstOffset, int count) where T : struct
        {
            int sz = Marshal.SizeOf<T>();
            int bytes = count * sz;

            CheckExternalIndex(source.Length, srcOffset, count);

            var handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try
            {
                InsertRaw(handle.AddrOfPinnedObject() + (srcOffset * sz), (int)dstOffset, bytes);
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// Trim (delete) indices from the internal buffer.
        /// </summary>
        /// <typeparam name="T">The type the internal buffer is treated as.</typeparam>
        /// <param name="offset">Offset into the internal buffer as if it where a <typeparamref name="T"/> array.</param>
        /// <param name="count">Number of indices to delete as if the internal buffer was a <typeparamref name="T"/> array.</param>
        public void Trim<T>(int offset, int count) where T : struct
        {
            int sz = Marshal.SizeOf<T>();

            TrimRaw(offset * sz, count * sz);
        }

        /// <summary>
        /// Trim (delete) indices from the internal buffer.
        /// </summary>
        /// <typeparam name="T">The type the internal buffer is treated as.</typeparam>
        /// <param name="offset">Byte offset in the internal buffer.</param>
        /// <param name="count">Number of indices to delete as if the internal buffer was a <typeparamref name="T"/> array.</param>
        public void Trim<T>(IntPtr offset, int count) where T : struct
        {
            TrimRaw((int)offset, count * Marshal.SizeOf<T>());
        }

        /// <summary>
        /// Copies internal contents to an array.
        /// </summary>
        /// <typeparam name="T">The type the internal buffer is treated as.</typeparam>
        /// <returns>A newly allocated array containing a copy of the buffer.</returns>
        public T[] ToArray<T>() where T : struct
        {
            T[] ts = new T[Size / Marshal.SizeOf<T>()];

            Read(ts, 0, 0, ts.Length);

            return ts;
        }

        /// <summary>
        /// Copies internal contents to a managed string.
        /// </summary>
        /// <returns>The string copied from the internal buffer.</returns>
        public override string ToString()
        {
            return ReadString(0, Size);
        }

        /// <summary>
        /// Exposes the internal buffer as a stream.
        /// </summary>
        /// <returns>The internal buffer as a stream.</returns>
        /// <remarks>
        /// Users of this function should not dispose the internal buffer. This
        /// may lead to undefined behavior.
        /// </remarks>
        public unsafe UnmanagedMemoryStream ToStream()
        {
            return new UnmanagedMemoryStream((byte*)buffer.ToPointer(), Size);
        }

        public void Deconstruct<T>(out int index, out string name, out T[] array) where T : struct
        {
            index = Index;
            name = Name;
            array = ToArray<T>();
        }

        public void Deconstruct(out int index, out string name, out string contents)
        {
            index = Index;
            name = Name;
            contents = ToString();
        }

        /// <summary>
        /// Serialize the node into a stream.
        /// </summary>
        /// <param name="writer">Writer to serialize into.</param>
        internal void Serialize(BinaryWriter writer)
        {
            void Align16(BinaryWriter w)
            {
                int count = 16 - (int)(w.BaseStream.Position % 16);

                for (int i = 0; i < count; i++)
                    w.Write((byte)0);
            }

            var keyBytes = Encoding.UTF8.GetBytes(Name);

            writer.Write((int)Type);
            writer.Write(Index);
            writer.Write(keyBytes.Length);
            writer.Write(Size);

            writer.Write(keyBytes);
            Align16(writer);

            byte[] block = new byte[1024];
            for (int i = 0; i < Size; i += 1024)
            {
                int sz = Math.Min(Size - i, 1024);
                Read(block, 0, i, sz);
                writer.Write(block, 0, sz);
            }

            Align16(writer);
        }

        /// <summary>
        /// Deserialize the node from a stream.
        /// </summary>
        /// <param name="reader">Reader to deserialize from.</param>
        internal void Deserialize(BinaryReader reader)
        {
            void Align16(BinaryReader r)
            {
                int count = 16 - (int)(r.BaseStream.Position % 16);
                reader.ReadBytes(count);
            }

            int nameLength;

            Type = (BnlNodeType)reader.ReadInt32();
            Index = reader.ReadInt32();
            nameLength = reader.ReadInt32();
            Realloc(reader.ReadInt32()); // Reads size;

            Name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));
            Align16(reader);

            byte[] block = new byte[1024];
            for (int i = 0; i < Size; i += 1024)
            {
                int sz = Math.Min(Size - i, 1024);
                reader.Read(block, 0, sz);
                Write(block, 0, i, sz, false);
            }
            Align16(reader);
        }

        private static void CheckExternalIndex(int lenght, int index, int count)
        {
            /**/ if (count < 0)
                throw new ArgumentOutOfRangeException("Count cannot be negative");
            else if (index < 0 || index >= lenght)
                throw new IndexOutOfRangeException("Input array index out of range.");
            else if (index + count > lenght)
                throw new ArgumentOutOfRangeException("Attempt to overflow destination array.");
        }

        private void CheckInternalIndex(int index, int count)
        {
            /**/ if (count < 0)
                throw new ArgumentOutOfRangeException("Count cannot be negative.");
            else if (index < 0 || index >= Size)
                throw new IndexOutOfRangeException("Internal buffer index out of range.");
            else if (index + count > Size)
                throw new ArgumentOutOfRangeException("Attempt to read post-buffer data.");
        }

        private void CheckInsertIndex(int index, int count)
        {
            /**/ if (count < 0)
                throw new ArgumentOutOfRangeException("Count cannot be negative.");
            else if (index < 0 || index > Size)
                throw new ArgumentOutOfRangeException("Attemp to insert far from the buffer");
        }

        private void Realloc(int newsize)
        {
            Size = newsize;
            if (buffer == IntPtr.Zero)
            {
                buffer = Marshal.AllocHGlobal(newsize);
            }
            else
            {
                buffer = Marshal.ReAllocHGlobal(buffer, (IntPtr)newsize);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                Marshal.FreeHGlobal(buffer);

                disposedValue = true;
            }
        }

        ~BnlNode()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
