using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTools
{
    public class Splash
    {
        public const ConsoleColor NoColor = (ConsoleColor) (-1);
        public static Action<string> Writer { get; set; } = Console.Write;
        public ConsoleColor ForegroundColor { get; set; } = NoColor;
        public ConsoleColor BackgroundColor { get; set; } = NoColor;
        public bool HasFColor
        {
            get { return Enum.IsDefined(typeof(ConsoleColor), ForegroundColor); }
            set { ForegroundColor = (value) ? Console.ForegroundColor : NoColor; }
        }
        public bool HasBColor
        {
            get { return Enum.IsDefined(typeof(ConsoleColor), BackgroundColor); }
            set { BackgroundColor = (value) ? Console.BackgroundColor : NoColor; }
        }
        public void Write(string value)
        {
            Act(() => Writer(value));
        }
        public void Act(Action act)
        {
            if (ForegroundColor == NoColor && BackgroundColor == NoColor)
            {
                act();
                return;
            }
            var fg = Console.ForegroundColor;
            var bg = Console.BackgroundColor;

            var doFG = (int)ForegroundColor < 16 && ForegroundColor >= 0;
            var doBG = (int)BackgroundColor < 16 && BackgroundColor >= 0;

            if (doFG) Console.ForegroundColor = ForegroundColor;
            if (doBG) Console.BackgroundColor = BackgroundColor;

            act();

            if (doFG) Console.ForegroundColor = fg;
            if (doBG) Console.BackgroundColor = bg;
        }
    }
}
