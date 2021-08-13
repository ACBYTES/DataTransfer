using DataTransfer.src;
using System;
using static DataTransfer.General.ConsoleGraphics<DataTransfer.General.ConsoleShape2D>;

namespace DataTransfer.General
{
    /// <summary>
    /// Contains all the functions and fields related to the user and console.
    /// </summary>
    public static class ConsoleManager
    {
        /// <summary>
        /// General message shown when execution of a command fails.
        /// </summary>
        public const string COMMAND_EXECUTION_FAILURE = "[Command Execution Failed]";

        /// <summary>
        /// All preferred <see cref="ConsoleColor"/>s
        /// </summary>
        public struct ConsoleColors
        {
            public const ConsoleColor ErrorColor = ConsoleColor.Red;
            public const ConsoleColor DefaultForeground = ConsoleColor.White;
            public const ConsoleColor DefaultBackground = ConsoleColor.Black;
            public const ConsoleColor DirectoryColor = ConsoleColor.Yellow;
            public const ConsoleColor FileColor = ConsoleColor.Cyan;
        }

        /// <summary>
        /// Simple console command that takes no parameters to respond.
        /// </summary>
        public struct ConsoleCommand
        {
            private readonly string command;
            private readonly Action response;

            public ConsoleCommand(string Command, Action Response)
            {
                command = Command;
                response = Response;
            }

            /// <summary>
            /// Responds to command.
            /// </summary>
            public void Respond()
            {
                response.Invoke();
            }

            /// <summary>
            /// Compares <paramref name="Command"/>'s <see cref="command"/> with <paramref name="Val"/>
            /// </summary>
            public static bool operator ==(ConsoleCommand Command, string Val) => Val == Command.command;
            /// <summary>
            /// Compares <paramref name="Command"/>'s <see cref="command"/> with <paramref name="Val"/>
            /// </summary>
            public static bool operator !=(ConsoleCommand Command, string Val) => Val != Command.command;

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public string GetCommand()
            {
                return command;
            }
        }

        /// <summary>
        /// Console command that takes an object from type <typeparamref name="T"/> to respond.
        /// </summary>
        /// <typeparam name="T">Type of parameter that <c>Respond</c> takes.</typeparam>
        public struct ConsoleParamedCommand<T>
        {
            private readonly string command;
            private readonly Action<T> response;

            public ConsoleParamedCommand(string Command, Action<T> Response)
            {
                command = Command;
                response = Response;
            }

            /// <summary>
            /// Responds to command.
            /// </summary>
            /// <param name="Param">Parameter of type <typeparamref name="T"/> to respond with.</param>
            public void Respond(T Param)
            {
                response.Invoke(Param);
            }

            /// <summary>
            /// Compares <paramref name="Command"/>'s <see cref="command"/> with <paramref name="Val"/>
            /// </summary>
            public static bool operator ==(ConsoleParamedCommand<T> Command, string Val) => Val == Command.command;
            /// <summary>
            /// Compares <paramref name="Command"/>'s <see cref="command"/> with <paramref name="Val"/>
            /// </summary>
            public static bool operator !=(ConsoleParamedCommand<T> Command, string Val) => Val != Command.command;

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        /// <summary>
        /// Console responses to some commands.
        /// </summary>
        public struct ConsoleResponses
        {
            public static readonly string Help = "-dev [Redirects you to developer's website]\n\n" +
                "-s IPAddress:PORT [Starts a server on the IP address on the specified port (No IP => App will look for machine's IPv4 address.) (No Port => App's default port.)]\n\n" +
                "-c IPAddress:PORT [Connects to a server with the specified address and port (No Port => App's default port.)) as a client]\n\n" +
                "-status [Shows your current status]\n\n" +
                "-send FilePath [Sends file to the first client connected]\n\n" +
                "-send -explorer FileName [Sends file in explorer's directory to the first client connected]\n\n" +
                "-send -explorer -i FileIndex [Sends file in explorer's directory based on the selected index. Index is the row number of the file shown when -explorer is called (File Indices only)]\n\n" +
                "-cancel FileIndex [Requests cancellation for the file at the corresponding index]\n\n" +
                $"-chunk Size [Sets the size of the chunks sent from the server. This value can't be more than ({FileHandler.MAX_CHUNK_SIZE})][Default: {Server.ChunkSize} bytes]\n\n" +
                "-memory [Memory occupied by this process.]\n\n" +
                "-explorer Func { Func: (No Func [Shows explorer at current directory]) || (-o Dir [Opens directory]) || (-o -i Index [Opens directory at selected index (Directory Indices only)]) }\n\n" +
                "-output Path [Sets the path where all the received files will be saved to]\n\n" +
                "-output -explorer [Sets the path where all the received files will be saved to, to the explorer's path]\n\n" +
                "-timeout Time [Sets the listening timeout on this side of the connection to Time (Milliseconds)]\n\n" +
                "-reset [Resets your session for new constructions and commands]\n\n" +
                "-clear [Clears console buffer.]";
            public const string ADDRESS_PARSE_ERROR = "Couldn't Parse Address";
            public const string PORT_PARSE_ERROR = "Couldn't Parse Port";
            public const string INVALID_STRUCTURE = "[Invalid Structure]";
            public const string RESPONSE_TIMEOUT = "Response TImeout";
        }

