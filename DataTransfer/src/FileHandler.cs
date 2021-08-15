using DataTransfer.General;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using static DataTransfer.General.ConsoleGraphics<DataTransfer.General.ConsoleShape2D>;

namespace DataTransfer.src
{
    /// <summary>
    /// Contains all of the functionalities that <see cref="Server"/> and <see cref="Client"/> need to deal with the files.
    /// </summary>
    public static class FileHandler
    {
        /// <summary>
        /// (Each header has a type based on <see cref="Shared.HeaderType"/>). The size is in bytes.
        /// </summary>
        public const byte HEADER_TYPE_SIZE = 1;

        /// <summary>
        /// Maximum length for a fileName. If fileName length varies on different devices, the fileName will be cut down to 255 characters including its extension using <see cref="ServerFile.PrepareFileName"/>
        /// </summary>
        public const byte MAX_FILE_NAME_LEN = 255;

        /// <summary>
        /// Maximum size for a fileName in bytes.
        /// </summary>
        public const byte MAX_FILENAME_LEN_SIZE = 1;

        /// <summary>
        /// Max header size in bytes.
        /// <para>FileNameLenSize + ContentLen + FileName</para>
        /// <para>byte + int32 + 255</para>
        /// </summary>
        /// <remarks>This number could be less based on fileName's length but instead of recalculating a maximum size for the body of our chunks based on the active fileName's length, we can suppose this value as the subtractor of body size.</remarks>
        public const int MAX_HEADER_SIZE = MAX_FILENAME_LEN_SIZE + 4 + MAX_FILE_NAME_LEN;

        /// <summary>
        /// Max header size, not including file's name size, in bytes.
        /// <para>FileNameLenSize + ContentLen</para>
        /// <para>byte + int32</para>
        /// </summary>
        public const byte HEADER_SIZE_NO_FILENAME = MAX_FILENAME_LEN_SIZE + 4;

        /// <summary>
        /// Max size for chunk's body in bytes.
        /// </summary>
        public const int MAX_CHUNK_SIZE = int.MaxValue - MAX_HEADER_SIZE;

        /// <summary>
        /// Maximum size for a signal header in bytes.
        /// <para>Header type size + FileName's length + File's size</para>
        /// <para>byte + byte + long</para>
        /// </summary>
        public const int MAX_SIGNAL_HEADER_SIZE = HEADER_TYPE_SIZE + 1 + 8;

        /// <summary>
        /// Maxmimum size of cancellation signal's header, not including fileName's size, in bytes.
        /// </summary>
        public const int MAX_CANCELLATION_SIGNAL_SIZE_NO_FILENAME = HEADER_TYPE_SIZE + MAX_FILENAME_LEN_SIZE;

        /// <summary>
        /// Message that should be shown when the user requests a file cancellation.
        /// </summary>
        public const string USER_FILE_CANCELLATION_MESSAGE = "Operation was cancelled by the user.";

        /// <summary>
        /// Message that should be shown when cancellation happens out of the known scopes (Method scopes that we know why cancellations happen there.)
        /// </summary>
        /// <example><see cref="ServerFile.SendFile().243"/>If cancellation gets requested when we're at a new cycle of our while loop, we break the while loop and will finally reach line 243 at which we don't know if cancellation was requested by a local command or by a cancellation signal.</example>
        public const string OUT_OF_SCOPE_CANCELLATION_MESSAGE = "OOS Cancellation";

        /// <summary>
        /// Defines an entity that's a file data holder.
        /// </summary>
        private interface IFile
        {
            public string GetFileName();
            public byte[] GetFileNameBytes();

            /// <summary>
            /// Returns a string showing the conditions of this file.
            /// </summary>
            public string GetStatus();
        }

        /// <summary>
        /// Represents a file on the server side containing all of the info and functions needed.
        /// </summary>
        public class ServerFile : IFile, IDisposable
        {
            private bool _disposed = false;
            private bool _enforced_Disposal = false;
            private readonly string filePath;
            private readonly string fileName;
            private long fileSize;
            private readonly byte[] b_FileName;
            private readonly byte fileNameLen;
            private bool signalConfirmed = false;
            private readonly int headerSize;
            private CancellationTokenSource cts_Active = new CancellationTokenSource();
            private FileStream activeStream;
            private long bytesRead = 0;
            private bool waitingForResponse = false;
            private string cancellationReason = string.Empty;
            private bool successful = false;

