using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTools
{
    public class ConsoleTextBlock
    {
        public string Text { get; set; }
        public ConsoleColor ForegroundColor { get; set; } = (ConsoleColor)(-1);
        public ConsoleColor BackgroundColor { get; set; } = (ConsoleColor)(-1);
        public ConsoleTextBlock(string text = "")
        {
            Text = text;
        }
        public void Write(string value = null)
        {
            value = value ?? Text;
            if ((int)ForegroundColor == -1 && (int)BackgroundColor == -1)
            {
                Console.Write(value);
                return;
            }
            var fg = Console.ForegroundColor;
            var bg = Console.BackgroundColor;

            var doFG = (int)ForegroundColor < 16 && ForegroundColor >= 0;
            var doBG = (int)BackgroundColor < 16 && BackgroundColor >= 0;

            if (doFG) Console.ForegroundColor = ForegroundColor;
            if (doBG) Console.BackgroundColor = BackgroundColor;

            Console.Write(value);

            if (doFG) Console.ForegroundColor = fg;
            if (doBG) Console.BackgroundColor = bg;
        }
    }
    abstract public class InputToolBase<T>
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
        public virtual T Selected { get; set; }
        public object ObjSelected { get { return Selected; } }
        public string Title { get { return TitleBlock.Text; } set { TitleBlock.Text = value; } }
        public string Header { get { return HeaderBlock.Text; } set { HeaderBlock.Text = value; } }
        public string Footer { get { return FooterBlock.Text; } set { FooterBlock.Text = value; } }
        public string ErrorMessage { get { return ErrorMessageBlock.Text; } set { ErrorMessageBlock.Text = value; } }
        public ConsoleTextBlock TitleBlock { get; set; } = new ConsoleTextBlock();
        public ConsoleTextBlock HeaderBlock { get; set; } = new ConsoleTextBlock();
        public ConsoleTextBlock FooterBlock { get; set; } = new ConsoleTextBlock();
        public ConsoleTextBlock ErrorMessageBlock { get; set; } = new ConsoleTextBlock() { ForegroundColor = ConsoleColor.Red };
        protected bool HasError { get; set; } = false;
        public Func<T, string> DisplayFormat { get; set; } = (selected) => selected.ToString();
        public string OutputString { get { return Selected != null ? DisplayFormat(Selected) : string.Empty; } }

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
        protected int PrintSegment(ConsoleTextBlock value, bool emptyLineAfter = true)
        {
            if (value.Text.Length == 0) return 0;
            var blocks = GetPrintLines(value.Text).ToList();
            foreach (var block in blocks)
            {
                Console.CursorLeft = Indent;
                value.Write(block);
                Console.CursorTop++;
            }
            Console.CursorTop++;
            return blocks.Count;
        }
        protected void PrintHead()
        {
            Console.CursorTop = 1;
            PrintSegment(HeaderBlock);
        }
        protected void PrintErrorMessage()
        {
            if (HasError) PrintSegment(ErrorMessageBlock);
        }
        protected abstract void PrintContent();
        protected void PrintFooter()
        {
            PrintSegment(FooterBlock);
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
