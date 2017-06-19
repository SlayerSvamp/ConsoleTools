using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTools
{
    abstract public class InputToolBase<T> : IInputTool<T>
    {
        protected IEnumerable<string> ContentParts { get; set; }
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
        public Splash HeaderColors { get; set; } = new Splash();
        public Splash ErrorMessageColors { get; set; } = new Splash { ForegroundColor = ConsoleColor.Red };
        public Splash InputColors { get; set; } = new Splash { ForegroundColor = ConsoleColor.White };
        public Splash FooterColors { get; set; } = new Splash();
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
        protected int PrintSegment(Splash colors, string value, bool padded = true)
        {
            if (value.Length == 0) return 0;
            var lines = GetPrintLines(value).ToList();
            foreach (var line in lines)
            {
                Console.CursorLeft = Indent;
                colors.Write(line);
                Console.CursorTop++;
            }
            Console.CursorTop++;
            return lines.Count;
        }
        protected void PrintHead()
        {
            Console.CursorTop = 1;
            PrintSegment(HeaderColors, Header);
        }
        protected void PrintErrorMessage()
        {
            if (HasError) PrintSegment(ErrorMessageColors, ErrorMessage);
        }
        public abstract IInputTool Activate();
        protected abstract void PrintContent();
        protected void PrintFooter()
        {
            PrintSegment(FooterColors, Footer);
        }
        protected void PrintAll()
        {
            Console.Clear();
            PrintHead();
            PrintErrorMessage();
            ContentCursorTop = Console.CursorTop;
            PrintContent();
            PrintFooter();
        }
    }
}
