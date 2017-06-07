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
    public abstract class InputBase<T> : ITextInput<T>
    {
        public T Selected { get; set; }
        public object ObjSelected { get { return Selected; } }
        public string OutputString
        {
            get
            {
                if (Selected == null) return string.Empty;
                return $"{Selected}";
            }
        }
        public string Title { get; set; }
        public string InputMessage { get; set; }
        public string ErrorMessage { get; set; }
        public ConsoleColor ErrorForegroundColor { get; set; } = ConsoleColor.Red;
        public ConsoleColor ErrorBackgroundColor { get; set; } = ConsoleColor.Black;
        public InputBase(string title, string inputMessage, string errorMessage)
        {
            Title = title;
            ErrorMessage = errorMessage;
            InputMessage = inputMessage;
        }
        protected Func<T, bool> Predicate { get; set; }
        protected Func<string, T> Converter { get; set; }
        void WriteErrorMessage()
        {
            var currentBGColor = Console.BackgroundColor;
            var currentFGColor = Console.ForegroundColor;
            Console.BackgroundColor = ErrorBackgroundColor;
            Console.ForegroundColor = ErrorForegroundColor;
            Console.Write(ErrorMessage);
            Console.BackgroundColor = currentBGColor;
            Console.ForegroundColor = currentFGColor;
        }
        public IInputTool Select()
        {
            var input = OutputString;
            bool showErrorMessage = false;
            T selected;
            while (true)
            {
                Console.Clear();
                if (showErrorMessage)
                {
                    Console.CursorTop = 5;
                    Console.CursorLeft = 3;
                    WriteErrorMessage();
                }
                else showErrorMessage = true;

                Console.CursorTop = 1;
                Console.CursorLeft = 3;
                Console.WriteLine(InputMessage);
                Console.CursorLeft = 3;
                System.Windows.Forms.SendKeys.SendWait(OutputString);
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
            }
            Selected = Converter(input);
            return this;
        }
    }
    public class CustomInput<T> : InputBase<T>, ITextInput<T>
    {
        public CustomInput(string title, string inputMessage, string errorMessage, Func<T, bool> predicate, Func<string, T> converter) : base(title, inputMessage, errorMessage)
        {
            Predicate = predicate;
            Converter = converter;
        }
    }
    public class IntegerInput : InputBase<int>, ITextInput<int>
    {
        public IntegerInput(string title, string inputMessage, string errorMessage, Func<int, bool> predicate) : base(title, inputMessage, errorMessage)
        {
            Predicate = predicate;
            Converter = (input) => Int32.Parse(input);
        }
    }
    public class DoubleInput : InputBase<double>, ITextInput<double>
    {
        public DoubleInput(string title, string inputMessage, string errorMessage, Func<double, bool> predicate) : base(title, inputMessage, errorMessage)
        {
            Predicate = predicate;
            Converter = (input) => Double.Parse(input);
        }
    }
    public class TextInput : InputBase<string>, ITextInput<string>
    {
        public TextInput(string title, string inputMessage, string errorMessage, Func<string, bool> predicate) : base(title, inputMessage, errorMessage)
        {
            Predicate = predicate;
            Converter = (input) => input;
        }
    }
    public class RegexInput : InputBase<string>, ITextInput<string>
    {
        public string Pattern { get; set; }
        public RegexInput(string title, string inputMessage, string errorMessage, string pattern) : base(title, inputMessage, errorMessage)
        {
            Pattern = pattern;
            Predicate = (input) => Regex.IsMatch(input, pattern);
            Converter = (input) => input;
        }
    }
}
