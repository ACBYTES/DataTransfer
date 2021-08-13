using System;
using System.Collections.Generic;
using System.Linq;

namespace DataTransfer.General
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Calculates half of the length of the vertical value of this console shape based on the message length.
        /// </summary>
        /// <param name="Len">String Length.</param>
        /// <returns>Half of the length of the vertical value.</returns>
        public static int HalfLen(this IConsoleShape Value, int Len)
        {
            return (int)MathF.Floor((float)Value.GetVerticalMultiplier() * Len / 2);
        }

        /// <summary>
        /// Calculates half of the length of this string. 
        /// </summary>
        /// <returns><code>(int)MathF.Floor((float)StrLen / 2)</code></returns>
        public static int HalfLen(this string Value)
        {
            return (int)MathF.Floor((float)Value.Length / 2);
        }

        /// <summary>
        /// Creates a parallel set of this string having <paramref name="HorizontalDistance"/> distance between them.
        /// </summary>
        /// <param name="HorizontalDistance">Distance between two parallel strings.</param>
        /// <returns>Parallel shape string.</returns>
        public static string Parallel(this string Value, int HorizontalDistance)
        {
            return $"{Value}{" ".RepeatString(HorizontalDistance)}{Value}";
        }

        /// <summary>
        /// Gets the amount of '\n's in this string.
        /// </summary>
        /// <returns>Amount of lines.</returns>
        public static int LineCount(this string Value)
        {
            return Value.Count(f => f == '\n');
        }

        /// <summary>
        /// Looks through the strings to find the longest one to return its length.
        /// </summary>
        /// <returns>Length of the longest string.</returns>
        public static int LongestLineStrLen(this IEnumerable<string> Values)
        {
            int res = 0;
            foreach (var item in Values)
            {
                if (item.Length > res)
                    res = item.Length;
            }
            return res;
        }

        /// <summary>
        /// Divides integer by <paramref name="Divisor"/> and performs <code>MathF.Ceiling</code> on it.
        /// </summary>
        /// <param name="Divisor">Number to divide this by.</param>
        /// <returns><code>(int)MathF.Ceiling((float)this / <paramref name="Divisor"/>)</code></returns>
        public static int DivCeil(this int Value, int Divisor)
        {
            return (int)MathF.Ceiling((float)Value / Divisor);
        }

        /// <summary>
        /// Divides integer by <paramref name="Divisor"/> and performs <code>MathF.Floor</code> on it.
        /// </summary>
        /// <param name="Divisor">Number to divide this by.</param>
        /// <returns><code>(int)MathF.Floor((float)this / <paramref name="Divisor"/>)</code></returns>
        public static int DivFloor(this int Value, int Divisor)
        {
            return (int)MathF.Floor((float)Value / Divisor);
        }

        /// <summary>
        /// Repeats string characters for <paramref name="Amount"/> times.
        /// </summary>
        /// <param name="Amount">Amount of times to repeat this string.</param>
        /// <param name="AdditionalValue">Additional value to add at the end of each iteration.</param>
        /// <returns>Repeated string.</returns>
        public static string RepeatString(this string Value, int Amount, string AdditionalValue = "")
        {
            string res = string.Empty;
            for (int i = 0; i < Amount; i++)
            {
                res += Value + AdditionalValue;
            }
            return res;
        }

        /// <summary>
        /// Adds all of the <typeparamref name="T"/>s in <paramref name="Values"/> together separating them by the <paramref name="Separator"/> using <see cref="object.ToString"/>.
        /// </summary>
        /// <param name="Separator">String that gets placed between each string that's been enumerated.</param>
        /// <param name="LastWithoutSeparator">If set to true, the last value that's been separated isn't going to contain <paramref name="Separator"/> at the end of it.</param>
        /// <returns>A string containing all of the <paramref name="Values"/> <typeparamref name="T"/>s together.</returns>
        public static string ToString<T>(this IEnumerable<T> Values, string Separator, bool LastWithoutSeparator = false) ///<typeparam name="T"/> could be written as <see cref="object"/> as well.
        {
            string res = string.Empty;
            foreach (var item in Values)
            {
                res += item.ToString() + Separator;
            }
            return LastWithoutSeparator ? res.Length - Separator.Length > 0 ? res.Remove(res.Length - Separator.Length) : res : res;
        }

        /// <summary>
        /// Places <typeparamref name="T"/>s in <paramref name="Target"/> in this array starting from <paramref name="Index"/>.
        /// </summary>
        /// <typeparam name="T">Type of the array and <paramref name="Target"/></typeparam>
        /// <param name="Target">Target enumerable to place its content in this array.</param>
        /// <param name="Index">Index that emplacement should start from.</param>
        public static void Emplace<T>(this T[] Base, IEnumerable<T> Target, int Index)
        {
            if (Base.Length - 1 < Index)
                return;
            foreach (var item in Target)
            {
                Base[Index++] = item;
            }
        }

        /// <summary>
        /// Converts this exception to a string containing <see cref="Exception.Message"/>, <see cref="Exception.InnerException"/> and <see cref="Exception.StackTrace"/> based on the passed values for <paramref name="Message"/>, <paramref name="InnerException"/> and <paramref name="StackTrace"/>.
        /// </summary>
        /// <param name="Message">Should include message?</param>
        /// <param name="InnerException">Should include inner exception?</param>
        /// <param name="StackTrace">Should include stack trace?</param>
        /// <returns>A string containing this exception's info.</returns>
        public static string ToStr(this Exception Exc, bool Message = true, bool InnerException = true, bool StackTrace = true)
        {
            return $"An exception occurred.{(Message ? $" M: [{Exc.Message}]" : "")}{(InnerException ? $" IE: [{Exc.InnerException}]" : "")}{(StackTrace ? $" ST: [{Exc.StackTrace}]" : "")}";
        }

        /// <summary>
        /// Checks if this enumerable contains a <see cref="src.FileHandler.ServerFile"/> with the same [fileName/filePath (<paramref name="SearchWithFileName"/>)] as <paramref name="Value"/>.
        /// </summary>
        /// <param name="Value">FileName to look for.</param>
        /// <param name="SearchWithFileName">Should check items based on their fileNames? If true, items will be checked using <see cref="src.FileHandler.ServerFile.GetFileName"/></param>
        /// <returns>True if found; otherwise, false.</returns>
        public static bool Contains(this IEnumerable<src.FileHandler.ServerFile> Files, string Value, bool SearchWithFileName)
        {
            if (SearchWithFileName)
                foreach (var item in Files)
                {
                    if (item.GetFileName() == Value)
                        return true;
                }
            else
                foreach (var item in Files)
                {
                    if (item == Value)
                        return true;
                }
            return false;
        }
    }
}