        /// <summary>
        /// A class containing info about a colored message that's going to be written to a console.
        /// </summary>
        /// <remarks>Generally used with <see cref="WriteLineCC(CC[])"/></remarks>
        public class CC
        {
            private readonly ConsoleColor color;
            private readonly string message;

            public CC(ConsoleColor Color, string Message)
            {
                color = Color;
                message = Message + " ";
            }

            public static implicit operator string(CC cc) => cc.message;
            public static implicit operator ConsoleColor(CC cc) => cc.color;

            public override string ToString()
            {
                return message;
            }
        }

        /// <summary>
        /// Array of all possible one-segment console commands.
        /// </summary>
        public static ConsoleCommand[] ConsoleCommands { get; } = new ConsoleCommand[] { new ConsoleCommand("exit", new Action(() => Environment.Exit(0))),
            new ConsoleCommand("-h", () => WriteShape(ConsoleColor.Yellow, ConsoleShapes.Rectangle, ConsoleResponses.Help)),
            new ConsoleCommand("-memory", () => WriteShape(ConsoleColor.Cyan, ConsoleShapes.Rectangle, SystemInfo.OccupiedMemory.ToString() + " bytes")),
            new ConsoleCommand("-clear", Console.Clear),
            new ConsoleCommand("-status", () => { WriteShape(ConsoleColor.Gray, ConsoleShapes.Rectangle, Core.CurrentMode == Core.UserMode.Server ? Server.GetStatus() : (Core.CurrentMode == Core.UserMode.Client ? Client.GetStatus() : "Nothing to show")); }),
            new ConsoleCommand("-dev", () => { try { System.Diagnostics.Process.Start(Core.WEBSITE); } catch { System.Diagnostics.Process.Start("explorer", Core.WEBSITE); } }),
            new ConsoleCommand("-reset", () => { (Core.CurrentMode == Core.UserMode.Client ? (Action)(() => { Client.Reset(); }) : Core.CurrentMode == Core.UserMode.Server ? (() => { Server.Reset(true); }) : () => { WriteShape(ConsoleColor.Gray, ConsoleShapes.Rectangle, "There's nothing to reset."); })(); })
        };

