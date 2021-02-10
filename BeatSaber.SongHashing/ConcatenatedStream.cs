using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("HashingTests")]
namespace BeatSaber.SongHashing
{
    /// <summary>
    /// A collection of Streams treated as one.
    /// </summary>
    internal class ConcatenatedStream : Stream
    {
        private readonly Queue<Stream> _streams;

        /// <summary>
        /// Creates an empty <see cref="ConcatenatedStream"/>.
        /// </summary>
        public ConcatenatedStream()
        {
            _streams = new Queue<Stream>();
        }

        public int StreamCount => _streams.Count;

        /// <summary>
        /// Creates a new <see cref="ConcatenatedStream"/> from a collection of Streams.
        /// </summary>
        /// <param name="initialSize"></param>
        public ConcatenatedStream(int initialSize)
        {
            _streams = new Queue<Stream>(initialSize);
        }

        public void Append(Stream stream)
        {
            _streams.Enqueue(stream ?? throw new ArgumentNullException(nameof(stream)));
        }

        /// <inheritdoc/>
        public override bool CanRead => _streams.All(s => s.CanRead);

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => _streams.Select(s => s.Length).Aggregate((a, b) => a + b);

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;
            if (_streams.Count == 0)
                return 0;
            Stream current = _streams.Peek();
            while (count > 0)
            {
                int bytesRead = current.Read(buffer, offset, count);
                if (bytesRead == 0)
                {
                    current.Dispose();
                    _streams.Dequeue();
                    if (_streams.Count == 0)
                        return totalBytesRead;
                    current = _streams.Peek();
                    continue;
                }

                totalBytesRead += bytesRead;
                offset += bytesRead;
                count -= bytesRead;
            }

            return totalBytesRead;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                while(_streams.Count > 0)
                {
                    _streams.Dequeue().Dispose();
                }
            }
        }
    }
}
