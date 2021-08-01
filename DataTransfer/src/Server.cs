using DataTransfer.General;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using static DataTransfer.General.ConsoleManager;
using static DataTransfer.General.ConsoleGraphics<DataTransfer.General.ConsoleShape2D>;
using static DataTransfer.src.FileHandler;
using DataTransfer.src.Shared;

namespace DataTransfer.src
{
    public static class Server
    {
        /// <summary>
        /// Message that should be shown when the client sends a cancellation signal.
        /// </summary>
        public const string CLIENT_FILE_CANCELLATION_SIGNAL_REASON = "Client cancelled the operation.";

        /// <summary>
        /// Event that notifies all subscribers[<see cref="ServerFile"/>s] of a newly received response.
        /// </summary>
        public static event Core.F_OneParam<ClientHeader> OnClientResponseReceived;

        /// <summary>
        /// Current chunk size.
        /// </summary>
        public static int ChunkSize { get; private set; } = MAX_CHUNK_SIZE;

        /// <summary>
        /// Active instance of <see cref="TcpListener"/> that's working as the server.
        /// </summary>
        private static TcpListener listener;

        /// <summary>
        /// Active client that's connected to <see cref="listener"/>
        /// </summary>
        private static TcpClient client;

        /// <summary>
        /// Server's endpoint.
        /// </summary>
        private static IPEndPoint serverEndPoint;

        /// <summary>
        /// Client's endpoint.
        /// </summary>
        private static IPEndPoint clientEndPoint;

        /// <summary>
        /// Active stream that's used to write to the client.
        /// </summary>
        private static NetworkStream clientStream;

        /// <summary>
        /// All of the currently active files being sent and processed.
        /// </summary>
        private static List<ServerFile> activeFiles;

        private static Mutex m_Write_Lock;
        private static CancellationTokenSource cts_Response;

        private static readonly Core.SpanComparator<bool> fileNameComparator = (ReadOnlySpan<char> S) => { return activeFiles.Contains(S.ToString()); };

        /// <summary>
        /// Current file that's being processed and <see cref="WriteToClient(byte[], string)"/>'s thread is waiting for to get done with all of its processings to release <see cref="m_Write_Lock"/>.
        /// </summary>
        private static string activeProcessingFileName;

        /// <summary>
        /// <see cref="activeProcessingFileName"/>'s processing state showing whether its processings are done yet, or not to let <see cref="WriteToClient(byte[], string)"/>'s active thread release <see cref="m_Write_Lock"/>.
        /// </summary>
        private static bool activeFileNameProcessingDone = false;

        /// <summary>
        /// Returns a string containing server's current conditions, files, etc...
        /// </summary>
        public static string GetStatus()
        {
            return $"- Server is up and running on ({serverEndPoint.Address}:{serverEndPoint.Port}).\n\n- {(client == null ? "There's not an active client connected." : $"An active client is connected on {clientEndPoint.Address}:{clientEndPoint.Port}")}\n\n- " +
                $"Active files being sent: [{(activeFiles == null ? "No active files are being sent." : activeFiles.ToString(", ", true))}]";
        }

        /// <summary>
        /// Sets up server.
        /// </summary>
        public static void SetUpServer(IPEndPoint EndPoint)
        {
            try
            {
                m_Write_Lock = new Mutex();
                activeFiles = new List<ServerFile>();
                listener = new TcpListener(EndPoint);
                cts_Response = new CancellationTokenSource();
                serverEndPoint = EndPoint;
                WriteShape(ConsoleColor.Blue, ConsoleShapes.Rectangle, $"Started Server On The Requested Address\n[{EndPoint.Address}:{EndPoint.Port}]");
                Task.Factory.StartNew(ListenForClient);
            }
            catch (Exception Exc)
            {
                Reset();
                WriteShape(ConsoleColor.Red, ConsoleShapes.Rectangle, Exc.ToStr());
            }
        }

        /// <summary>
        /// Tries to listen for a <see cref="TcpClient"/>.
        /// </summary>
        private static void ListenForClient()
        {
            if (!listener.Server.IsBound)
                listener.Start();
            client = listener.AcceptTcpClient();
            clientStream = client.GetStream();
            clientEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
            Task.Factory.StartNew(ReadResponses, cts_Response.Token);
            WriteShape(ConsoleColor.Cyan, ConsoleShapes.Rectangle, $"Client Got Connected.\n[{((IPEndPoint)client.Client.RemoteEndPoint).Address}]");
        }

        /// <summary>
        /// Resets server and everything related to its connection.
        /// </summary>
        public static void Reset()
        {
            if (clientStream != null && clientStream.CanWrite)
                NetworkFunctions.AnnounceDisconnection(clientStream);
            if (activeFiles != null)
            {
                foreach (ServerFile item in activeFiles)
                {
                    item.Dispose();
                }
                activeFiles.Clear();
            }
            try
            {
                if (listener != null)
                    listener.Stop();
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
                if (cts_Response != null)
                    cts_Response.Cancel();
            }
            catch { }
            listener = null;
            client = null;
            activeFiles = null;
            cts_Response = null;
            serverEndPoint = null;
            clientEndPoint = null;
            try
            {
                m_Write_Lock.Dispose();
            }
            catch { }
            Core.ResetUserMode();
            WriteShape(ConsoleColor.Green, ConsoleShapes.Rectangle, "Server reset was successful!");
        }

