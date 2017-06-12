using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleTools
{
    enum InputFormat
    {
        String
    }
    public abstract class TextInputBase<T> : InputToolBase<T>, ITextInput<T>
    {
        protected Func<T, bool> Predicate { get; set; }
        protected Func<string, T> Converter { get; set; }
        public IInputTool Select()
        {
            PreSelectTrigger(Selected);
            var input = OutputString;
            T selected;
            while (true)
            {
                PrintAll();
                Console.CursorLeft = Indent;
                Console.CursorTop = ContentCursorTop;
                System.Windows.Forms.SendKeys.SendWait(input);
                input = Console.ReadLine();
                try
                {
                    selected = Converter(input);
                    if (Predicate(selected))
                    {
                        break;
                    }
                }
                catch {/*empty*/}
                HasError = true;
            }
            Selected = Converter(input);
            HasError = false;
            PostSelectTrigger(Selected);
            return this;
        }
        protected override void PrintContent()
        {
            Console.CursorTop += 3;
        }
    }
    public class CustomInput<T> : TextInputBase<T>, ITextInput<T>
    {
        public CustomInput(Func<T, bool> predicate, Func<string, T> converter)
        {
            Predicate = predicate;
            Converter = converter;
        }
    }
    public class IntegerInput : TextInputBase<int>, ITextInput<int>
    {
        public IntegerInput(Func<int, bool> predicate)
        {
            Predicate = predicate;
            Converter = (input) => Int32.Parse(input);
        }
    }
    public class DoubleInput : TextInputBase<double>, ITextInput<double>
    {
        public DoubleInput(Func<double, bool> predicate)
        {
            Predicate = predicate;
            Converter = (input) => Double.Parse(input);
        }
    }
    public class TextInput : TextInputBase<string>, ITextInput<string>
    {
        public TextInput(Func<string, bool> predicate)
        {
            Predicate = predicate;
            Converter = (input) => input;
        }
    }
    public class RegexInput : TextInputBase<string>, ITextInput<string>
    {
        public string Pattern { get; set; }
        public RegexInput(string pattern)
        {
            Pattern = pattern;
            Predicate = (input) => Regex.IsMatch(input, pattern);
            Converter = (input) => input;
        }
    }
}
