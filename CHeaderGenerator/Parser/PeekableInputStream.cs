using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHeaderGenerator.Parser
{
    class PeekableInputStream : Stream
    {
        private readonly Stream innerStream;
        private readonly Stack<int> peekBytes = new Stack<int>();

        public PeekableInputStream(Stream innerStream)
        {
            this.innerStream = innerStream;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            return;
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.innerStream.Read(buffer, offset, count); b
        }

        public int Peek()
        {
            if (this.peekBytes.Count > 0)
                return this.peekBytes.Peek();

            var next = this.innerStream.ReadByte();
            this.peekBytes.Push(next);
            return next;
        }

        public int[] Peek(int count)
        {
            while (this.peekBytes.Count < count)
            {
                var next = this.innerStream.ReadByte();
                this.peekBytes.Push(next);
            }

            var currentPeekBytes = this.peekBytes.ToArray();
            Array.Reverse(currentPeekBytes);

            var peekArray = new int[count];
            Array.Copy(currentPeekBytes, peekArray, count);

            return peekArray;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
