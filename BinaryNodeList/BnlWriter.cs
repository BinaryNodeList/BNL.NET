using System;
using System.IO;

namespace BinaryNodeList
{
    /// <summary>
    /// Write a Binary Node List to a stream.
    /// </summary>
    public class BnlWriter : IDisposable
    {
        private bool keepOpen;
        private BinaryWriter writer;
        private long headIndex = -1;

        /// <summary>
        /// Number of nodes currently written.
        /// </summary>
        public int Count { get; private set; } = 0;

        /// <summary>
        /// Create a BNL stream writer.
        /// </summary>
        /// <param name="writer">Writer to write to.</param>
        /// <param name="keepOpen">Should the writer be kept open?</param>
        public BnlWriter(BinaryWriter writer, bool keepOpen = false)
        {
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
            this.keepOpen = keepOpen;
        }

        /// <summary>
        /// Write the next BNL node.
        /// </summary>
        /// <param name="node">Node to write.</param>
        public void Next(BnlNode node)
        {
            if (headIndex == -1)
            {
                headIndex = writer.BaseStream.Position;
                writer.Seek(16, SeekOrigin.Current);
            }

            if (node is null)
                throw new ArgumentNullException(nameof(node));

            node.Serialize(writer);
            Count++;
        }

        private void WriteHead()
        {
            writer.Write(BnlConstants.MAGIC_WORD);
            writer.Write(BnlConstants.MIN_VERSION);
            writer.Write(Count);
            writer.Write((int)0);
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
                long endPosition = writer.BaseStream.Position;
                writer.BaseStream.Seek(headIndex, SeekOrigin.Begin);

                WriteHead();

                writer.BaseStream.Seek(endPosition, SeekOrigin.Begin);

                if (!keepOpen)
                    writer.Dispose();

            }

            isDisposed = true;
        }
    }
}
