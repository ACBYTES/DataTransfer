using System;
using System.IO;

namespace DataTransfer.General
{
    /// <summary>
    /// Contains information about all of the needed directories and paths.
    /// </summary>
    public static class Directories
    {
        private const string ASSEMBLY_NAME = "DataTransfer.dll";
        
        /// <summary>
        /// Path at which this executable exists and is executed from.
        /// </summary>
        public static string StartupPath { get; } = $"{System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(ASSEMBLY_NAME, "")}";

        /// <summary>
        /// The path where all the received files will be saved to.
        /// </summary>
        public static string OutputPath { get; set; } = $"{StartupPath}Files\\";

        static Directories()
        {
            Directory.CreateDirectory(OutputPath);
        }

        /// <summary>
        /// Generates some random numbers for <paramref name="FileName"/> and appends them to the end of the <paramref name="FileName"/>'s value, before its extension.<para>This function does also trim <paramref name="FileName"/> to make sure its length doesn't exceed <see cref="src.FileHandler.MAX_FILE_NAME_LEN"/></para>
        /// </summary>
        /// <param name="FileName">FileName to generate random numbers for it.</param>
        /// <param name="RandomLength">Amount of numbers to add to <paramref name="FileName"/></param>
        /// <param name="Comparator">Comparator for comparing the generated fileName to see if it meets the needs. <para>One example of this can be a fileName comparator to make sure the generated name doesn't exist and if it does, this function will redo its job to generate a new fileName.</para><para>Note that it should return true if the returned value matched the needs.</para></param>
        /// <returns>A possibly trimmed fileName with some random numbers.</returns>
        public static ReadOnlySpan<char> TrimAndGenerateRandom(string FileName, int RandomLength, src.Core.SpanComparator<bool> Comparator)
        {
            ReadOnlySpan<char> tempFN = FileName.AsSpan();
            while (!Comparator.Invoke(tempFN)) //File.Exists(string.Concat(filePath, tempFN))
            {
                int extIndex = FileName.LastIndexOf('.');
                ReadOnlySpan<char> extension = extIndex > 0 ? tempFN.Slice(extIndex) : string.Empty;
                int trimInd = src.FileHandler.MAX_FILE_NAME_LEN - extension.Length - RandomLength;
                int fileNameLenNoExtension = tempFN.Length - extension.Length;
                ReadOnlySpan<char> trimmedName = string.Concat(tempFN.Slice(0, trimInd > fileNameLenNoExtension ? fileNameLenNoExtension : trimInd), src.Rnd.GenerateRandomNumbers(RandomLength));
                tempFN = string.Concat(trimmedName, extension);
            }
            return tempFN;
        }

        /// <summary>
        /// Prepares <paramref name="FileName"/> for network communications. FileNames shouldn't be longer than <see cref="src.FileHandler.MAX_FILE_NAME_LEN"/>; as a result, this function trims down <paramref name="FileName"/> to <see cref="src.FileHandler.MAX_FILE_NAME_LEN"/> characters.
        /// </summary>
        /// <param name="FileName">FileName to prepare.</param>
        /// <returns>If <paramref name="FileName"/> is longer than <see cref="src.FileHandler.MAX_FILE_NAME_LEN"/>, a trimmed string will be returned. Otherwise <paramref name="FileName"/> itself.</returns>
        public static string PrepareFileName(string FileName)
        {
            if (FileName.Length > src.FileHandler.MAX_FILE_NAME_LEN)
            {
                ReadOnlySpan<char> tempFN = FileName.AsSpan();
                ReadOnlySpan<char> extension = tempFN.Slice(FileName.LastIndexOf('.'));
                ReadOnlySpan<char> trimmedName = tempFN.Slice(0, src.FileHandler.MAX_FILE_NAME_LEN - extension.Length);
                return string.Concat(trimmedName, extension);
            }
            return FileName;
        }
    }
}