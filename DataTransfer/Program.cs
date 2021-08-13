using DataTransfer.General;
using DataTransfer.src;
using System;
using System.Threading;

using static DataTransfer.General.ConsoleManager;
using static DataTransfer.General.ConsoleGraphics<DataTransfer.General.ConsoleShape2D>;

namespace DataTransfer
{
    class Program
    {
        static void Main(string[] Args)
        {
            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) =>
            {
                if (Core.CurrentMode == Core.UserMode.Server)
                    Server.Reset();
                else if (Core.CurrentMode == Core.UserMode.Client)
                    Client.Reset();
            };
            SetConsoleColors(ConsoleColors.DefaultForeground, ConsoleColors.DefaultBackground);
            WriteShape(ConsoleColor.DarkCyan, ConsoleShapes.Rectangle, Core.STARTUP_MESSAGE);
            WriteShape(ConsoleColor.Magenta, ConsoleShapes.Rectangle, "PLEASE USE IN FULLSCREEN FOR CORRECT EXPERIENCE");
            HandleCommands(Args.ToString(" "));
            new Thread(ReadTillTermination).Start();
        }

        static void ReadTillTermination()
        {
            while (true)
            {
                HandleCommands(Console.ReadLine());
            }
        }

        static void HandleCommands(string Command)
        {
            var args = Command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args.Length > 0)
            {
                foreach (var item in ConsoleStrArrCommands)
                {
                    if (item == args[0])
                    {
                        item.Respond(args);
                        return;
                    }
                }

                foreach (var item in ConsoleStrCommands)
                {
                    if (args.Length < 2)
                        break;

                    if (item == args[0])
                    {
                        item.Respond(args[1]);
                        return;
                    }
                }

                foreach (var item in ConsoleCommands)
                {
                    if (item == args[0])
                    {
                        item.Respond();
                        return;
                    }
                }

                WriteUnknownCommand();
            }
        }

        public static void WriteUnknownCommand() => WriteError("[Unknown Command]. Use -h For Help.");
    }
}
