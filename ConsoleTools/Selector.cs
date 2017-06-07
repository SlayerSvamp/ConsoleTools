using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleTools
{
    public class Selector<T> : ISelector
    {
        protected int index;
        IEnumerable<T> choices = null;
        public string Title { get; set; }
        public string InputMessage { get; set; }
        public virtual T Selected { get { return Choices[Index]; } }
        public virtual object ObjSelected { get { return Selected; } }
        public string OutputString { get { return DisplayFormat(Selected); } }
        public Func<T, string> DisplayFormat { get; set; } = (selected) => selected.ToString();
        public List<T> Choices{get{ return choices.Where(Filter).ToList(); }}
        public Func<T, bool> Filter { get; set; } = (x) => true;
        public int Index
        {
            get { return index; }
            set
            {
                index = value;
                while (index < 0) index += Choices.Count;
                index %= Choices.Count;
            }
        }
        public ConsoleColor FooterForegroundColor { get; set; } = ConsoleColor.Gray;
        public ConsoleColor FooterBackgroundColor { get; set; } = ConsoleColor.Black;
        public ConsoleColor SelectedForegroundColor { get; set; } = ConsoleColor.White;
        public ConsoleColor SelectedBackgroundColor { get; set; } = ConsoleColor.Black;
        public string Footer { get; set; } = "";

        public Selector(string title, string inputMessage, IEnumerable<T> choices)
        {
            if (choices == null)
            {
                throw new ArgumentException("Selector type constructor: argument 'choices' cannot be null");
            }

            if (title.Length > 79) title = title.Substring(0, 79);
            Title = title;
            InputMessage = inputMessage;

            this.choices = choices.ToList();
        }

        protected void SetCursor()
        {
            Console.CursorLeft = 1;
            Console.CursorTop = 3 + Index;
            Console.Write(">");
            Console.CursorLeft = 1;
        }
        protected virtual string FormatChoice(T choice)
        {
            return $"   {DisplayFormat(choice)}";
        }
        protected virtual void PrintChoice(T choice, string formatted)
        {
            bool isActive = Choices[Index].Equals(choice);
            ConsoleColor fgcolor = Console.ForegroundColor;
            ConsoleColor bgcolor = Console.BackgroundColor;

            if (isActive)
            {
                Console.ForegroundColor = SelectedForegroundColor;
                Console.BackgroundColor = SelectedBackgroundColor;
            }
            Console.WriteLine(formatted);
            if (isActive)
            {
                Console.ForegroundColor = fgcolor;
                Console.BackgroundColor = bgcolor;
            }
        }
        protected void PrintFooter()
        {
            if (Footer != "")
            {
                var fgcolor = Console.ForegroundColor;
                var bgcolor = Console.BackgroundColor;
                Console.ForegroundColor = FooterForegroundColor;
                Console.BackgroundColor = FooterBackgroundColor;
                Console.WriteLine(Footer);
                Console.ForegroundColor = fgcolor;
                Console.BackgroundColor = bgcolor;
            }
        }
        protected void PrintChoices(bool includeFooter = true)
        {
            Console.Clear();
            Console.WriteLine();
            Console.WriteLine($"   {InputMessage}");
            Console.WriteLine();
            foreach (var choice in Choices)
            {
                var text = FormatChoice(choice);
                PrintChoice(choice, text);
            }
            Console.WriteLine();

            if (includeFooter) PrintFooter();
        }
        public virtual IInputTool Select()
        {
            while (true)
            {
                PrintChoices();
                SetCursor();
                switch ((int)Console.ReadKey(true).Key)
                {
                    case (int)ConsoleKey.UpArrow:
                        Index--;
                        goto case -1;
                    case (int)ConsoleKey.DownArrow:
                        Index++;
                        goto case -1;
                    case -1:
                        Console.Write(" ");
                        SetCursor();
                        continue;
                    default:
                        Console.Clear();
                        return this;
                }
            }
        }
    }
    public class InputToolSelector<T> : Selector<T> where T : IInputTool
    {
        public InputToolSelector(string title, string inputMessage, IEnumerable<T> choices) : base(title, inputMessage, choices)
        {
            DisplayFormat = (value) => value.Title;
        }
    }
    public class EnumSelector<T> : Selector<T>, IEnumSelector<T> where T : struct, IComparable, IConvertible, IFormattable
    {
        public EnumSelector(string title, string inputMessage) : base(title, inputMessage, Enum.GetValues(typeof(T)).Cast<T>())
        {
            DisplayFormat = (value) => Regex.Replace(value.ToString(), @"([a-zåäö])([A-ZÅÄÖ])", m => $"{m.ToString()[0]} {m.ToString()[1]}".ToLower());
        }
    }
    public abstract class FlagSelectorBase<T, U> : EnumSelector<T>, IFlagSelector<T> where T : struct, IComparable, IConvertible, IFormattable
    {
        public Action AfterToggle { get; set; } = () => { };
        public U TotalFlagValue { get; protected set; }
        public override T Selected { get { return (T)(dynamic)TotalFlagValue; } }
        public FlagSelectorBase(string title, string inputMessage) : base(title, inputMessage)
        {
        }

        protected bool IsSelected(T value)
        {
            return (Selected as Enum).HasFlag(value as Enum);
        }
        protected override string FormatChoice(T choice)
        {
            return $"  {(IsSelected(choice) ? "»" : " ")} {DisplayFormat(choice)}";
        }
        protected abstract void ToggleFlag();

        public override IInputTool Select()
        {
            while (true)
            {
                PrintChoices();
                SetCursor();
                switch ((int)Console.ReadKey(true).Key)
                {
                    case (int)ConsoleKey.UpArrow:
                        Index--;
                        goto case -1;
                    case (int)ConsoleKey.DownArrow:
                        Index++;
                        goto case -1;
                    case -1:
                        Console.Write(" ");
                        SetCursor();
                        break;
                    case (int)ConsoleKey.Spacebar:
                        ToggleFlag();
                        AfterToggle();
                        break;
                    default:
                        Console.Clear();
                        return this;
                }
            }
        }
    }
    public static class FlagSelector
    {
        public static IFlagSelector<T> New<T>(string title, string inputMessage) where T : struct, IComparable, IConvertible, IFormattable
        {
            Type type = Enum.GetUnderlyingType(typeof(T));
            var typecode = Type.GetTypeCode(type);
            switch (typecode)
            {
                case TypeCode.SByte:
                    return new SByteFlagSelector<T>(title, inputMessage);
                case TypeCode.Byte:
                    return new ByteFlagSelector<T>(title, inputMessage);
                case TypeCode.Int16:
                    return new Int16FlagSelector<T>(title, inputMessage);
                case TypeCode.UInt16:
                    return new UInt16FlagSelector<T>(title, inputMessage);
                case TypeCode.Int32:
                    return new Int32FlagSelector<T>(title, inputMessage);
                case TypeCode.UInt32:
                    return new UInt32FlagSelector<T>(title, inputMessage);
                case TypeCode.Int64:
                    return new Int64FlagSelector<T>(title, inputMessage);
                case TypeCode.UInt64:
                    return new UInt64FlagSelector<T>(title, inputMessage);
                default:
                    throw new Exception("WTF!?");
            }
        }
    }
    #region FlagSelectorAlternatives
    public class SByteFlagSelector<T> : FlagSelectorBase<T, sbyte> where T : struct, IComparable, IConvertible, IFormattable
    {
        public SByteFlagSelector(string title, string inputMessage) : base(title, inputMessage)
        {
        }

        protected override void ToggleFlag()
        {
            sbyte val = (sbyte)Convert.ChangeType(Choices[Index], typeof(sbyte));
            if (IsSelected(Choices[Index]))
                TotalFlagValue &= (sbyte)~val;
            else TotalFlagValue |= val;
        }
    }
    public class ByteFlagSelector<T> : FlagSelectorBase<T, byte> where T : struct, IComparable, IConvertible, IFormattable
    {
        public ByteFlagSelector(string title, string inputMessage) : base(title, inputMessage)
        {
        }

        protected override void ToggleFlag()
        {
            byte val = (byte)Convert.ChangeType(Choices[Index], typeof(byte));
            if (IsSelected(Choices[Index]))
                TotalFlagValue &= (byte)~val;
            else TotalFlagValue |= val;
        }
    }
    public class Int16FlagSelector<T> : FlagSelectorBase<T, short> where T : struct, IComparable, IConvertible, IFormattable
    {
        public Int16FlagSelector(string title, string inputMessage) : base(title, inputMessage)
        {
        }

        protected override void ToggleFlag()
        {
            short val = (short)Convert.ChangeType(Choices[Index], typeof(short));
            if (IsSelected(Choices[Index]))
                TotalFlagValue &= (short)~val;
            else TotalFlagValue |= val;
        }
    }
    public class UInt16FlagSelector<T> : FlagSelectorBase<T, ushort> where T : struct, IComparable, IConvertible, IFormattable
    {
        public UInt16FlagSelector(string title, string inputMessage) : base(title, inputMessage)
        {
        }

        protected override void ToggleFlag()
        {
            ushort val = (ushort)Convert.ChangeType(Choices[Index], typeof(ushort));
            if (IsSelected(Choices[Index]))
                TotalFlagValue &= (ushort)~val;
            else TotalFlagValue |= val;
        }
    }
    public class Int32FlagSelector<T> : FlagSelectorBase<T, int> where T : struct, IComparable, IConvertible, IFormattable
    {
        public Int32FlagSelector(string title, string inputMessage) : base(title, inputMessage)
        {
        }

        protected override void ToggleFlag()
        {
            int val = (int)Convert.ChangeType(Choices[Index], typeof(int));
            if (IsSelected(Choices[Index]))
                TotalFlagValue &= ~val;
            else TotalFlagValue |= val;
        }
    }
    public class UInt32FlagSelector<T> : FlagSelectorBase<T, uint> where T : struct, IComparable, IConvertible, IFormattable
    {
        public UInt32FlagSelector(string title, string inputMessage) : base(title, inputMessage)
        {
        }

        protected override void ToggleFlag()
        {
            uint val = (uint)Convert.ChangeType(Choices[Index], typeof(uint));
            if (IsSelected(Choices[Index]))
                TotalFlagValue &= ~val;
            else TotalFlagValue |= val;
        }
    }
    public class Int64FlagSelector<T> : FlagSelectorBase<T, long> where T : struct, IComparable, IConvertible, IFormattable
    {
        public Int64FlagSelector(string title, string inputMessage) : base(title, inputMessage)
        {
        }

        protected override void ToggleFlag()
        {
            long val = (long)Convert.ChangeType(Choices[Index], typeof(long));
            if (IsSelected(Choices[Index]))
                TotalFlagValue &= ~val;
            else TotalFlagValue |= val;
        }
    }
    public class UInt64FlagSelector<T> : FlagSelectorBase<T, ulong> where T : struct, IComparable, IConvertible, IFormattable
    {
        public UInt64FlagSelector(string title, string inputMessage) : base(title, inputMessage)
        {
        }

        protected override void ToggleFlag()
        {
            ulong val = (ulong)Convert.ChangeType(Choices[Index], typeof(ulong));
            if (IsSelected(Choices[Index]))
                TotalFlagValue &= ~val;
            else TotalFlagValue |= val;
        }
    }
    #endregion
}