            /// <summary>
            /// Instantiates a new instance, letting the instance take responsibilities for preparing <see cref="fileName"/>.
            /// </summary>
            /// <param name="FilePath">Path to the surely existing file.</param>
            public ServerFile(string FilePath)
            {
                Server.OnClientResponseReceived += Server_OnClientResponseReceived;
                filePath = FilePath;
                fileName = Directories.PrepareFileName(Path.GetFileName(FilePath));
                b_FileName = Core.ASCII.GetBytes(fileName);
                fileNameLen = (byte)fileName.Length;
                headerSize = HEADER_TYPE_SIZE + HEADER_SIZE_NO_FILENAME + fileName.Length;
                new Task(SendFile, cts_Active.Token, TaskCreationOptions.LongRunning).Start();
                WriteShape(ConsoleColor.DarkBlue, ConsoleShapes.Rectangle, $"[File Operation] ({fileName})");
            }

            /// <summary>
            /// Instantiates a new instance, without the insatance taking responsibilities for preparing <see cref="fileName"/>.
            /// </summary>
            /// <param name="FilePath">Path to the surely existing file.</param>
            /// <param name="FileName">Network name for the file.</param>
            public ServerFile(string FilePath, string FileName)
            {
                Server.OnClientResponseReceived += Server_OnClientResponseReceived;
                filePath = FilePath;
                fileName = FileName;
                b_FileName = Core.ASCII.GetBytes(fileName);
                fileNameLen = (byte)fileName.Length;
                headerSize = HEADER_TYPE_SIZE + HEADER_SIZE_NO_FILENAME + fileName.Length;
                new Task(SendFile, cts_Active.Token, TaskCreationOptions.LongRunning).Start();
            }

            public string GetFileName() => fileName;

            public byte[] GetFileNameBytes() => b_FileName;

            public string GetStatus() => $"[{fileName}][{bytesRead * 100 / fileSize}%]";

            /// <summary>
            /// Sends a signal to the client about the newly incoming file.
            /// </summary>
            private void SendFileSignal()
            {
                byte[] signal = new byte[MAX_SIGNAL_HEADER_SIZE + fileNameLen];
                EmplaceHeader(fileSize, ref signal);
                Server.SetActiveFileNameHeaderProcessingState(fileName, true);
                Server.WriteToClient(signal, fileName);
            }

            /// <summary>
            /// Sends a signal to the client informing them of the cancellation of this file.
            /// </summary>
            private void SendCancellationSignal()
            {
                byte[] signal = new byte[MAX_CANCELLATION_SIGNAL_SIZE_NO_FILENAME + fileNameLen];
                EmplaceHeader(ref signal);
                Server.SetActiveFileNameHeaderProcessingState(fileName, true);
                Server.WriteToClient(signal, fileName);
            }

            /// <summary>
            /// Emplaces the needed data in <paramref name="Bytes"/>
            /// </summary>
            private void EmplaceHeader(int ContentLength, ref byte[] Bytes, Shared.HeaderType HeaderType = Shared.HeaderType.Chunk)
            {
                Bytes[0] = (byte)HeaderType;
                Bytes[1] = fileNameLen;
                Bytes.Emplace(b_FileName, 2);
                Bytes.Emplace(BitConverter.GetBytes(ContentLength), 2 + b_FileName.Length);
            }

            /// <summary>
            /// Emplaces the needed data in <paramref name="Bytes"/>
            /// </summary>
            private void EmplaceHeader(ref byte[] Bytes, Shared.HeaderType HeaderType = Shared.HeaderType.FileCancellationSignal)
            {
                Bytes[0] = (byte)HeaderType;
                Bytes[1] = fileNameLen;
                Bytes.Emplace(b_FileName, 2);
            }

