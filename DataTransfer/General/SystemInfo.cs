using System;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;

namespace DataTransfer.General
{
    public static class SystemInfo
    {
        private static readonly ComputerInfo info = new ComputerInfo();
        private const ulong INT32_MAX = int.MaxValue;

        /// <summary>
        /// Memory occupied by this process.
        /// </summary>
        public static long OccupiedMemory { get { using var process = Process.GetCurrentProcess(); return process.PrivateMemorySize64; } }

        /// <summary>
        /// Checks to see if <paramref name="Value"/> exceeds the amount of memory available.
        /// </summary>
        /// <param name="Value">Value to check.</param>
        /// <returns>True if exceeds.<para>False if doesn't.</para></returns>
        /// <remarks>Receives int because chunk sizes are <see cref="Int32"/>s and this part is only important when <see cref="ComputerInfo.AvailablePhysicalMemory"/> is less than or equal to <see cref="Int32.MaxValue"/> cause in all of the other cases, the result will always be false.</remarks>
        public static bool ExceedsAvailableMemory(int Value)
        {
            if (info.AvailablePhysicalMemory > INT32_MAX)
                return false;
            return (ulong)Value > info.AvailablePhysicalMemory;
        }
    }
}