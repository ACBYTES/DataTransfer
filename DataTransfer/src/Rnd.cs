using System;

namespace DataTransfer.src
{
    /// <summary>
    /// All randomization functionalities of this application.
    /// </summary>
    public static class Rnd
    {
        private static readonly Random random = new Random();

        /// <summary>
        /// Length of the random numbers that get placed in front of a fileName used by the callers of <see cref="General.Directories.TrimAndGenerateRandom(string, int, Core.SpanComparator{bool})"/>.
        /// </summary>
        public const int FILENAME_RND_LEN = 4;

        /// <summary>
        /// Generates a string containing <paramref name="Length"/> random numbers in it.
        /// </summary>
        /// <param name="Length"></param>
        /// <returns>A string containing random numbers.</returns>
        public static string GenerateRandomNumbers(int Length)
        {
            if (Length < 1)
                return null;
            string res = "-";
            for (int i = 0; i < Length; i++)
            {
                res += random.Next(0, 9);
            }
            return res;
        }
    }
}