using DataTransfer.General;
using System;
using System.Net.Sockets;

namespace DataTransfer.src.Shared
{
    /// <summary>
    /// All of the possible header types being passed.
    /// </summary>
    public enum HeaderType
    {
        Disconnected = 0, FileSignal = 1, Chunk = 2, FileCancellationSignal = 3
    }

    /// <summary>
    /// Holds info about a received response.
    /// </summary>
    public struct ClientHeader
    {
        public string HeaderFileName { get; }
        public HeaderType @HeaderType { get; }

        public ClientHeader(HeaderType Type, string FileName)
        {
            HeaderType = Type;
            HeaderFileName = FileName;
        }
    }

    /// <summary>
    /// Holds info about a chunk received from the server about its fileName and its content. 
    /// </summary>
    public struct ServerChunk
    {
        public string HeaderFileName { get; }
        public byte[] Body { get; }

        public ServerChunk(byte[] FileName, byte[] Body)
        {
            HeaderFileName = Core.ASCII.GetString(FileName);
            this.Body = Body;
        }
    }

    /// <summary>
    /// Contains all of the network methods needed.
    /// </summary>
    public static class NetworkFunctions
    {
        /// <summary>
        /// Reads <paramref name="Stream"/>'s content until the length of the content that's being read gets equaled to <paramref name="Length"/>.
        /// </summary>
        /// <param name="Stream">Stream to read from.</param>
        /// <param name="Length"></param>
        /// <returns>A byte array with the length of <paramref name="Length"/></returns>
        public static byte[] ReadFor(NetworkStream Stream, int Length)
        {
            byte[] bytes = new byte[Length];
            int bytesRead = 0;
            for (; bytesRead < Length;)
            {
                bytesRead += Stream.Read(bytes, bytesRead, Length - bytesRead);
            }
            return bytes;
        }

        /// <summary>
        /// Reads the header type written to <paramref name="Stream"/> [The first byte written].
        /// </summary>
        /// <param name="Stream">Stream to read from.</param>
        public static HeaderType ReadHeaderType(NetworkStream Stream)
        {
            return (HeaderType)ReadFor(Stream, 1)[0];
        }

        /// <summary>
        /// Reads fileName written to <paramref name="Stream"/>. [Starting from index 1]
        /// </summary>
        /// <param name="Stream">Stream to read from.</param>
        /// <remarks>Header type should be read before this for correct indexing.</remarks>
        public static byte[] ReadFileName(NetworkStream Stream)
        {
            byte fileNameLen = ReadFor(Stream, 1)[0];
            return ReadFor(Stream, fileNameLen);
        }

        /// <summary>
        /// Writes a byte to <paramref name="Stream"/> with the value of <see cref="HeaderType.Disconnected"/>.
        /// </summary>
        /// <param name="Stream">Stream to read from.</param>
        public static void AnnounceDisconnection(NetworkStream Stream)
        {
            try
            {
                Stream.Write(new byte[1] { (byte)HeaderType.Disconnected }, 0, 1);
            }
            catch
            {
                ConsoleManager.WriteError("Shared.Functions.67 => [STREAM.CONNECTION]");
            }
        }
    }
}