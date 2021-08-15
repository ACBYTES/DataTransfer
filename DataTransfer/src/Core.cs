using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using DataTransfer.General;
using System.Threading;

using static DataTransfer.General.ConsoleManager;
using static DataTransfer.General.ConsoleGraphics<DataTransfer.General.ConsoleShape2D>;

namespace DataTransfer.src
{
    /// <summary>
    /// Contains all the core functionalities.
    /// </summary>
    public static class Core
    {
        public const string STARTUP_MESSAGE = "DEVELOPED BY [ACBYTES (ALIREZA SHAHBAZI) - (HTTPS://WWW.ACBYTES.IR)]";
        public const string WEBSITE = "https://www.acbytes.ir";

        public delegate void F_OneParam<T>(T Param);
        public delegate T SpanComparator<out T>(ReadOnlySpan<char> C);

        public const int DEFAULT_CONNECTION_PORT = 1011;

        /// <summary>
        /// User's current mode.
        /// </summary>
        public static UserMode CurrentMode { get; private set; } = UserMode.None;

        /// <summary>
        /// Timeout for <see cref="Server"/> and <see cref="Client"/> when listening for responses in milliseconds.
        /// </summary>
        public static int ListeningTimeout { get; set; } = 120000;

        public static Encoding ASCII { get; } = Encoding.ASCII;

        /// <summary>
        /// Checks to see if <see cref="CurrentMode"/> is the same as <paramref name="TargetMode"/>. If not, it'll print a "Command Denied" message.
        /// </summary>
        public static bool HandlePrivileges(UserMode TargetMode)
        {
            if (TargetMode != CurrentMode)
            {
                WriteError($"[Command Denied] You can't execute this command now.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets <see cref="CurrentMode"/> to <see cref="UserMode.None"/>.
        /// </summary>
        public static void ResetUserMode()
        {
            CurrentMode = UserMode.None;
        }

        /// <summary>
        /// All of the possible user modes.
        /// </summary>
        public enum UserMode
        {
            Server, Client, None
        }

        /// <summary>
        /// Initializes a <see cref="Server"/> using <paramref name="Address"/> with a possible port in it.
        /// </summary>
        /// <param name="Address">Address to use with a possible custom port.</param>
        public static void InitializeServer(string Address)
        {
            if (TryParseEndPoint(Address, out IPEndPoint endPoint))
            {
                CurrentMode = UserMode.Server;
                Server.SetUpServer(endPoint);
            }
            else
                WriteShape(ConsoleColors.ErrorColor, ConsoleShapes.Rectangle, ConsoleResponses.INVALID_STRUCTURE);
        }

        /// <summary>
        /// Initializes a <see cref="Server"/> using <see cref="GetLocalIP()"/>.
        /// </summary>
        public static void InitializeServer()
        {
            IPAddress address = GetLocalIP();
            CurrentMode = UserMode.Server;
            Server.SetUpServer(new IPEndPoint(address, DEFAULT_CONNECTION_PORT));
        }

        /// <summary>
        /// Initializes a client using <see cref="GetLocalIP"/> and connects to <paramref name="ServerAddress"/> with a possible port in it.
        /// </summary>
        /// <param name="ServerAddress"> to connect to with a possible custom port.</param>
        public static void InitializeClient(string ServerAddress)
        {
            if (TryParseEndPoint(ServerAddress, out IPEndPoint endPoint))
            {
                CurrentMode = UserMode.Client;
                Client.SetUpClient(new IPEndPoint(GetLocalIP(), DEFAULT_CONNECTION_PORT), endPoint);
            }
            else
                WriteShape(ConsoleColors.ErrorColor, ConsoleShapes.Rectangle, ConsoleResponses.INVALID_STRUCTURE);
        }

        /// <summary>
        /// Initializes a client using <paramref name="ClientAddress"/> and connects to <paramref name="ServerAddress"/> with each of these containing possible ports.
        /// </summary>
        /// <param name="ClientAddress">Client's address to use with a possible custom port.</param>
        /// <param name="ServerAddress">Server's address to use with a possible custom port.</param>
        public static void InitializeClient(string ClientAddress, string ServerAddress)
        {
            if (TryParseEndPoint(ClientAddress, out IPEndPoint clientEndPoint) && TryParseEndPoint(ServerAddress, out IPEndPoint serverEndPoint))
            {
                CurrentMode = UserMode.Client;
                Client.SetUpClient(clientEndPoint, serverEndPoint);
            }
            else
                WriteShape(ConsoleColors.ErrorColor, ConsoleShapes.Rectangle, ConsoleResponses.INVALID_STRUCTURE);
        }

        /// <summary>
        /// Tries to parse <paramref name="Address"/> and output an <see cref="IPEndPoint"/>.
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="Result"></param>
        /// <returns>
        /// <para>True if <paramref name="Address"/> was successfully parsed.</para>
        /// <para>False if otherwise.</para>
        /// </returns>
        private static bool TryParseEndPoint(string Address, out IPEndPoint Result)
        {
            Result = null;
            var ip = Address.AsSpan();
            IPAddress address;
            int port = DEFAULT_CONNECTION_PORT;
            var ind = ip.IndexOf(':');
            if (ind > -1)
            {
                if (TryParsePort(ip.Slice(ind + 1), out port))
                {
                    if (IPAddress.TryParse(ip.Slice(0, ind), out address))
                    {
                        Result = new IPEndPoint(address, port);
                        return true;
                    }
                }
            }

            else
            {
                if (IPAddress.TryParse(ip, out address))
                {
                    Result = new IPEndPoint(address, port);
                    return true;
                }
            }

            WriteError(ConsoleResponses.ADDRESS_PARSE_ERROR);
            return false;
        }

        /// <summary>
        /// Tries to parse a port inside <paramref name="Value"/>.
        /// </summary>
        /// <param name="Value">Possible port to parse.</param>
        /// <param name="Port">Outputs parsed port to this.</param>
        /// <returns>
        /// <para>True if a port was found and parsed.</para>
        /// <para>False if otherwise.</para>
        /// </returns>
        private static bool TryParsePort(ReadOnlySpan<char> Value, out int Port)
        {
            Port = 0;
            if (Value.Length > 0)
            {
                Port = int.Parse(Value);
                if (Port > 0)
                    return true;
            }
            WriteError(ConsoleResponses.PORT_PARSE_ERROR);
            return false;
        }

        /// <summary>
        /// Tries to find an IPv4 address on this machine. If no addresses get found, an <seealso cref="Exception"/> will be thrown.
        /// </summary>
        /// <returns>The IPv4 address on this machine.</returns>
        private static IPAddress GetLocalIP()
        {
            foreach (var item in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                {
                    return item;
                }
            }
            WriteError("Couldn't find local IPv4 IP.");
            throw new Exception("Couldn't find local IP.");
        }

        /// <summary>
        /// Contains and handles all the thread functionalities needed.
        /// </summary>
        public static class Threads
        {
            /// <summary>
            /// Makes thread sleep until <paramref name="Condition"/> returns true. This process will continue until the time that has passed is less than <see cref="ListeningTimeout"/>.
            /// </summary>
            /// <param name="Condition">This will feed the loop with the condition we are waiting for.</param>
            /// <param name="Frequency">Frequency at which <paramref name="Condition"/> will be checked in milliseconds.</param>
            public static void SleepUntil(Func<bool> Condition, int Frequency = 100)
            {
                int timePassed = 0;
                while (!Condition() && timePassed < ListeningTimeout)
                {
                    Thread.Sleep(Frequency);
                    timePassed += Frequency;
                }
            }
        }
    }
}