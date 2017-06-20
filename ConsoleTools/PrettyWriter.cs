using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTools
{
    public static class PrettyWriter
    {
        public static int Indent { get; set; } = 3;
        public static int MaxWriteLength { get { return Console.BufferWidth - Indent - 1; } }
        private static IEnumerable<string> GetPrintLines(string value)
        {
            var lines = value.Split('\n').Select(s => s.TrimEnd());

            Func<char, int, bool> shortEnough = (x, i) => i < MaxWriteLength;
            foreach (var line in lines)
            {
                var val = line;
                while (val.Length > 0)
                {
                    yield return string.Concat(val.TakeWhile(shortEnough));
                    val = string.Concat(val.SkipWhile(shortEnough));
                }
            }
        }

        public static int Write(Splash splash, string content) 
        {
            return Write(splash, new string[] { content });
        }
        public static int Write(Splash splash, IEnumerable<string> content)
        {
            var lines = content.Select(GetPrintLines).SelectMany(x => x).ToList();
            return Write(splash, lines);
        }
        private static int Write(Splash splash, List<string> lines)
        {
            foreach (var line in lines)
            {
                Console.CursorLeft = Indent;
                splash.Write(line);
                Console.CursorTop++;
            }
            Console.CursorLeft = Indent;
            Console.CursorTop++;
            return lines.Count;
        }
    }
}
