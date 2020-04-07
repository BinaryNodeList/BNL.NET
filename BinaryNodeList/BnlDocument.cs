using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BinaryNodeList
{
    /// <summary>
    /// A convenient class that store BNL nodes for use and serialization.
    /// </summary>
    public class BnlDocument : IDisposable, IEnumerable<BnlNode>
    {
        private List<BnlNode> nodes = new List<BnlNode>();

        /// <summary>
        /// Number of nodes this document has.
        /// </summary>
        public int Count => nodes.Count;

        public BnlNode this[string name, int index]
        {
            get
            {
                return nodes.Single(x => x.Index == index && x.Name == name);
            }
        }

        /// <summary>
        /// Checks existance of a node in the document.
        /// </summary>
        /// <param name="name">Name of the node.</param>
        /// <param name="index">Index of the node.</param>
        /// <returns>Boolean indicating the existance of a node.</returns>
        public bool Exists(string name, int index)
        {
            return nodes.Exists(x => x.Index == index && x.Name == name);
        }

        /// <summary>
        /// Add a node to the document.
        /// </summary>
        /// <param name="node">The node to add.</param>
        /// <exception cref="Exception">Thrown when a node with the same identifieer exists.</exception>
        public void Add(BnlNode node)
        {
            if (Exists(node.Name, node.Index))
                throw new Exception("A node with the same name already exists in the document.");

            nodes.Add(node);
        }

        /// <summary>
        /// Add a range of nodes to the 
        /// </summary>
        /// <param name="nodes">The nodes to add.</param>
        /// <exception cref="Exception">Any one of the nodes to be added has the same identifier as a node already in the document.</exception>
        public void AddRange(IEnumerable<BnlNode> nodes)
        {
            if (this.nodes.Any(i => nodes.Any(e => i.Index == e.Index && i.Name == e.Name)))
                throw new Exception("Range contains a node with an identifier that already exists in the document.");
            
            foreach(var node in nodes)
            {
                this.nodes.Add(node);
            }
        }

        /// <summary>
        /// Remove a node from the document.
        /// </summary>
        /// <param name="name">Name of the node.</param>
        /// <param name="index">Index of the node.</param>
        /// <param name="dispose">Should the removed node be disposed?</param>
        public void Remove(string name, int index, bool dispose = true)
        {
            var i = nodes.FindIndex(x => x.Index == index && x.Name == name);
            if (dispose)
                nodes[i].Dispose();
            nodes.RemoveAt(i);
        }

        /// <summary>
        /// Remove all nodes matching a condition in the document.
        /// </summary>
        /// <param name="matcher">A function to match nodes.</param>
        /// <param name="dispose">Should the removed node be disposed?</param>
        public void Remove(Predicate<BnlNode> matcher, bool dispose = true)
        {
            nodes.ForEach
            (
                x =>
                {
                    if (matcher(x))
                    {
                        if (dispose)
                            x.Dispose();
                        nodes.Remove(x);
                    }
                }
            );
        }

        /// <summary>
        /// Clear this document.
        /// </summary>
        /// <param name="dispose">Should the nodes be disposed?</param>
        public void Clear(bool dispose = true)
        {
            if (dispose)
                nodes.ForEach(x => x.Dispose());
            nodes.Clear();
        }

        /// <summary>
        /// Serialize this document.
        /// </summary>
        /// <param name="writer">Writer to serialize to.</param>
        /// <param name="keepOpen">Should the writer be disposed?</param>
        public void Serialize(BinaryWriter writer, bool keepOpen = false)
        {
            using (var swriter = new BnlWriter(writer, keepOpen))
            {
                foreach (var node in nodes)
                {
                    swriter.Next(node);
                }
            }
        }

        /// <summary>
        /// Deserialize a file into this document.
        /// </summary>
        /// <param name="reader">Reader to deserialize from.</param>
        /// <param name="keepOpen">Should the reader be disposed?</param>
        public void Deserialize(BinaryReader reader, bool keepOpen = false)
        {
            Clear();

            using (var sreader = new BnlReader(reader, keepOpen))
            {
                while (sreader.Left > 0)
                {
                    nodes.Add(sreader.Next());
                }
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
                    foreach (var node in nodes)
                    {
                        node.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

        public IEnumerator<BnlNode> GetEnumerator() => nodes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => (nodes as IEnumerable).GetEnumerator();

    }
}
