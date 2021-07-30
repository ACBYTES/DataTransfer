using DataTransfer.General;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DataTransfer.src.Shared;

using static DataTransfer.General.ConsoleGraphics<DataTransfer.General.ConsoleShape2D>;
using static DataTransfer.General.ConsoleManager;
using static DataTransfer.src.FileHandler;

namespace DataTransfer.src
{
    public static class Client
    {
        /// <summary>
        /// Message that should be shown when the server sends a cancellation signal.
        /// </summary>
        public const string SERVER_FILE_CANCELLATION_SIGNAL_REASON = "Server cancelled the operation.";

        /// <summary>
        /// Event that notifies all subscribers[<see cref="ClientFile"/>s] of a newly received chunk.
        /// </summary>
        public static event Core.F_OneParam<ServerChunk> OnChunkReceived;

        /// <summary>
        /// All of the currently active files being received and processed.
        /// </summary>
        private static List<ClientFile> activeFiles;

        /// <summary>
        /// Active instance of <see cref="TcpClient"/> that's connected to the server.
        /// </summary>
        private static TcpClient client;

        /// <summary>
        /// Active stream that's used to write to the server.
        /// </summary>
        private static NetworkStream clientStream;

        /// <summary>
        /// Server's endpoint.
        /// </summary>
        private static IPEndPoint serverEndPoint;

        /// <summary>
        /// Client's endpoint.
        /// </summary>
        private static IPEndPoint clientEndPoint;

        private static Mutex m_On_Operation_Done;
        private static Mutex m_Write_Lock;
        private static CancellationTokenSource cts_Read;

        /// <summary>
        /// Current file that's being processed and <see cref="WriteToStream(byte[], string)"/>'s thread is waiting for to get done with all of its processings to release <see cref="m_Write_Lock"/>.
        /// </summary>
        private static string activeProcessingFileName;

        /// <summary>
        /// <see cref="activeProcessingFileName"/>'s processing state showing whether its processings are done yet, or not to let <see cref="WriteToStream(byte[], string)"/>'s active thread release <see cref="m_Write_Lock"/>.
        /// </summary>
        private static bool activeFileNameProcessingDone = false;

        /// <summary>
        /// Returns a string containing client's current conditions, files, etc...
        /// </summary>
        public static string GetStatus()
        {
            return $"- Client is up and running on ({clientEndPoint.Address}:{clientEndPoint.Port}).\n\n- Client is connected to ({serverEndPoint.Address}:{serverEndPoint.Port})\n\n- " +
                $"Active files being received: [{(activeFiles == null ? "No active files are being sent." : activeFiles.ToString(", ", true))}]";
        }

        /// <summary>
        /// Sets up client for a new connection to a server.
        /// </summary>
        public static void SetUpClient(IPEndPoint ClientEndPoint, IPEndPoint ServerEndPoint)
        {
            try
            {
                activeFiles = new List<ClientFile>();
                client = new TcpClient(ClientEndPoint);
                client.ConnectAsync(ServerEndPoint.Address, ServerEndPoint.Port);
                Core.Threads.SleepUntil(() => { return client.Connected; });
                if (client.Connected)
                {
                    clientStream = client.GetStream();
                    WriteShape(ConsoleColor.Blue, ConsoleShapes.Rectangle, $"Started Client on the Requested Address and Got Connected to the Server.\n[{ClientEndPoint.Address}:{ClientEndPoint.Port}]");
                    serverEndPoint = ServerEndPoint;
                    clientEndPoint = ClientEndPoint;
                    cts_Read = new CancellationTokenSource();
                    m_On_Operation_Done = new Mutex();
                    m_Write_Lock = new Mutex();
                    Task.Factory.StartNew(ReadStream, cts_Read.Token);
                }
                else
                {
                    Reset();
                    WriteShape(ConsoleColor.Yellow, ConsoleShapes.Rectangle, "[Connection Timeout] Server didn't respond. Resetting client.");
                }
            }
            catch (Exception Exc)
            {
                Reset();
                WriteShape(ConsoleColors.ErrorColor, ConsoleShapes.Rectangle, Exc.ToStr());
            }
        }

        /// <summary>
        /// Resets client and everything related to its connection.
        /// </summary>
        public static void Reset()
        {
            if (clientStream != null && clientStream.CanWrite)
                NetworkFunctions.AnnounceDisconnection(clientStream);
            if (activeFiles != null)
            {
                foreach (var item in activeFiles)
                {
                    item.Dispose();
                }
                activeFiles.Clear();
            }
            try
            {
                if (clientStream != null)
                {
                    clientStream.Flush();
                    clientStream.Dispose();
                }
            }
            catch { }
            try
            {
                if (client != null)
                {
                    client.Dispose();
                }
            }
            catch { }
            try
            {
                if (cts_Read != null)
                    cts_Read.Cancel();
            }
            catch { }
            try
            {
                m_Write_Lock.Dispose();
            }
            catch { }
            try
            {
                m_On_Operation_Done.Dispose();
            }
            catch { }
            client = null;
            activeFiles = null;
            cts_Read = null;
            serverEndPoint = null;
            clientEndPoint = null;
            Core.ResetUserMode();
            WriteShape(ConsoleColor.Green, ConsoleShapes.Rectangle, "Client reset was successful!");
        }

