﻿using System;
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
        public override T Value
        {
            get { return Options[Index]; }
            set
            {
                var newIndex = Options.IndexOf(value);
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
        public virtual T PreviewValue
        {
            get { return Options[PreviewIndex]; }
            set
            {
                var newIndex = Options.IndexOf(value);
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
        public List<T> Options { get; private set; }
        public IEnumerable<object> ObjOptions { get { return Options.Cast<object>(); } }
        public int IndexCursorPosition { get { return ContentCursorTop + Options.Select(c => GetPrintLines(DisplayFormat(c)).Count()).TakeWhile((v, i) => i < Index).Sum(); } }
        public int PreviewIndexCursorPosition { get { return ContentCursorTop + Options.Select(c => GetPrintLines(DisplayFormat(c)).Count()).TakeWhile((v, i) => i < PreviewIndex).Sum(); } }
        public int Index
        {
            get { return index; }
            set
            {
                index = value;
                while (index < 0) index += Options.Count;
                index %= Options.Count;
            }
        }
        public int PreviewIndex
        {
            get { return previewIndex; }
            set
            {
                previewIndex = value;
                while (previewIndex < 0) previewIndex += Options.Count;
                previewIndex %= Options.Count;
            }
        }
        public Action<T> PreviewTrigger { get; set; } = (t) => { };
        public Action<T> CancelTrigger { get; set; } = (t) => { };
        public bool Cancel { get; set; } = false;
        public bool IsMenu { get; set; } = false;
        public Dictionary<ConsoleKey, Action<ConsoleModifiers>> KeyPressActions { get; private set; }

        public Selector(IEnumerable<T> choices)
        {
            Options = choices.ToList();
            if (choices == null)
            {
                throw new ArgumentException("Selector type constructor: argument 'choices' cannot be null");
            }

            var gHeader = $"Go to index (0-{Options.Count - 1}):";
            var gErrorMessage = $"Index must be between 0 and {Options.Count - 1}!";
            var gFooter = string.Join("\n", Options.Select((s, i) => $"{i}: {DisplayFormat(s)}"));
            KeyPressActions = new Dictionary<ConsoleKey, Action<ConsoleModifiers>>
            {
                { ConsoleKey.UpArrow,(m) => {PreviewIndex--; PreviewTrigger(PreviewValue); } },
                { ConsoleKey.DownArrow, (m) => {PreviewIndex++; PreviewTrigger(PreviewValue); } },
                {
                ConsoleKey.G, (m) =>
                {
                    if (m != ConsoleModifiers.Control) return;
                    PreviewIndex = (int)new IntegerInput(x => x >= 0 && x < Options.Count)
                    { Header = gHeader, ErrorMessage = gErrorMessage, Footer = gFooter }
                        .Activate()
                        .ObjValue;
                }},
                { ConsoleKey.LeftWindows, (m) => { } },
                { ConsoleKey.RightWindows, (m) => { } },
                { ConsoleKey.Escape, (m) => Cancel = true }
            };
            PreviewValue = Value;
        }

        protected virtual string FormatChoice(T choice)
        {
            return DisplayFormat(choice);
        }
        protected override void PrintContent()
        {
            foreach (var choice in Options)
            {
                bool isActive = Options[PreviewIndex].Equals(choice);
                var value = $"{(isActive ? ">" : " ")}{FormatChoice(choice)}";
                var colors = isActive ? InputSplash : ContentSplashSelector(choice);
                PrintSegment(colors, value);
            }
            BufferWriter.AddLine("");
        }
        protected virtual void PreActivate()
        {
            Cancel = false;
            PreActivateTrigger(Value);
        }
        protected virtual void PostActivate()
        {
            Value = PreviewValue;
            PostActivateTrigger(Value);
        }
        public override IInputTool Activate()
        {
            PreActivate();
            while (true)
            {
                if (Cancel && AllowCancel)
                {
                    CancelTrigger(Value);
                    if (Cancel && AllowCancel)
                    {
                        PreviewValue = Value;
                        return this;
                    }
                }
                PrintAll();
                Console.CursorTop = PreviewIndexCursorPosition;
                Console.CursorLeft = Indent;
                var input = Console.ReadKey(true);
                if (KeyPressActions.TryGetValue(input.Key, out var a))
                {
                    a(input.Modifiers);
                }
                else
                {
                    PostActivate();
                    if (IsMenu && !Cancel)
                    {
                        Activate();
                    }
                    return this;
                }
            }
        }
    }
    public class InputToolSelector<T> : Selector<T>, IInputToolSelector<T> where T : IInputTool
    {
        protected override void PostActivate()
        {
            base.PostActivate();
            Value.Activate();
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
    public class FlagSelector<T> : EnumSelector<T>, IFlagSelector<T> where T : struct, IComparable, IConvertible, IFormattable
    {
        public enum CompositeFlagToggleMode { AsGroup, XorMode }
        public Action<T> PostToggleTrigger { get; set; } = (t) => { };
        private dynamic TotalFlagValue { get; set; } = (dynamic)Convert.ChangeType(0, typeof(T).GetEnumUnderlyingType());
        private dynamic PreviewTotalFlagValue { get; set; } = (dynamic)Convert.ChangeType(0, typeof(T).GetEnumUnderlyingType());
        public override T Value { get { return (T)TotalFlagValue; } set { TotalFlagValue = value; } }
        public override T PreviewValue { get { return (T)PreviewTotalFlagValue; } set { PreviewTotalFlagValue = value; } }
        public FlagSelector()
        {
            KeyPressActions.Add(ConsoleKey.Spacebar, (m) => ToggleFlag());
        }

        protected override string FormatChoice(T choice)
        {
            var c = (PreviewValue as Enum).HasFlag(choice as Enum) ? "»" : " ";
            return $"{c}{DisplayFormat(choice)}";
        }
        protected void ToggleFlag()
        {
            var typeCode = Type.GetTypeCode(typeof(T).GetEnumUnderlyingType());
            dynamic newValue = Convert.ChangeType(PreviewTotalFlagValue, typeCode);
            newValue ^= (dynamic)Convert.ChangeType(base.PreviewValue, typeCode);
            PreviewTotalFlagValue = newValue;
            PostToggleTrigger(PreviewValue);
        }
    }
}