            /// <summary>
            /// Emplaces the needed data in <paramref name="Bytes"/>
            /// </summary>
            private void EmplaceHeader(long ContentLength, ref byte[] Bytes, Shared.HeaderType HeaderType = Shared.HeaderType.FileSignal)
            {
                Bytes[0] = (byte)HeaderType;
                Bytes[1] = fileNameLen;
                Bytes.Emplace(b_FileName, 2);
                Bytes.Emplace(BitConverter.GetBytes(ContentLength), 2 + b_FileName.Length);
            }

            /// <summary>
            /// Performs the needed tasks for sending this file.
            /// </summary>
            private void SendFile()
            {
                try
                {
                    activeStream = File.OpenRead(filePath);
                    fileSize = activeStream.Length;
                    SendFileSignal();
                    Core.Threads.SleepUntil(() => { return signalConfirmed; });
                    if (signalConfirmed)
                    {
                        while (bytesRead < fileSize && !cts_Active.IsCancellationRequested)
                        {
                            var remaining = fileSize - bytesRead;
                            int count = (int)(Server.ChunkSize > remaining ? remaining : Server.ChunkSize);
                            byte[] bytes = new byte[headerSize + count];
                            int s_Read = 0;
                            for (; s_Read < count;)
                            {
                                s_Read += activeStream.Read(bytes, s_Read + headerSize, count - s_Read);
                            }
                            bytesRead += s_Read;
                            EmplaceHeader(count, ref bytes);
                            Server.WriteToClient(bytes, fileName);
                            waitingForResponse = true;
                            Core.Threads.SleepUntil(() => { return !waitingForResponse || cts_Active.IsCancellationRequested; });
                            if (waitingForResponse && !cts_Active.IsCancellationRequested)
                            {
                                Cancel(ConsoleManager.ConsoleResponses.RESPONSE_TIMEOUT);
                                break;
                            }
                        }
                        if (!cts_Active.IsCancellationRequested)
                        {
                            successful = true;
                            Dispose();
                        }
                        else if (cancellationReason == string.Empty)
                            Cancel(OUT_OF_SCOPE_CANCELLATION_MESSAGE);
                    }

                    else
                    {
                        Cancel(ConsoleManager.ConsoleResponses.RESPONSE_TIMEOUT, false); //Shouldn't send a cancellation signal because there were no confirmations.
                    }
                }
                catch (Exception Exc)
                {
                    WriteShape(ConsoleManager.ConsoleColors.ErrorColor, ConsoleShapes.Rectangle, Exc.ToStr());
                    Dispose();
                }
            }

            private void Server_OnClientResponseReceived(Shared.ClientHeader Response)
            {
                if (Response.HeaderFileName != fileName)
                    return;
                if (Response.HeaderType == Shared.HeaderType.FileCancellationSignal)
                {
                    Cancel(Server.CLIENT_FILE_CANCELLATION_SIGNAL_REASON, false);
                    return;
                }
                if (Response.HeaderType == Shared.HeaderType.Chunk)
                    waitingForResponse = false;
                else if (Response.HeaderType == Shared.HeaderType.FileSignal)
                    signalConfirmed = true;
                Server.SetActiveFileNameHeaderProcessingState(fileName, true);
            }

            /// <summary>
            /// Compares the string with <see cref="filePath"/>.
            /// </summary>
            public static bool operator ==(ServerFile F, string S) => F.filePath == S;

            /// <summary>
            /// Compares the string with <see cref="filePath"/>.
            /// </summary>
            public static bool operator !=(ServerFile F, string S) => F.filePath != S;

            /// <summary>
            /// Compares the <see cref="filePath"/>s
            /// </summary>
            public static bool operator ==(ServerFile F, ServerFile F1) => F.filePath == F1.filePath;

            /// <summary>
            /// Compares the <see cref="filePath"/>s
            /// </summary>
            public static bool operator !=(ServerFile F, ServerFile F1) => F.filePath != F1.filePath;

            public static implicit operator string(ServerFile F) => F.filePath;
            public static implicit operator ServerFile(string S) => new ServerFile(S);

