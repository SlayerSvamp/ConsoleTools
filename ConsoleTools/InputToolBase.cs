using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTools
{
    public class BufferChar
    {
        public Splash Splash { get; set; } = new Splash();
        public char Char { get; set; } = ' ';
    }
    public static class BufferWriter
    {
        private static int LastBufferHeight { get; set; } = 0;
        public static List<IEnumerable<BufferChar>> ScreenBuffer { get; set; } = new List<IEnumerable<BufferChar>>();
        public static void AddLine(string line, Splash splash = null)
        {
            splash = splash ?? new Splash();
            var chars = line.Select(x =>
                new BufferChar
                {
                    Char = x,
                    Splash = splash
                });
            ScreenBuffer.Add(chars);
        }
        public static void Write()
        {
            Console.CursorTop = 0;
            Console.CursorLeft = 0;
            foreach (var bufferRow in ScreenBuffer)
            {
                foreach (var buffer in bufferRow)
                    buffer.Splash.Act(() => Console.Write(buffer.Char));

                while (Console.BufferWidth > Console.CursorLeft + 1)
                    Console.Write(' ');

                Console.CursorLeft = 0;
                Console.CursorTop++;
            }

            var val = LastBufferHeight - Console.CursorTop;
            LastBufferHeight = Console.CursorTop;
            for (int i = 0; i <= val; i++)
            {
                Console.WriteLine(new string(' ', Console.BufferWidth - 1));
            }

        }
    }

    abstract public class InputToolBase<T> : IInputTool<T>
    {
        protected int ContentCursorTop { get; set; }
        public int Indent { get; set; } = 3;
        public int MaxWriteLength
        {
            get { return Console.BufferWidth - Indent - 1; }
            set
            {
                if (value < 1)
                    throw new ArgumentException("InputToolBase.MaxWriteLength cannot be lower than 1");

                var candidate = value + Indent + 1;
                if (candidate >= Int16.MaxValue)
                    throw new ArgumentException($"Trying to set MaxWriteLength too high. Console.BufferWidth == (MaxWriteLength + Indent + 1). Console.BufferWidth must be lower than Int16.MaxValue");
                if (candidate < 15)
                    throw new ArgumentException("Console.BufferWidth == (MaxWriteLength + Indent + 1). Console.BufferWidth in not allowed to be lower than 15");
                else
                    Console.BufferWidth = candidate;

            }
        }
        public virtual T Value { get; set; }
        public object ObjValue { get { return Value; } }
        public string Title { get; set; } = "";
        public string Header { get; set; } = "";
        public string Footer { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public Splash HeaderSplash { get; set; } = new Splash();
        public Splash ErrorMessageSplash { get; set; } = new Splash { ForegroundColor = ConsoleColor.Red };
        public Splash InputSplash { get; set; } = new Splash { ForegroundColor = ConsoleColor.White };
        public Splash FooterSplash { get; set; } = new Splash();
        public bool HasError { get; set; } = false;
        public Func<T, string> DisplayFormat { get; set; } = (selected) => selected.ToString();
        public string ValueAsString { get { return Value != null ? DisplayFormat(Value) : string.Empty; } }
        public Action<T> PreActivateTrigger { get; set; } = (t) => { };
        public Action<T> PostActivateTrigger { get; set; } = (t) => { };
        public Func<T, Splash> ContentSplashSelector { get; set; } = (x) => new Splash();
        protected IEnumerable<string> GetPrintLines(string value)
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
        protected void PrintSegment(Splash colors, string value)
        {
            var lines = GetPrintLines(value).ToList();

            foreach (var line in lines)
            {
                var chars = $"{new string(' ', Indent)}{line}"
                            .Select(x => new BufferChar()
                            {
                                Splash = colors,
                                Char = x
                            });
                BufferWriter.ScreenBuffer.Add(chars);
            }
        }
        protected void PrintHead()
        {
            BufferWriter.AddLine("");
            PrintSegment(HeaderSplash, Header);
            BufferWriter.AddLine("");
        }
        protected void PrintErrorMessage()
        {
            if (HasError)
            {
                PrintSegment(ErrorMessageSplash, ErrorMessage);
                BufferWriter.AddLine("");
            }
        }
        public abstract IInputTool Activate();
        protected abstract void PrintContent();
        protected void PrintFooter()
        {
            BufferWriter.AddLine("");
            PrintSegment(FooterSplash, Footer);
        }
        protected void PrintAll()
        {
            BufferWriter.ScreenBuffer.Clear();
            PrintHead();
            PrintErrorMessage();
            ContentCursorTop = BufferWriter.ScreenBuffer.Count;
            PrintContent();
            PrintFooter();
            BufferWriter.Write();
        }
    }
}
