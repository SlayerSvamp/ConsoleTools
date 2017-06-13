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
        public bool AllowCancel { get; set; } = true;
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
                else
                {
                    throw new ArgumentException($"Value '{value}' does not exist in given ISelector list");
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
                else
                {
                    throw new ArgumentException("Value does not exist in given ISelector list");
                }
            }
        }
        public List<T> Choices { get; private set; }
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
        public Action<T> CancelTrigger { get; set; } = (t) => { };
        public bool Cancel { get; set; } = false;
        public bool IsMenu { get; set; } = false;
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
                { ConsoleKey.Escape, (m) => Cancel = true }
            };
            PreviewSelected = Selected;
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
            PostSelectTrigger(Selected);
        }
        protected virtual void PreSelect()
        {
            Cancel = false;
            PreSelectTrigger(Selected);
        }
        public override IInputTool Select()
        {
            PreSelect();
            while (true)
            {
                if (Cancel && AllowCancel)
                {
                    CancelTrigger(Selected);
                    if (Cancel && AllowCancel)
                    {
                        PreviewSelected = Selected;
                        return this;
                    }
                }
                PrintAll();
                Console.CursorTop = PreviewIndexCursorPosition;
                Console.CursorLeft = Indent;
                var input = Console.ReadKey(true);
                if (KeyActionDictionary.TryGetValue(input.Key, out var a))
                {
                    a(input.Modifiers);
                }
                else
                {
                    PostSelect();
                    if ((IsMenu && !Cancel) || !AllowCancel)
                    {
                        Select();
                    }
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
        }
        public InputToolSelector(IEnumerable<T> choices) : base(choices)
        {
            IsMenu = true;
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
        public Action<T> AfterToggle { get; set; } = (t) => { };
        public U TotalFlagValue { get; protected set; }
        public U PreviewTotalFlagValue { get; protected set; }
        public override T Selected { get { return (T)(dynamic)TotalFlagValue; } set { TotalFlagValue = (U)Convert.ChangeType(value, (typeof(U))); } }
        public override T PreviewSelected { get { return (T)(dynamic)PreviewTotalFlagValue; } set { PreviewTotalFlagValue = (U)Convert.ChangeType(value, (typeof(U))); } }
        protected FlagSelectorBase()
        {
            KeyActionDictionary.Add(ConsoleKey.Spacebar, (m) => { ToggleFlag(); AfterToggle(PreviewSelected); });
        }

        protected bool IsSelected(T value)
        {
            return (PreviewSelected as Enum).HasFlag(value as Enum);
        }
        protected override string FormatChoice(T choice)
        {
            return $"{(IsSelected(choice) ? "»" : " ")}{DisplayFormat(choice)}";
        }
        protected abstract void ToggleFlag();
    }
    public static class FlagSelector
    {
        public static IFlagSelector<T> New<T>(string Title = "", string Header = "", string ErrorMessage = "", string Footer = "") where T : struct, IComparable, IConvertible, IFormattable
        {
            Type underying = Enum.GetUnderlyingType(typeof(T));
            var typecode = Type.GetTypeCode(underying);
            Type type = null;
            switch (typecode)
            {
                case TypeCode.SByte: type = typeof(SByteFlagSelector<T>); break;
                case TypeCode.Byte: type = typeof(ByteFlagSelector<T>); break;
                case TypeCode.Int16: type = typeof(Int16FlagSelector<T>); break;
                case TypeCode.UInt16: type = typeof(UInt16FlagSelector<T>); break;
                case TypeCode.Int32: type = typeof(Int32FlagSelector<T>); break;
                case TypeCode.UInt32: type = typeof(UInt32FlagSelector<T>); break;
                case TypeCode.Int64: type = typeof(Int64FlagSelector<T>); break;
                case TypeCode.UInt64: type = typeof(UInt64FlagSelector<T>); break;
            }
            var res = (IFlagSelector<T>)Activator.CreateInstance(type);
            res.Title = Title;
            res.Header = Header;
            res.ErrorMessage = ErrorMessage;
            res.Footer = Footer;
            return res;
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
