using System;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;

namespace DataTransfer.General
{
    public static class SystemInfo
    {
        private static readonly ComputerInfo info = new ComputerInfo();

        /// <summary>
        /// Memory occupied by this process.
        /// </summary>
        public static long OccupiedMemory { get { using var process = Process.GetCurrentProcess(); return process.PrivateMemorySize64; } }

        /// <summary>
        /// Checks to see if <paramref name="Value"/> exceeds the amount of memory available.
        /// </summary>
        /// <param name="Value">Value to check.</param>
        /// <returns>True if exceeds.<para>False if doesn't.</para></returns>
        public static bool ExceedsAvailableMemory(int Value)
        {
            unchecked
            {
                return (ulong)Value > info.AvailablePhysicalMemory;
            }
        }
    }
}