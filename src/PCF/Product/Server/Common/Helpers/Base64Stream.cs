namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// An implementation of Stream that reads and writes base64 data. This is necessary because the .NET APIs 
    /// lack streaming features built in and require frequent large copies.
    /// </summary>
    public class Base64Stream : Stream
    {
        private const int StagingBufferSize = 768;

        // A temporary staging area for chars from windows. It is thread local so that we don't hit concurrency issues.
        private static readonly ThreadLocal<char[]> CharWriteBuffer = new ThreadLocal<char[]>(() => new char[2 * StagingBufferSize]);

        // We can either be in decode or encode mode, but not both.
        private readonly bool isDecodeMode;

        // Maintain a staging buffer of data. It's very important that the length of the buffer be divisible by 3.
        // This derives from the fact that 3 bytes translates to 4 base64 characters. By doing things in chunks of
        // 3, we can treat the base64 data as a stream.
        private readonly byte[] stagingBuffer = new byte[StagingBufferSize];

        // The current position within the staging buffer.
        private int stagingBufferIndex;

        // The current length of the staging buffer. The data may not consume the entire buffer.
        private int stagingBufferLength;

        // When in decode mode, the text we are decoding.
        private readonly string decodeText;

        // The offset within the decode text.
        private int decodeTextIndex;
        
        private readonly StringBuilder encodedData;

        /// <summary>
        /// Initializes a Base64Stream that reads from the given string.
        /// </summary>
        /// <param name="base64Text">The base64 text.</param>
        public Base64Stream(string base64Text)
        {
            this.isDecodeMode = true;
            this.decodeText = base64Text;
        }

        /// <summary>
        /// Initializes a Base64Stream that writes the to the <see cref="EncodedOutput"/> property.
        /// </summary>
        public Base64Stream()
        {
            this.isDecodeMode = false;
            this.encodedData = new StringBuilder();
        }

        /// <summary>
        /// The output of the write operation. When in read mode, this returns null.
        /// </summary>
        public StringBuilder EncodedOutput => this.encodedData;

        /// <summary>
        /// Indicates if this stream supports read operations.
        /// </summary>
        public override bool CanRead => this.isDecodeMode;

        /// <summary>
        /// Indicates if this stream supports interior seeks.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Indicates if this stream supports write operations.
        /// </summary>
        public override bool CanWrite => !this.isDecodeMode;

        /// <summary>
        /// Base64Stream does not support length operations.
        /// </summary>
        public override long Length => throw new NotSupportedException();

        /// <summary>
        /// Base64Stream does not support position setting.
        /// </summary>
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        
        /// <summary>
        /// Base64Stream does not support seeking.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Base64Stream does not support setting the length.
        /// </summary>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Flushes the contents of the stream. This is a no-op.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Closes the stream. When in write mode, this flushes any pending writes. The output is not fully formed until Close is called.
        /// </summary>
        public override void Close()
        { 
            if (this.CanWrite && this.stagingBufferIndex != 0)
            {
                this.WriteNextChunk();
            }
        }

        /// <summary>
        /// Reads into the given buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset within the buffer.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!this.CanRead)
            {
                throw new InvalidOperationException("This base64 stream is in write mode. Read operations are not supported.");
            }

            int bytesWritten = 0;
            
            while (count > 0 && (this.decodeTextIndex < this.decodeText.Length || this.stagingBufferIndex < this.stagingBufferLength))
            {
                if (this.stagingBufferIndex >= this.stagingBufferLength)
                {
                    // Refill the staging buffer if we've run past the end.
                    this.DecodeNextChunk();
                }

                // Figure out how many bytes from the staging buffer to copy to the output.
                int bytesToCopy = Math.Min(this.stagingBufferLength - this.stagingBufferIndex, count);
                Array.Copy(this.stagingBuffer, this.stagingBufferIndex, buffer, offset, bytesToCopy);

                count -= bytesToCopy;
                offset += bytesToCopy;
                bytesWritten += bytesToCopy;
                this.stagingBufferIndex += bytesToCopy;
            }

            return bytesWritten;
        }
        
        private unsafe void DecodeNextChunk()
        {
            int charsToRead = Math.Min(this.stagingBuffer.Length * 4 / 3, this.decodeText.Length - this.decodeTextIndex);

            fixed (char* pChar = this.decodeText)
            fixed (byte* pDecodeBuffer = &this.stagingBuffer[0])
            {
                uint decodeBufferLength = (uint)this.stagingBuffer.Length;
                
                // This is interesting. Chars in c# are unicode, so they are double-wide (2 byte) characters.
                // Intuitively, incrementing a pointer should move the pointer a fixed number of bytes.
                // However, in c#, incrementing a pointer by X moves it X * sizeof(thing) bytes.
                // So since chars are 2 bytes, adding 1 to a char* pointer actually advances it 2 bytes.
                // If you cast the char* to a byte* and add 1, the pointer only gets incremented by 1.
                char* readOffset = pChar + this.decodeTextIndex;

                if (!CryptStringToBinaryW(
                    readOffset,                // pointer in the string to read from.
                    (uint)charsToRead,         // characters ro read from the string.
                    0x1,                       // flag indicating base64 input.
                    pDecodeBuffer,             // buffer to write to
                    ref decodeBufferLength,    // InOut. In: Length of the buffer, Out: Number of bytes written to the buffer
                    out _,
                    out _))
                {
                    int lastError = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException("Base64 Decode error: " + lastError.ToString());
                }

                this.stagingBufferLength = (int)decodeBufferLength;
                this.stagingBufferIndex = 0;
                this.decodeTextIndex += charsToRead;
            }
        }

        /// <summary>
        /// Writes the given bytes to the stream.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!this.CanWrite)
            {
                throw new InvalidOperationException("This base64 stream is in read mode. Write operations are not supported.");
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count exceeds the length of the buffer.");
            }

            while (count > 0)
            {
                // Copy from the buffer they gave us into our staging buffer.
                int bytesAvailable = this.stagingBuffer.Length - this.stagingBufferIndex;
                int bytesToCopy = Math.Min(bytesAvailable, count);

                Array.Copy(buffer, offset, this.stagingBuffer, this.stagingBufferIndex, bytesToCopy);

                this.stagingBufferIndex += bytesToCopy;
                offset += bytesToCopy;
                count -= bytesToCopy;
                
                // If the staging buffer is full, then flush it to then flush it to the underlying string builder.
                if (this.stagingBufferIndex == this.stagingBuffer.Length)
                {
                    // Time to flush.
                    this.WriteNextChunk();
                    this.stagingBufferIndex = 0;
                }
            }
        }

        private unsafe void WriteNextChunk()
        {
            char[] destination = CharWriteBuffer.Value;

            fixed (char* pChar = destination)
            fixed (byte* pByte = &this.stagingBuffer[0])
            {
                uint charsWritten = (uint)destination.Length;

                bool result = CryptBinaryToStringW(
                    pByte,                              // The byte array to read from
                    (uint)this.stagingBufferIndex,      // The length of the byte array. This is the last index we wrote to.
                    0x40000000 | 0x1,                   // no CRLF | b64
                    pChar,                              // Character array to write to.
                    ref charsWritten);                  // InOut. In: Length of the character buffer, Out: Number of characters written.

                if (!result)                  
                {
                    int lastError = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException("Base64 Encode error: " + lastError.ToString());
                }

                this.encodedData.Append(pChar, (int)charsWritten);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
        [DllImport("crypt32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe extern bool CryptStringToBinaryW(
            char* pszString,
            uint cchString,
            uint dwFlags,
            byte* pbBinary,
            ref uint pcbBinary,
            out uint pdwSkip,
            out uint pdwFlags);
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
        [DllImport("crypt32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe extern bool CryptBinaryToStringW(
            byte* pbBinary,
            uint cbBinary,
            uint dwFlags,
            char* pszString,
            ref uint pcchString);
    }
}