        /// <summary>
        /// Array of all possible console commands that take in a <para>string[]</para> to respond.
        /// </summary>
        public static ConsoleParamedCommand<string[]>[] ConsoleStrArrCommands { get; } = new ConsoleParamedCommand<string[]>[]
        {
            new ConsoleParamedCommand<string[]>("-s", (string[] Vals) =>
            {
                if (Core.CurrentMode == Core.UserMode.None)
                {
                    if (Vals.Length == 1)
                        Core.InitializeServer();
                    else if (Vals.Length == 2)
                        Core.InitializeServer(Vals[1]);
                    else
                        Program.WriteUnknownCommand();
                }
                else
                    WriteShape(ConsoleColor.Gray, ConsoleShapes.Rectangle, $"{COMMAND_EXECUTION_FAILURE} Construction has already been done.");
            }),
            new ConsoleParamedCommand<string[]>("-c", (string[] Vals) =>
            {
                if (Core.CurrentMode == Core.UserMode.None)
                {
                    if (Vals.Length == 2)
                        Core.InitializeClient(Vals[1]);
                    else if (Vals.Length == 3)
                        Core.InitializeClient(Vals[1], Vals[2]);
                    else
                        Program.WriteUnknownCommand();
                }
                else
                    WriteShape(ConsoleColor.Gray, ConsoleShapes.Rectangle, $"{COMMAND_EXECUTION_FAILURE} Construction has already been done.");
            }),
            new ConsoleParamedCommand<string[]>("-send", (string[] Vals) =>
            {
                if (Vals.Length == 2)
                    Server.SendFile(Vals[1]);
                else if (Vals.Length == 3 && Vals[1] == "-explorer")
                    Server.SendFile(Explorer.CurrentDir + "\\" + Vals[2]);
                else if (Vals.Length == 4 && Vals[1] == "-explorer" && Vals[2] == "-i")
                {
                    if (int.TryParse(Vals[3], out int index) && Explorer.TryGetFilePath(index, out string path))
                        Server.SendFile(path);
                    else
                        WriteError(COMMAND_EXECUTION_FAILURE);
                }
                else
                    Program.WriteUnknownCommand();
            }),
            new ConsoleParamedCommand<string[]>("-explorer", (string[] Vals) =>
            {
                if (Vals.Length == 1)
                    Explorer.Show();
                else if (Vals.Length == 3 && Vals[1] == "-o")
                    Explorer.Open(Vals[2]);
                else if (Vals.Length == 4 && Vals[1] == "-o" && Vals[2] == "-i")
                {
                    if (int.TryParse(Vals[3], out int index))
                        Explorer.Open(index);
                    else
                        WriteError($"{COMMAND_EXECUTION_FAILURE} Couldn't parse index.");
                }
                else
                    Program.WriteUnknownCommand();
            }),
            new ConsoleParamedCommand<string[]>("-output", (string[] Vals) =>
            {
                if (Vals.Length == 2)
                {
                    if (Vals[1] == "-explorer")
                    {
                        Directories.OutputPath = Explorer.CurrentDir;
                        WriteShape(ConsoleColor.Green, ConsoleShapes.Rectangle, $"Output path is [{Directories.OutputPath}] now.");
                    }
                    else
                    {
                        if (System.IO.Directory.Exists(Vals[1]))
                        {
                            Directories.OutputPath = Vals[1] + "\\";
                            WriteShape(ConsoleColor.Green, ConsoleShapes.Rectangle, $"Output path is [{Directories.OutputPath}] now.");
                        }
                        else
                            WriteError($"{COMMAND_EXECUTION_FAILURE} Path doesn't exist.");
                    }
                }
                else
                    Program.WriteUnknownCommand();
            }),
        };

        /// <summary>
        /// Array of all possible console commands that take in a <para>string</para> to respond.
        /// </summary>
        public static ConsoleParamedCommand<string>[] ConsoleStrCommands { get; } = new ConsoleParamedCommand<string>[]
        {
            new ConsoleParamedCommand<string>("-chunk", (string Val) =>
            {
                Server.SetChunkSize(Val);
            }),
            new ConsoleParamedCommand<string>("-timeout", (string Val) =>
            {
                int timeout = -1;
                int.TryParse(Val, out timeout);
                if (timeout > 0)
                {
                    Core.ListeningTimeout = timeout;
                    WriteShape(ConsoleColor.Green, ConsoleShapes.Rectangle, $"Timeout = {timeout}");
                }
                else
                    WriteError($"{COMMAND_EXECUTION_FAILURE} Input wasn't in correct format. Timeout should be bigger than 0.");
            }),
            new ConsoleParamedCommand<string>("-cancel", (string Val) =>
            {
                if (int.TryParse(Val, out int index))
                {
                    if (Core.CurrentMode == Core.UserMode.Server)
                        Server.CancelFileOperation(index);
                    else if (Core.CurrentMode == Core.UserMode.Client)
                        Client.CancelFileOperation(index);
                    else
                        WriteError($"[Command Denied] You can't execute this command.");
                }
                else
                    WriteError($"{COMMAND_EXECUTION_FAILURE} Couldn't parse index.");
            }),
        };

