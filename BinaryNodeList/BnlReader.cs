using System;
using System.IO;

namespace BinaryNodeList
{
    /// <summary>
    /// Reads nodes from a BNL stream.
    /// </summary>
    public class BnlReader : IDisposable
    {
        private BinaryReader reader;
        private bool keepOpen;
        private bool headRead = false;

        /// <summary>
        /// BNL version of this stream. (If read.)
        /// </summary>
        public int Version { get; private set; }
        /// <summary>
        /// Number of nodes this document has.
        /// </summary>
        public int Count { get; private set; } = 0;
        /// <summary>
        /// Number of nodes left to read.
        /// </summary>
        public int Left { get; private set; } = 1;

        /// <summary>
        /// Create a BNL stream reader.
        /// </summary>
        /// <param name="reader">Reader to read from.</param>
        /// <param name="keepOpen">Should the reader be kept open?</param>
        public BnlReader(BinaryReader reader, bool keepOpen = false)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.keepOpen = keepOpen;
        }

        private void ReadHeader()
        {
            if (headRead)
                return;

            int magic = reader.ReadInt32();

            if (magic != BnlConstants.MAGIC_WORD)
                throw new Exception("Source stream is not a Binary Node List stream.");

            int version = reader.ReadInt32();

            if (version > BnlConstants.MAX_VERSION || version < BnlConstants.MIN_VERSION)
                throw new Exception("Unsupported Binary Node List version.");

            Version = version;

            Count = Left = reader.ReadInt32();

            reader.ReadInt32(); // Read reserved bytes.

            headRead = true;
        }

        /// <summary>
        /// Read the next node from the stream.
        /// </summary>
        /// <returns>The next node.</returns>
        public BnlNode Next()
        {
            if (!headRead)
                ReadHeader();

            if (Left < 0)
                throw new Exception("No more nodes left to read.");

            BnlNode node = new BnlNode();
            node.Deserialize(reader);
            Left--;
            return node;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private bool isDisposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            if (disposing)
            {
                if (!keepOpen)
                    reader.Dispose();

            }

            isDisposed = true;
        }
    }
}