            public override string ToString() => $"[{fileName}][[{bytesRead}]/[{fileSize}] ({bytesRead * 100 / fileSize}%)]";

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            /// <summary>
            /// Cancels the sending process of this file.
            /// </summary>
            /// <param name="LocalCommand">If true, it means that cancellation was requested/decided (Meaning that if as an example we face a response timeout, cancellation was requested because of an external reason, so <paramref name="LocalCommand"/> must be false) 
            /// by a local process and thus, a cancellation signal should be sent to the client on the other side. Otherwise, it means that this function was called because of a received cancellation signal, so, we don't need to send a cancellation signal.</param>
            public void Cancel(string CancellationReason, bool LocalCommand = true)
            {
                if (!cts_Active.IsCancellationRequested)
                {
                    cts_Active.Cancel();
                    if (LocalCommand)
                        SendCancellationSignal();
                    cancellationReason = CancellationReason;
                    Dispose();
                }
            }

            /// <summary>
            /// Enforces disposal of this object. Should be used when activeFiles array is being enumerated and any modification breaks code's execution. This means that disposal will happen without calling <see cref="Server.File_OnOperationDone(ServerFile, ConsoleColor, string))"/>.
            /// </summary>
            public void EnforceDisposal()
            {
                if (!successful)
                    cts_Active.Cancel();
                _enforced_Disposal = true;
                Dispose();
            }

