using System;
using System.IO;
using System.Collections.Generic;

using static DataTransfer.General.ConsoleManager;
using static DataTransfer.General.ConsoleGraphics<DataTransfer.General.ConsoleShape2D>;

namespace DataTransfer.General
{
    /// <summary>
    /// Virtual file explorer created for easier navigations through paths.
    /// </summary>
    public static class Explorer
    {
        /// <summary>
        /// Explorer's current directory.
        /// </summary>
        public static string CurrentDir { get; private set; } = Directories.StartupPath;

        /// <summary>
        /// Prints out explorer's data to the console.
        /// </summary>
        public static void Show()
        {
            string[] dirs = Directory.GetDirectories(CurrentDir);
            string[] files = Directory.GetFiles(CurrentDir);
            List<string> values = new List<string>(dirs.Length + files.Length);
            foreach (var item in files)
            {
                values.Add(GenerateShape(ConsoleShapes.Rectangle, item.Replace(CurrentDir, ""), ""));
            }

            foreach (var item in dirs)
            {
                values.Add(GenerateShape(ConsoleShapes.Rectangle, item.Replace(CurrentDir, ""), ""));
            }

            WriteShape(ConsoleColor.Green, ConsoleShapes.Rectangle, values.Count > 0 ? values.ToString("\n") : "Directory is empty");
        }

        /// <summary>
        /// Opens <paramref name="Dir"/> if exists and calls <see cref="Show"/>. Otherwise, it will notify the user about the wrong directory.
        /// </summary>
        /// <param name="Dir">Directory to open.</param>
        public static void Open(string Dir)
        {
            if (Dir == "..")
            {
                CurrentDir += "..\\";
                Show();
            }
            else if (Dir != "...") //Directory exists returns true for this value so this has to get checked like this.
            {
                if (Directory.Exists(Dir))
                {
                    CurrentDir = Dir.EndsWith("\\") ? Dir : Dir + "\\";
                    Show();
                }

                else
                    WriteShape(ConsoleColor.Red, ConsoleShapes.Rectangle, $"{COMMAND_EXECUTION_FAILURE} Directory doesn't exist.");
            }
        }

        /// <summary>
        /// Opens a directory based on <paramref name="Index"/>. This function retrieves all of <see cref="CurrentDir"/>'s directories and selects the one at <paramref name="Index"/>. If the directory index is wrong or out of range, it'll notify the user. Otherwise, <see cref="Show"/> will be called.
        /// </summary>
        /// <param name="Index">Directory's index.</param>
        public static void Open(int Index)
        {
            string[] dirs = Directory.GetDirectories(CurrentDir);
            if (dirs.Length - 1 < Index)
                WriteShape(ConsoleColor.Red, ConsoleShapes.Rectangle, $"{COMMAND_EXECUTION_FAILURE} Directory doesn't exist.");
            else
            {
                CurrentDir = dirs[Index].EndsWith("\\") ? dirs[Index] : dirs[Index] + "\\";
                Show();
            }
        }

        /// <summary>
        /// Tries to get the path to a file existing in <see cref="CurrentDir"/>. This function retrieves all of the <see cref="CurrentDir"/>'s directories and selects the one at <paramref name="Index"/>.
        /// </summary>
        /// <param name="Index">File's index.</param>
        /// <returns>True if <paramref name="Index"/> is correct and outputs the file's path to <paramref name="Value"/>. <para>Otherwise, false and outputs null to <paramref name="Value"/></para></returns>
        public static bool TryGetFilePath(int Index, out string Value)
        {
            Value = null;
            string[] files = Directory.GetFiles(CurrentDir);
            if (files.Length - 1 < Index)
            {
                WriteShape(ConsoleColor.Red, ConsoleShapes.Rectangle, $"{COMMAND_EXECUTION_FAILURE} File doesn't exist.");
                return false;
            }
            else
            {
                Value = files[Index];
                return true;
            }
                
        }
    }
}
