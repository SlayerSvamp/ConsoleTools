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
        public override IInputTool Activate()
        {
            PreActivateTrigger(Value);
            var input = ValueAsString;
            T selected;
            while (true)
            {
                PrintAll();
                Console.CursorLeft = Indent;
                Console.CursorTop = ContentCursorTop;
                InputSplash.Act(() =>
                {
                    try
                    {
                        System.Windows.Forms.SendKeys.SendWait(input);
                    }
                    catch
                    {
                        foreach (var c in input)
                            try { System.Windows.Forms.SendKeys.SendWait(c.ToString()); }
                            catch { /***** swollow everything! *****/ }
                    }
                    input = Console.ReadLine();
                });
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
            Value = Converter(input);
            HasError = false;
            PostActivateTrigger(Value);
            return this;
        }
        protected override void PrintContent()
        {
            for (int i = 0; i < 3; i++)
                BufferWriter.AddLine("");
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