        /// <summary>
        /// Calls <see cref="ClientFile.Cancel(string)"/> on a possible <see cref="ClientFile"/> in <see cref="activeFiles"/>.
        /// </summary>
        public static void CancelFileOperation(string FileName, string CancellationReason = USER_FILE_CANCELLATION_MESSAGE)
        {
            lock (activeFiles)
            {
                foreach (var item in activeFiles)
                {
                    if (item.GetFileName() == FileName)
                    {
                        item.Cancel(CancellationReason, false);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Calls <see cref="ClientFile.Cancel(string)"/> on a possible <see cref="ClientFile"/> in <see cref="activeFiles"/>.
        /// </summary>
        public static void CancelFileOperation(int Index, string CancellationReason = USER_FILE_CANCELLATION_MESSAGE)
        {
            if (activeFiles.Count - 1 < Index)
            {
                WriteShape(ConsoleColors.FileColor, ConsoleShapes.Rectangle, "Index was out of range.");
                return;
            }
            activeFiles[Index].Cancel(CancellationReason);
        }

        /// <summary>
        /// Writes the data passed to <see cref="clientStream"/>.
        /// </summary>
        /// <param name="Bytes">Data to write.</param>
        /// <param name="FileName">FileName that's requesting this write. [Needed to update <see cref="activeProcessingFileName"/>]</param>
        public static async void WriteToStream(byte[] Bytes, string FileName)
        {
            if (clientStream != null)
            {
                try
                {
                    await clientStream.WriteAsync(Bytes.AsMemory(0, Bytes.Length));
                }
                catch (Exception Exc)
                {
                    WriteShape(ConsoleColors.ErrorColor, ConsoleShapes.Rectangle, Exc.ToStr());
                    Reset();
                }
            }
            else
            {
                WriteError("[Couldn't communicate with the server.] Client.129 => NULL || !CW");
                Reset();
            }
        }

        /// <summary>
        /// Reads data from <see cref="clientStream"/>.
        /// </summary>
        public static void ReadStream()
        {
            while (!cts_Read.IsCancellationRequested)
            {
                HeaderType headerType = NetworkFunctions.ReadHeaderType(clientStream);
                if (headerType == HeaderType.Disconnected)
                {
                    WriteShape(ConsoleColor.Yellow, ConsoleShapes.Rectangle, $"{serverEndPoint.Address}:{serverEndPoint.Port} got disconnected! Resetting client.");
                    Reset();
                    break;
                }
                else if (headerType == HeaderType.FileCancellationSignal)
                {
                    CancelFileOperation(Core.ASCII.GetString(NetworkFunctions.ReadFileName(clientStream)), SERVER_FILE_CANCELLATION_SIGNAL_REASON);
                }
                else if (headerType == HeaderType.FileSignal)
                {
                    byte[] fileName = NetworkFunctions.ReadFileName(clientStream);
                    long fileSize = BitConverter.ToInt64(NetworkFunctions.ReadFor(clientStream, 8));
                    activeFiles.Add(new ClientFile(fileName, fileSize));
                }
                else if (headerType == HeaderType.Chunk)
                {
                    byte[] fileName = NetworkFunctions.ReadFileName(clientStream);
                    int contentLen = BitConverter.ToInt32(NetworkFunctions.ReadFor(clientStream, 4));
                    byte[] content = NetworkFunctions.ReadFor(clientStream, contentLen);
                    OnChunkReceived.Invoke(new ServerChunk(fileName, content));
                }
                else
                    clientStream.Flush();
            }
        }

        /// <summary>
        /// Sets the current file that's being processed and the <see cref="WriteToClient(byte[], string)"/>'s mutex is waiting for to release.
        /// </summary>
        /// <param name="FileName">Filename that processings are being done on.</param>
        /// <param name="State">State of processing. (<see cref="WriteToStream(byte[], string)"/> is the only method that calls this method, setting this to false.)</param>
        /// <remarks>This process is necessary because simultaneous files might each send different commands at the same time and releasing the mutex when a header type is something like <see cref="HeaderType.FileCancellationSignal"/> should happen instantly. We shouldn't release the write mutex if the file that's requested a write and has locked the method isn't the same as the one that has sent a command.
        /// <para>If <paramref name="FileName"/> isn't the same as <see cref="activeProcessingFileName"/> and <paramref name="State"/> is true, this method will not change anything unless they both are the same names. Else, variables will get updated.</para></remarks>
        public static void SetActiveFileNameHeaderProcessingState(string FileName, bool State)
        {
            if (activeProcessingFileName == FileName && State)
                activeFileNameProcessingDone = State;
            else
            {
                activeProcessingFileName = FileName;
                activeFileNameProcessingDone = State;
            }
        }

        /// <summary>
        /// Handles post operation functionalities of a <see cref="ServerFile"/>
        /// </summary>
        /// <param name="File">Caller of this function.</param>
        /// <param name="MessageColor">Color to write <paramref name="Message"/> with.</param>
        /// <param name="Message">Message to write to the console.</param>
        public static void File_OnOperationDone(ClientFile File, ConsoleColor MessageColor, string Message)
        {
            m_On_Operation_Done.WaitOne(Core.ListeningTimeout);
            WriteShape(MessageColor, ConsoleShapes.Rectangle, Message);
            lock (activeFiles)
                activeFiles.Remove(File);
            m_On_Operation_Done.ReleaseMutex();
        }
    }
}