        /// <summary>
        /// Clears the things related to the previous connection of the server for listening for a new <see cref="TcpClient"/>.
        /// </summary>
        private static void Clear()
        {
            foreach (ServerFile item in activeFiles)
            {
                item.Cancel("SERVER-CLEARANCE", false);
            }
            client = null;
            clientEndPoint = null;
            clientStream.Dispose();
            activeFiles.Clear();
            cts_Response = new CancellationTokenSource();
            try
            {
                m_Write_Lock.Dispose();
            }
            catch { }
            m_Write_Lock = new Mutex();
            Task.Factory.StartNew(ListenForClient);
        }

        /// <summary>
        /// Sets the size of the chunks sent to the client.
        /// </summary>
        public static void SetChunkSize(string Size)
        {
            if (Core.HandlePrivileges(Core.UserMode.None))
                if (listener == null)
                {
                    if (int.TryParse(Size, out int size) && size > 0 && size <= MAX_CHUNK_SIZE)
                    {
                        if (!SystemInfo.ExceedsAvailableMemory(size))
                        {
                            ChunkSize = size;
                            WriteShape(ConsoleColor.Green, ConsoleShapes.Rectangle, $"Chunk size is now [{ size }]");
                        }

                        else
                            WriteError($"{COMMAND_EXECUTION_FAILURE} The value is more than the available physical memory.");
                    }

                    else
                        WriteError($"{COMMAND_EXECUTION_FAILURE} Invalid Chunk Size.");
                }

                else
                    WriteError($"{COMMAND_EXECUTION_FAILURE} Can't change server properties after construction.");
        }

        /// <summary>
        /// Calls <see cref="ServerFile.Cancel(string)"/> on a possible <see cref="ServerFile"/> in <see cref="activeFiles"/>.
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
        /// Prepares a new <see cref="ServerFile"/> for sending to the client.
        /// </summary>
        public static void SendFile(string Path)
        {
            if (Core.HandlePrivileges(Core.UserMode.Server))
                if (client == null)
                    WriteError($"{COMMAND_EXECUTION_FAILURE} There's not an active client connected.");
                else
                {
                    if (!File.Exists(Path))
                    {
                        WriteShape(ConsoleColor.DarkYellow, ConsoleShapes.Rectangle, $"{COMMAND_EXECUTION_FAILURE}] File doesn't exist.");
                        return;
                    }
                    if (activeFiles.Contains(Path, false))
                    {
                        WriteShape(ConsoleColor.Red, ConsoleShapes.Rectangle, $"[Operation Failed] This file is already being sent!");
                        return;
                    }
                    string fileName = System.IO.Path.GetFileName(Path);
                    if (activeFiles.Contains(fileName, true))
                    {
                        activeFiles.Add(new ServerFile(Path, Directories.TrimAndGenerateRandom(fileName, Rnd.FILENAME_RND_LEN, fileNameComparator).ToString()));
                    }

                    ServerFile file = Path;
                    activeFiles.Add(file);
                }
        }

        /// <summary>
        /// Writes the data passed to <see cref="clientStream"/>.
        /// </summary>
        /// <param name="Bytes">Data to write.</param>
        /// <param name="FileName">FileName that's requesting this write. [Needed to update <see cref="activeProcessingFileName"/>]</param>
        public static void WriteToClient(byte[] Bytes, string FileName)
        {
            Task.Run(() =>
            {
                m_Write_Lock.WaitOne(Core.ListeningTimeout);
                if (clientStream != null)
                {
                    try
                    {
                        clientStream.Write(Bytes, 0, Bytes.Length);
                    }
                    catch (Exception Exc)
                    {
                        WriteShape(ConsoleColors.ErrorColor, ConsoleShapes.Rectangle, Exc.ToStr());
                        Clear();
                    }
                }
                else
                {
                    WriteError("[Couldn't communicate with the client.] Server.204 => NULL || !CW");
                    Reset();
                }
                if (Bytes[0] != (byte)HeaderType.FileCancellationSignal)
                {
                    SetActiveFileNameHeaderProcessingState(FileName, false);
                    Core.Threads.SleepUntil(() => { return activeFileNameProcessingDone; }); //If no processes confirm the end of the active file's processings, we release the mutex because the timeout processings are done by other threads and we don't need to request cancellation the active file here.
                }
                m_Write_Lock.ReleaseMutex();
            });
        }

        /// <summary>
        /// Reads data from <see cref="clientStream"/>.
        /// </summary>
        public static void ReadResponses()
        {
            while (!cts_Response.IsCancellationRequested)
            {
                HeaderType headerType = NetworkFunctions.ReadHeaderType(clientStream);
                if (headerType == HeaderType.Disconnected)
                {
                    WriteShape(ConsoleColor.Yellow, ConsoleShapes.Rectangle, $"{clientEndPoint.Address}:{clientEndPoint.Port} got disconnected! Clearing server side for a new connection.");
                    Clear();
                    break;
                }
                if (headerType > HeaderType.FileCancellationSignal || headerType < HeaderType.Disconnected)
                {
                    clientStream.Flush();
                    return;
                }
                OnClientResponseReceived.Invoke(new ClientHeader(headerType, Core.ASCII.GetString(NetworkFunctions.ReadFileName(clientStream))));
            }
        }

        /// <summary>
        /// Sets the current file that's being processed and the <see cref="WriteToClient(byte[], string)"/>'s mutex is waiting for to release.
        /// </summary>
        /// <param name="FileName">Filename that processings are being done on.</param>
        /// <param name="State">State of processing. (<see cref="WriteToClient(byte[], string)"/> is the only method that calls this method, setting this to false.)</param>
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
        public static void File_OnOperationDone(ServerFile File, ConsoleColor MessageColor, string Message)
        {
            WriteShape(MessageColor, ConsoleShapes.Rectangle, Message);
            lock (activeFiles)
                activeFiles.Remove(File);
        }
    }
}