            public void Dispose()
            {
                Server.SetActiveFileNameHeaderProcessingState(fileName, true); //Release for any unexpected exception.
                if (_disposed)
                    return;
                _disposed = true;
                try
                {
                    if (activeStream != null)
                    {
                        activeStream.Dispose();
                        activeStream = null;
                    }
                }
                catch { }
                if (cts_Active.IsCancellationRequested && !_enforced_Disposal)
                    Server.File_OnOperationDone(this, ConsoleColor.Red, $"({filePath})'s operation was cancelled. [{cancellationReason}]");

                else if (successful && !_enforced_Disposal)
                    Server.File_OnOperationDone(this, ConsoleColor.DarkGreen, $"({filePath}) was successfully sent!");

                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Represents a file on the client side containing all of the info and functions needed.
        /// </summary>
        public class ClientFile : IFile, IDisposable
        {
            private bool _disposed = false;
            private bool _enforced_Disposal = false;
            private readonly string fileName;
            private readonly string filePath;
            private readonly long fileSize;
            private readonly byte[] b_FileName;
            private readonly byte fileNameLen;
            private FileStream fileStream;
            private readonly int headerSize;
            private bool cancelled = false;
            private string cancellationReason = string.Empty;
            private bool successful = false;
            private static readonly Core.SpanComparator<bool> fileNameComparator = (ReadOnlySpan<char> Span) => { return !File.Exists(string.Concat(Directories.OutputPath, Span)); };

            public ClientFile(byte[] FileName, long FileSize)
            {
                Client.OnChunkReceived += Client_OnChunkReceived;
                b_FileName = FileName;
                fileName = Core.ASCII.GetString(FileName);
                filePath = string.Concat(Directories.OutputPath, Directories.TrimAndGenerateRandom(fileName, Rnd.FILENAME_RND_LEN, fileNameComparator));
                fileStream = File.Create(filePath);
                fileNameLen = (byte)fileName.Length;
                fileSize = FileSize;
                headerSize = HEADER_TYPE_SIZE + MAX_FILENAME_LEN_SIZE + fileName.Length;
                WriteShape(ConsoleColor.DarkBlue, ConsoleShapes.Rectangle, $"[File Signal] ({fileName}\\{fileSize})");
                Confirm(Shared.HeaderType.FileSignal);
            }

            public string GetFileName() => fileName;

            public byte[] GetFileNameBytes() => b_FileName;

            public string GetStatus() => $"[{fileName}][{fileStream.Length * 100 / fileSize}%]";

            /// <summary>
            /// Confirms a task based on the received data from the server by sending them the result using <paramref name="HeaderType"/>.
            /// </summary>
            private void Confirm(Shared.HeaderType HeaderType)
            {
                byte[] data = new byte[headerSize];
                data[0] = (byte)HeaderType;
                data[1] = fileNameLen;
                data.Emplace(b_FileName, 2);
                Client.WriteToStream(data, fileName);
            }

            /// <summary>
            /// Sends a signal to the client informing them of the cancellation of this file.
            /// </summary>
            private void SendCancellationSignal()
            {
                byte[] signal = new byte[MAX_CANCELLATION_SIGNAL_SIZE_NO_FILENAME + fileNameLen];
                EmplaceHeader(ref signal);
                Client.WriteToStream(signal, fileName);
            }

            /// <summary>
            /// Emplaces the needed data in <paramref name="Bytes"/>
            /// </summary>
            private void EmplaceHeader(ref byte[] Bytes, Shared.HeaderType HeaderType = Shared.HeaderType.FileCancellationSignal)
            {
                Bytes[0] = (byte)HeaderType;
                Bytes[1] = fileNameLen;
                Bytes.Emplace(b_FileName, 2);
            }

            /// <summary>
            /// Writes <paramref name="Chunk"/> to <see cref="fileStream"/> and then confirms the received chunk.
            /// </summary>
            private void WriteReceivedChunk(byte[] Chunk)
            {
                if (fileStream != null)
                {
                    fileStream.Write(Chunk, 0, Chunk.Length);
                    Confirm(Shared.HeaderType.Chunk);
                    if (fileStream.Length == fileSize)
                    {
                        successful = true;
                        Dispose();
                    }
                }
                else
                    Cancel("FileHandler.ClientFile.428->NULL");
            }

            private void Client_OnChunkReceived(Shared.ServerChunk Header)
            {
                if (Header.HeaderFileName != fileName)
                    return;
                WriteReceivedChunk(Header.Body);
            }

            /// <summary>
            /// Compares the string with <see cref="filePath"/>.
            /// </summary>
            public static bool operator ==(ClientFile F, string S) => F.filePath == S;

            /// <summary>
            /// Compares the string with <see cref="filePath"/>.
            /// </summary>
            public static bool operator !=(ClientFile F, string S) => F.filePath != S;

            /// <summary>
            /// Compares the <see cref="filePath"/>s
            /// </summary>
            public static bool operator ==(ClientFile F, ClientFile F1) => F.filePath == F1.filePath;

            /// <summary>
            /// Compares the <see cref="filePath"/>s
            /// </summary>
            public static bool operator !=(ClientFile F, ClientFile F1) => F.filePath != F1.filePath;

            public override string ToString()
            {
                return $"[{fileName}][[{fileStream.Length}]/[{fileSize}] ({fileStream.Length * 100 / fileSize}%)]";
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            /// <summary>
            /// Cancels the sending process of this file.
            /// </summary>
            /// <param name="LocalCommand">If true, it means that cancellation was requested/decided (Meaning that if as an example we face a response timeout, cancellation was requested because of an external reason, so <paramref name="LocalCommand"/> must be false) 
            /// by a local process and thus, a cancellation signal should be sent to the client on the other side. Otherwise, it means that this function was called because of a received cancellation signal, so, we don't need to send a cancellation signal.</param>
            public void Cancel(string CancellationReason, bool LocalCommand = true)
            {
                if (!cancelled)
                {
                    if (LocalCommand)
                        SendCancellationSignal();
                    cancelled = true;
                    cancellationReason = CancellationReason;
                    Dispose();
                }
            }

            /// <summary>
            /// Enforces disposal of this object. Should be used when activeFiles array is being enumerated and any modification breaks code's execution. This means that disposal will happen without calling <see cref="Client.File_OnOperationDone(ClientFile, ConsoleColor, string)"/>.
            /// </summary>
            public void EnforceDisposal()
            {
                _enforced_Disposal = true;
                Dispose();
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                if (fileStream != null)
                {
                    fileStream.Dispose();
                    fileStream = null;
                }

                if (cancelled && !_enforced_Disposal)
                    Client.File_OnOperationDone(this, ConsoleColor.Red, $"({filePath})'s operation was cancelled. [{cancellationReason}]");
                else if (successful && !_enforced_Disposal)
                    Client.File_OnOperationDone(this, ConsoleColor.DarkGreen, $"({filePath}) was successfully received!");

                GC.SuppressFinalize(this);
            }
        }
    }
}