        /// <summary>
        /// Sets console's foreground color to <paramref name="ForegroundColor"/>.
        /// </summary>
        /// <param name="ForegroundColor"></param>
        public static void SetConsoleColors(ConsoleColor ForegroundColor)
        {
            Console.ForegroundColor = ForegroundColor;
        }

        /// <summary>
        /// Sets console's foreground and background to <paramref name="ForegroundColor"/> and <paramref name="BackgroundColor"/>.
        /// </summary>
        /// <param name="ForegroundColor"></param>
        /// <param name="BackgroundColor"></param>
        public static void SetConsoleColors(ConsoleColor ForegroundColor, ConsoleColor BackgroundColor)
        {
            Console.ForegroundColor = ForegroundColor;
            Console.BackgroundColor = BackgroundColor;
        }

        /// <summary>
        /// Writes <paramref name="Message"/> to console using <paramref name="ForegroundColor"/> as the foreground.
        /// </summary>
        /// <param name="Message">Message to write.</param>
        /// <param name="ForegroundColor">Color to use.</param>
        public static void WriteColored(string Message, ConsoleColor ForegroundColor)
        {
            SetConsoleColors(ForegroundColor);
            Console.Write(Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Writes <paramref name="Message"/> to console using <paramref name="ForegroundColor"/> as the foreground color and <paramref name="BackgroundColor"/> as the background color.
        /// </summary>
        /// <param name="Message">Message to write.</param>
        /// <param name="ForegroundColor">Foreground color to use.</param>
        /// <param name="BackgroundColor">Background color to use.</param>
        public static void WriteColored(string Message, ConsoleColor ForegroundColor, ConsoleColor BackgroundColor)
        {
            SetConsoleColors(ForegroundColor, BackgroundColor);
            Console.Write(Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Writes <paramref name="Message"/> to console using <paramref name="ForegroundColor"/> as the foreground with a line terminator.
        /// </summary>
        /// <param name="Message">Message to write.</param>
        /// <param name="ForegroundColor">Color to use.</param>
        public static void WriteLineColored(string Message, ConsoleColor ForegroundColor)
        {
            SetConsoleColors(ForegroundColor);
            Console.WriteLine(Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Writes <paramref name="Message"/> to console using <paramref name="ForegroundColor"/> as the foreground color and <paramref name="BackgroundColor"/> as the background color with a line terminator.
        /// </summary>
        /// <param name="Message">Message to write.</param>
        /// <param name="ForegroundColor">Foreground color to use.</param>
        /// <param name="BackgroundColor">Background color to use.</param>
        public static void WriteLineColored(string Message, ConsoleColor ForegroundColor, ConsoleColor BackgroundColor)
        {
            SetConsoleColors(ForegroundColor, BackgroundColor);
            Console.WriteLine(Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Writes <paramref name="Message"/> to console with <see cref="ConsoleColors.ErrorColor"/> as the foreground color with a line terminator.
        /// </summary>
        /// <param name="Message">Message to write.</param>
        public static void WriteError(string Message)
        {
            SetConsoleColors(ConsoleColors.ErrorColor);
            Console.WriteLine(Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Writes <paramref name="Messages"/> to console and changes color based on <paramref name="Messages"/> color indices.
        /// <para>Color index should be placed at start of each message.</para>
        /// <code>Index message0, NewIndex message1...</code>
        /// </summary>
        /// <param name="Messages"></param>
        public static void WriteLineCC(params CC[] Messages)
        {
            try
            {
                foreach (var item in Messages)
                {
                    WriteColored(item, item);
                }
            }
            catch (Exception Exc)
            {
                WriteError(Exc.ToStr());
            }
        }
    }
}