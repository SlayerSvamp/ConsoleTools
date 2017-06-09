using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleTools
{
    public class Selector<T> : InputToolBase<T>, ISelector<T>
    {
        protected int index;
        protected int previewIndex;
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
        public virtual T PreviewSelected
        {
            get { return Choices[PreviewIndex]; }
            set
            {
                var newIndex = Choices.IndexOf(value);
                if (newIndex >= 0)
                {
                    PreviewIndex = newIndex;
                }
            }
        }
        public List<T> Choices { get; private set; } = null;
        public int IndexCursorPosition { get { return ContentCursorTop + Choices.Select(c => GetPrintLines(DisplayFormat(c)).Count()).TakeWhile((v, i) => i < Index).Sum(); } }
        public int PreviewIndexCursorPosition { get { return ContentCursorTop + Choices.Select(c => GetPrintLines(DisplayFormat(c)).Count()).TakeWhile((v, i) => i < PreviewIndex).Sum(); } }
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
        public int PreviewIndex
        {
            get { return previewIndex; }
            set
            {
                previewIndex = value;
                while (previewIndex < 0) previewIndex += Choices.Count;
                previewIndex %= Choices.Count;
            }
        }
        public ColorWriter SelectedColors { get; set; } = new ColorWriter { ForegroundColor = ConsoleColor.White };
        public Action<T> PreviewTrigger { get; set; } = (t) => { };
        public bool StepOut { get; set; } = false;
        public Dictionary<ConsoleKey, Action<ConsoleModifiers>> KeyActionDictionary { get; private set; }

        public Selector(IEnumerable<T> choices)
        {
            Choices = choices.ToList();
            if (choices == null)
            {
                throw new ArgumentException("Selector type constructor: argument 'choices' cannot be null");
            }

            var gHeader = $"Go to index (0-{Choices.Count - 1}):";
            var gErrorMessage = $"Index must be between 0 and {Choices.Count - 1}!";
            var gFooter = string.Join("\n", Choices.Select((s, i) => $"{i}: {DisplayFormat(s)}"));
            KeyActionDictionary = new Dictionary<ConsoleKey, Action<ConsoleModifiers>>
            {
                { ConsoleKey.UpArrow,(m) => {PreviewIndex--; PreviewTrigger(PreviewSelected); } },
                { ConsoleKey.DownArrow, (m) => {PreviewIndex++; PreviewTrigger(PreviewSelected); } },
                {
                ConsoleKey.G, (m) =>
                {
                    if (m != ConsoleModifiers.Control) return;
                    PreviewIndex = (int)new IntegerInput(x => x >= 0 && x < Choices.Count)
                    { Header = gHeader, ErrorMessage = gErrorMessage, Footer = gFooter }
                        .Select()
                        .ObjSelected;
                }},
                { ConsoleKey.LeftWindows, (m) => { } },
                { ConsoleKey.RightWindows, (m) => { } },
                { ConsoleKey.Escape, (m) => StepOut = true }
            };


        }

        protected virtual string FormatChoice(T choice)
        {
            return DisplayFormat(choice);
        }
        protected override void PrintContent()
        {
            foreach (var choice in Choices)
            {
                bool isActive = Choices[PreviewIndex].Equals(choice);
                var value = $"{(isActive ? ">" : " ")}{FormatChoice(choice)}";
                var colors = isActive ? SelectedColors : new ColorWriter();
                PrintSegment(colors, value, false);
                Console.CursorTop--;
            }
            Console.CursorTop++;
        }
        protected virtual void PostSelect()
        {
            Selected = PreviewSelected;
        }
        protected virtual void PreSelect()
        {
            StepOut = false;
        }
        public IInputTool Select()
        {
            PreSelect();
            PreSelectTrigger(Selected);
            while (true)
            {
                PrintAll();
                Console.CursorTop = PreviewIndexCursorPosition;
                Console.CursorLeft = Indent;
                var input = Console.ReadKey(true);
                if (KeyActionDictionary.TryGetValue(input.Key, out var a))
                {
                    a(input.Modifiers);
                    if (StepOut)
                    {
                        PreviewSelected = Selected;
                        return this;
                    }
                }
                else
                {
                    PostSelect();
                    PostSelectTrigger(Selected);
                    return this;
                }
            }
        }
    }
    public class InputToolSelector<T> : Selector<T>, IInputToolSelector<T> where T : IInputTool
    {
        protected override void PostSelect()
        {
            base.PostSelect();
            Selected.Select();
            Select();
        }
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
        public U PreviewTotalFlagValue { get; protected set; }
        public override T Selected { get { return (T)(dynamic)TotalFlagValue; } }
        public override T PreviewSelected { get { return (T)(dynamic)PreviewTotalFlagValue; } }
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
            sbyte val = (sbyte)Convert.ChangeType(Choices[PreviewIndex], typeof(sbyte));
            if (IsSelected(Choices[PreviewIndex]))
                PreviewTotalFlagValue &= (sbyte)~val;
            else PreviewTotalFlagValue |= val;
        }
    }
    public class ByteFlagSelector<T> : FlagSelectorBase<T, byte> where T : struct, IComparable, IConvertible, IFormattable
    {
        protected override void ToggleFlag()
        {
            byte val = (byte)Convert.ChangeType(Choices[PreviewIndex], typeof(byte));
            if (IsSelected(Choices[PreviewIndex]))
                PreviewTotalFlagValue &= (byte)~val;
            else PreviewTotalFlagValue |= val;
        }
    }
    public class Int16FlagSelector<T> : FlagSelectorBase<T, short> where T : struct, IComparable, IConvertible, IFormattable
    {
        protected override void ToggleFlag()
        {
            short val = (short)Convert.ChangeType(Choices[PreviewIndex], typeof(short));
            if (IsSelected(Choices[PreviewIndex]))
                PreviewTotalFlagValue &= (short)~val;
            else PreviewTotalFlagValue |= val;
        }
    }
    public class UInt16FlagSelector<T> : FlagSelectorBase<T, ushort> where T : struct, IComparable, IConvertible, IFormattable
    {
        protected override void ToggleFlag()
        {
            ushort val = (ushort)Convert.ChangeType(Choices[PreviewIndex], typeof(ushort));
            if (IsSelected(Choices[PreviewIndex]))
                PreviewTotalFlagValue &= (ushort)~val;
            else PreviewTotalFlagValue |= val;
        }
    }
    public class Int32FlagSelector<T> : FlagSelectorBase<T, int> where T : struct, IComparable, IConvertible, IFormattable
    {
        protected override void ToggleFlag()
        {
            int val = (int)Convert.ChangeType(Choices[PreviewIndex], typeof(int));
            if (IsSelected(Choices[PreviewIndex]))
                PreviewTotalFlagValue &= ~val;
            else PreviewTotalFlagValue |= val;
        }
    }
    public class UInt32FlagSelector<T> : FlagSelectorBase<T, uint> where T : struct, IComparable, IConvertible, IFormattable
    {
        protected override void ToggleFlag()
        {
            uint val = (uint)Convert.ChangeType(Choices[PreviewIndex], typeof(uint));
            if (IsSelected(Choices[PreviewIndex]))
                PreviewTotalFlagValue &= ~val;
            else PreviewTotalFlagValue |= val;
        }
    }
    public class Int64FlagSelector<T> : FlagSelectorBase<T, long> where T : struct, IComparable, IConvertible, IFormattable
    {
        protected override void ToggleFlag()
        {
            long val = (long)Convert.ChangeType(Choices[PreviewIndex], typeof(long));
            if (IsSelected(Choices[PreviewIndex]))
                PreviewTotalFlagValue &= ~val;
            else PreviewTotalFlagValue |= val;
        }
    }
    public class UInt64FlagSelector<T> : FlagSelectorBase<T, ulong> where T : struct, IComparable, IConvertible, IFormattable
    {
        protected override void ToggleFlag()
        {
            ulong val = (ulong)Convert.ChangeType(Choices[PreviewIndex], typeof(ulong));
            if (IsSelected(Choices[PreviewIndex]))
                PreviewTotalFlagValue &= ~val;
            else PreviewTotalFlagValue |= val;
        }
    }
    #endregion
}
