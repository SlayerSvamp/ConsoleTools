using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleTools
{
    public class Selector<T> : InputToolBase<T>, ISelector
    {
        protected int index;
        public override T Selected
        {
            get { return Choices[Index]; }
            set
            {
                var newIndex = Choices.IndexOf(value);
                if (newIndex >= 0)
                {
                    Index = newIndex;
                }
            }
        }
        public List<T> Choices { get; private set; } = null;
        public int IndexCursorPosition { get { return ContentCursorTop + Choices.Select(c => GetPrintLines(DisplayFormat(c)).Count()).Sum(); } }
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
        public Dictionary<ConsoleKey, Action<ConsoleModifiers>> KeyActionDictionary { get; private set; }
        public ConsoleColor SelectedForegroundColor { get; set; } = ConsoleColor.White;
        public ConsoleColor SelectedBackgroundColor { get; set; } = ConsoleColor.Black;

        public Selector(IEnumerable<T> choices)
        {
            Choices = choices.ToList();
            if (choices == null)
            {
                throw new ArgumentException("Selector type constructor: argument 'choices' cannot be null");
            }

            var gHeader = $"Go to index (0-{Choices.Count - 1}):";
            var gErrorMessage = $"Index must be between 0 and {Choices.Count - 1}!";
            var gFooter = $"   {string.Join("\r\n   ", Choices.Select(DisplayFormat))}";
            KeyActionDictionary = new Dictionary<ConsoleKey, Action<ConsoleModifiers>>
            {
                { ConsoleKey.UpArrow,(m) => Index-- },
                { ConsoleKey.DownArrow, (m) => Index++ },
                { ConsoleKey.G, (m) => {
                    if(m != ConsoleModifiers.Control) return;
                    Index = (int)new IntegerInput((x) => x >= 0 && x < Choices.Count)
                        { Header = gHeader, ErrorMessage = gErrorMessage, Footer = gFooter }
                        .Select()
                        .ObjSelected;
                    }
                },
                { ConsoleKey.LeftWindows, (m) => { } },
                { ConsoleKey.RightWindows, (m) => { } },
            };
        }

        protected virtual string FormatChoice(T choice)
        {
            return DisplayFormat(choice);
        }
        protected virtual void PrintChoice(T choice, string formatted)
        {
        }
        protected override void PrintContent()
        {
            foreach (var choice in Choices)
            {
                bool isActive = Choices[Index].Equals(choice);
                var block = new ConsoleTextBlock($"{(isActive ? ">" : " ")}{FormatChoice(choice)}");
                if (isActive)
                {
                    block.ForegroundColor = SelectedForegroundColor;
                    block.BackgroundColor = SelectedBackgroundColor;
                }
                PrintSegment(block, false);
                Console.CursorTop--;
            }
            Console.CursorTop++;
        }
        public virtual IInputTool Select()
        {
            while (true)
            {
                PrintAll();
                Console.CursorTop = IndexCursorPosition;
                var input = Console.ReadKey(true);
                if (KeyActionDictionary.TryGetValue(input.Key, out var a))
                {
                    a(input.Modifiers);
                }
                else
                {
                    return this;
                }
            }
        }
    }
    public class InputToolSelector<T> : Selector<T> where T : IInputTool
    {
        public InputToolSelector(IEnumerable<T> choices) : base(choices)
        {
            DisplayFormat = (value) => value.Title;
        }
    }
    public class EnumSelector<T> : Selector<T>, IEnumSelector<T> where T : struct, IComparable, IConvertible, IFormattable
    {
        public EnumSelector() : this(x => true)
        {
        }
        public EnumSelector(Func<T, bool> filter) : base(Enum.GetValues(typeof(T)).Cast<T>().Where(filter))
        {
            DisplayFormat = (value) => Regex.Replace(value.ToString(), @"([a-zåäö])([A-ZÅÄÖ])", m => $"{m.ToString()[0]} {m.ToString()[1]}".ToLower());
        }
    }
    public abstract class FlagSelectorBase<T, U> : EnumSelector<T>, IFlagSelector<T> where T : struct, IComparable, IConvertible, IFormattable
    {
        public Action AfterToggle { get; set; } = () => { };
        public U TotalFlagValue { get; protected set; }
        public override T Selected { get { return (T)(dynamic)TotalFlagValue; } }
        public FlagSelectorBase()
        {
            KeyActionDictionary.Add(ConsoleKey.Spacebar, (m) => { ToggleFlag(); AfterToggle(); });
        }

        protected bool IsSelected(T value)
        {
            return (Selected as Enum).HasFlag(value as Enum);
        }
        protected override string FormatChoice(T choice)
        {
            return $"{(IsSelected(choice) ? "»" : " ")}{DisplayFormat(choice)}";
        }
        protected abstract void ToggleFlag();
    }
    public static class FlagSelector
    {
        public static IFlagSelector<T> New<T>() where T : struct, IComparable, IConvertible, IFormattable
        {
            Type type = Enum.GetUnderlyingType(typeof(T));
            var typecode = Type.GetTypeCode(type);
            switch (typecode)
            {
                case TypeCode.SByte:
                    return new SByteFlagSelector<T>();
                case TypeCode.Byte:
                    return new ByteFlagSelector<T>();
                case TypeCode.Int16:
                    return new Int16FlagSelector<T>();
                case TypeCode.UInt16:
                    return new UInt16FlagSelector<T>();
                case TypeCode.Int32:
                    return new Int32FlagSelector<T>();
                case TypeCode.UInt32:
                    return new UInt32FlagSelector<T>();
                case TypeCode.Int64:
                    return new Int64FlagSelector<T>();
                case TypeCode.UInt64:
                    return new UInt64FlagSelector<T>();
                default:
                    throw new Exception("WTF!?");
            }
        }
    }
    #region FlagSelectorAlternatives
    public class SByteFlagSelector<T> : FlagSelectorBase<T, sbyte> where T : struct, IComparable, IConvertible, IFormattable
    {
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
