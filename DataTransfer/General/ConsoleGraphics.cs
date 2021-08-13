using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransfer.General
{
    /// <summary>
    /// Controls Graphical Content of Console
    /// </summary>
    /// <typeparam name="T">Shape Type (2D or 3D)</typeparam>
    public static class ConsoleGraphics<T> where T : IConsoleShape
    {
        /// <summary>
        /// Multiplying calculations are done per 5 characters. See <see cref="GenerateShape(ConsoleShapes, string, string)"/>.
        /// </summary>
        private const int CALCULATIONS_PER_CHARACTER = 5;

        private static readonly IConsoleShape[] shapes2D = new IConsoleShape[]
        {
            new ConsoleShape2D("_", "║", 6, 1),
        };

        private static readonly IConsoleShape[] shapes3D = new IConsoleShape[]
        {
            new ConsoleShape3D("__", "║", "/", 6, 1, 1),
        };

        /// <summary>
        /// Returns <paramref name="Shape"/> in Specified Dimensions
        /// </summary>
        /// <param name="Shape">Target Shape</param>
        /// <returns>A <typeparamref name="T"/>.</returns>
        public static T GetShape(ConsoleShapes Shape)
        {
            return (T)(typeof(T) == typeof(ConsoleShape2D) ? shapes2D : shapes3D)[(int)Shape];
        }

        /// <summary>
        /// Generates <paramref name="Shape"/> and returns it as a string.
        /// </summary>
        /// <param name="Shape">Shape to write.</param>
        /// <param name="Message">Message in <paramref name="Shape"/></param>
        /// <param name="EndL">Line Ending string.</param>
        /// <returns>Generated <paramref name="Shape"/> as string.</returns>
        public static string GenerateShape(ConsoleShapes Shape, string Message = "", string EndL = "\n")
        {
            if (typeof(T) == typeof(ConsoleShape2D))
            {
                var shape = (ConsoleShape2D)shapes2D[(int)Shape];
                var messageLines = Message.Split('\n');
                var len = messageLines.Length > 1 ? messageLines.LongestLineStrLen() : Message.Length;
                var horizontal = shape.GetHorizontalValue();
                var vertical = shape.GetVerticalValue();
                var hLen = (len.DivCeil(CALCULATIONS_PER_CHARACTER) * shape.GetHorizontalMultiplier()) + (len % 2 == 0 ? 0 : 1);
                var vLen = (messageLines.Length == 0 ? 1 : messageLines.Length.DivCeil(2)) * shape.GetVerticalMultiplier();
                var repeatedHorizontal = horizontal.RepeatString(hLen);
                var borderedMessage = string.Empty;
                var parallelVertical = vertical.Parallel(repeatedHorizontal.Length);
                var horizontalVerticalCombined = vertical + repeatedHorizontal + vertical;
                foreach (var item in messageLines)
                {
                    var newString = $"{" ".RepeatString(repeatedHorizontal.Length.DivFloor(2) - len.DivFloor(2))}{item}";
                    borderedMessage += $"{vertical}{newString}{" ".RepeatString(repeatedHorizontal.Length - newString.Length)}{vertical}\n";
                }
                var repeatedParallelVertical = parallelVertical.RepeatString(vLen.DivCeil(messageLines.Length), "\n");
                return $" {repeatedHorizontal}\n{repeatedParallelVertical}{borderedMessage}{parallelVertical.RepeatString(vLen.DivCeil(messageLines.Length) - 1, "\n")}{horizontalVerticalCombined}{EndL}";
            }

            else
            {
                throw new NotImplementedException("ConsoleShape3D has not yet been implemented.");
            }
        }

        /// <summary>
        /// Writes <paramref name="Shape"/> to the console using <paramref name="WriteFunc"/>.
        /// </summary>
        /// <param name="WriteFunc">Function to write string with.</param>
        /// <param name="Shape">Shape to write.</param>
        /// <param name="Message">Message in <paramref name="Shape"/></param>
        /// <param name="EndL">Line Ending string.</param>
        public static void WriteShape(Action<string> WriteFunc, ConsoleShapes Shape, string Message = "", string EndL = "\n")
        {
            WriteFunc.Invoke(GenerateShape(Shape, Message, EndL));
        }

        /// <summary>
        /// Writes <paramref name="Shape"/> to the console using <see cref="ConsoleManager.WriteLineColored(string, ConsoleColor)"/>.
        /// </summary>
        /// <param name="Color">Color to write with.</param>
        /// <param name="Shape">Shape to write.</param>
        /// <param name="Message">Message in <paramref name="Shape"/>.</param>
        /// <param name="EndL">Line Ending string.</param>
        public static void WriteShape(ConsoleColor Color, ConsoleShapes Shape, string Message = "", string EndL = "\n")
        {
            ConsoleManager.WriteLineColored(GenerateShape(Shape, Message, EndL), Color);
        }
    }

    /// <summary>
    /// All of the internal console shapes available.
    /// </summary>
    public enum ConsoleShapes
    {
        Rectangle = 0
    }

    /// <summary>
    /// Defines an entity that's a console shape containing specific functionalities.
    /// </summary>
    public interface IConsoleShape
    {
        string GetHorizontalValue();
        string GetVerticalValue();
        int GetHorizontalMultiplier();
        int GetVerticalMultiplier();
    }

    /// <summary>
    /// 2D Console Shape
    /// </summary>
    public struct ConsoleShape2D : IConsoleShape
    {
        private readonly string horizontalValue;
        private readonly string verticalValue;
        private readonly int horizontalMultiplier;
        private readonly int verticalMultiplier;

        /// <summary>
        /// 2D Console Shape
        /// </summary>
        /// <param name="HC">Horizontal Value</param>
        /// <param name="VC">Vertical Value</param>
        /// <param name="HCM">Horizontal Multiplier Per 5 Characters In Printing String (string.Len * HCM)</param>
        /// <param name="VCM">Vertical Multiplier Per 5 Characters In Printing String (string.Len * VCM)</param>
        public ConsoleShape2D(string HC, string VC, int HCM, int VCM)
        {
            horizontalValue = HC;
            verticalValue = VC;
            horizontalMultiplier = HCM;
            verticalMultiplier = VCM;
        }

        public int GetHorizontalMultiplier()
        {
            return horizontalMultiplier;
        }

        public string GetHorizontalValue()
        {
            return horizontalValue;
        }

        public int GetVerticalMultiplier()
        {
            return verticalMultiplier;
        }

        public string GetVerticalValue()
        {
            return verticalValue;
        }
    }

    /// <summary>
    /// 3D Console Shape
    /// </summary>
    public struct ConsoleShape3D : IConsoleShape
    {
        private readonly string horizontalValue;
        private readonly string verticalValue;
        private readonly string zValue;
        private readonly int horizontalMultiplier;
        private readonly int verticalMultiplier;
        private readonly int zMultiplier;

        /// <summary>
        /// 3D Console Shape
        /// </summary>
        /// <param name="HC">Horizontal Value</param>
        /// <param name="VC">Vertical Value</param>
        /// <param name="ZC">Z Axis Value</param>
        /// <param name="HCM">Horizontal Multiplier Per 5 Characters In Printing String (string.Len * HCM)</param>
        /// <param name="VCM">Vertical Multiplier Per 5 Characters In Printing String (string.Len * VCM)</param>
        /// <param name="ZCM">Z Axis Multiplier Per 5 Characters In Printing String (string.Len * ZCM)</param>
        public ConsoleShape3D(string HC, string VC, string ZC, int HCM, int VCM, int ZCM)
        {
            horizontalValue = HC;
            verticalValue = VC;
            zValue = ZC;
            horizontalMultiplier = HCM;
            verticalMultiplier = VCM;
            zMultiplier = ZCM;
        }

        public int GetHorizontalMultiplier()
        {
            return horizontalMultiplier;
        }

        public string GetHorizontalValue()
        {
            return horizontalValue;
        }

        public int GetVerticalMultiplier()
        {
            return verticalMultiplier;
        }

        public string GetVerticalValue()
        {
            return verticalValue;
        }

        public int GetZMultiplier()
        {
            return zMultiplier;
        }

        public string GetZValue()
        {
            return zValue;
        }
    }
}