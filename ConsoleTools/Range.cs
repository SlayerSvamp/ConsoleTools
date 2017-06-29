using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTools
{
    public abstract class RangeBase<T> : InputToolBase<T>, IRange<T> where T : struct, IComparable, IComparable<T>, IConvertible, IEquatable<T>, IFormattable
    {
        int slideWidth = 0;
        public bool AllowCancel { get; set; } = true;
        public Action<T> CancelTrigger { get; set; } = (t) => { };
        public bool Cancel { get; set; } = false;
        public T PreviewValue { get; set; }
        public Action<T> PreviewTrigger { get; set; } = (t) => { };
        public double SlideValueWidth { get { return GetRangeDisplayPart(PreviewValue); } }
        public int SlideWidth
        {
            get
            {
                return slideWidth <= 0
                    ? Math.Min(Range.DefaultSlideWidth, MaxWriteLength)
                    : Math.Min(MaxWriteLength, slideWidth);
            }
            set { slideWidth = value; }
        }
        public T MinRangeValue { get; set; }
        public T MaxRangeValue { get; set; }
        public abstract T RangeSize { get; }
        private ConsoleModifiers IncrementModifiers { get; set; }
        private T _increment;
        public T Increment
        {
            get
            {
                if (IncrementByModifiers.TryGetValue(IncrementModifiers, out T value))
                    return value;
                return _increment;
            }
            set { _increment = value; }
        }
        public Dictionary<ConsoleModifiers, T> IncrementByModifiers { get; set; } = new Dictionary<ConsoleModifiers, T>();
        public string SlideSymbols { get; set; } = " .-x#";
        public Splash SlideSplash { get; set; } = new Splash() { BackgroundColor = ConsoleColor.DarkYellow, ForegroundColor = ConsoleColor.Yellow };
        public Splash SlideBackgroundSplash { get; set; } = new Splash() { BackgroundColor = ConsoleColor.DarkGray, ForegroundColor = ConsoleColor.DarkGray };
        protected abstract void IncrementPreviewValue(bool Positive = true);
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
            bool loop = true;
            while (loop)
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
                var key = Console.ReadKey(true);
                IncrementModifiers = key.Modifiers;
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        Cancel = true;
                        continue;
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.Subtract:
                        IncrementPreviewValue(false);
                        break;
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.Add:
                        IncrementPreviewValue();
                        break;
                    default:
                        loop = false;
                        continue;
                }
                PreviewTrigger(PreviewValue);
            }
            PostActivate();
            return this;
        }
        protected RangeBase(T minValue, T maxValue, T increment)
        {
            MinRangeValue = minValue;
            MaxRangeValue = maxValue;
            Increment = increment;
        }
        protected abstract double GetRangeDisplayPart(T value);
        protected override void PrintContent()
        {
            Console.CursorLeft = Indent;
            InputSplash.Write(DisplayFormat(PreviewValue));
            Console.CursorTop += 2;
            Console.CursorLeft = Indent;
            Range.DrawBar(SlideWidth, SlideValueWidth, SlideSymbols, SlideSplash, SlideBackgroundSplash);
            Console.CursorTop += 2;
            Console.CursorLeft = Indent;
        }
    }
    public static class Range
    {
        public static void DrawBar(int barWidth, double valueWidth, string slideSymbols, Splash valueSplash = null, Splash backgroundSplash = null)
        {
            valueSplash = valueSplash ?? new Splash() { ForegroundColor = ConsoleColor.Yellow, BackgroundColor = ConsoleColor.DarkYellow };
            backgroundSplash = backgroundSplash ?? new Splash() { ForegroundColor = ConsoleColor.DarkGray, BackgroundColor = ConsoleColor.DarkGray };

            for (int i = 0; i < barWidth; i++)
            {
                var index = (int)Math.Floor((valueWidth - i) * (slideSymbols.Length - 1));
                index = Math.Min(index, slideSymbols.Length - 2);
                var splash = (i < valueWidth) ? valueSplash : backgroundSplash;
                splash.Write(slideSymbols[index < 0 ? 0 : index + 1].ToString());
            }
        }
        public static int DefaultSlideWidth { get; set; } = 50;
        public static IRange<T> New<T>(T MinValue, T MaxValue, T Increment, string Title = "", string Header = "", string ErrorMessage = "", string Footer = "", T? Default = null)
            where T : struct, IComparable, IComparable<T>, IConvertible, IEquatable<T>, IFormattable
        {
            Type type = null;
            var typecode = Type.GetTypeCode(typeof(T));
            switch (typecode)
            {
                case TypeCode.SByte: type = typeof(SByteRange); break;
                case TypeCode.Byte: type = typeof(ByteRange); break;
                case TypeCode.Int16: type = typeof(Int16Range); break;
                case TypeCode.UInt16: type = typeof(UInt16Range); break;
                case TypeCode.Int32: type = typeof(Int32Range); break;
                case TypeCode.UInt32: type = typeof(UInt32Range); break;
                case TypeCode.Int64: type = typeof(Int64Range); break;
                case TypeCode.UInt64: type = typeof(UInt64Range); break;
                case TypeCode.Single: type = typeof(SingleRange); break;
                case TypeCode.Double: type = typeof(DoubleRange); break;
                case TypeCode.Decimal: type = typeof(DecimalRange); break;
                default: throw new GenericTypeException($"Range.New does not support {typeof(T)} ranges");
            }
            var res = (IRange<T>)Activator.CreateInstance(type, MinValue, MaxValue, Increment);
            res.PreviewValue = res.Value = Default ?? MinValue;
            res.Title = Title;
            res.Header = Header;
            res.ErrorMessage = ErrorMessage;
            res.Footer = Footer;
            return res;
        }
    }
    public class DecimalRange : RangeBase<decimal>
    {
        public DecimalRange(decimal minValue, decimal maxValue, decimal increment) : base(minValue, maxValue, increment) { }

        public override decimal RangeSize { get { return MaxRangeValue - MinRangeValue; } }

        protected override double GetRangeDisplayPart(decimal value)
        {
            var diff = value - MinRangeValue;
            return SlideWidth * (double)diff / (double)RangeSize;
        }

        protected override void IncrementPreviewValue(bool Positive = true)
        {
            var test = PreviewValue;
            if (Positive) test += Increment;
            else test -= Increment;
            PreviewValue = Math.Min(Math.Max(test, MinRangeValue), MaxRangeValue);
        }
    }
    public class SingleRange : RangeBase<float>
    {
        public SingleRange(float minValue, float maxValue, float increment) : base(minValue, maxValue, increment) { }

        public override float RangeSize { get { return MaxRangeValue - MinRangeValue; } }

        protected override double GetRangeDisplayPart(float value)
        {
            var diff = value - MinRangeValue;
            return (double)SlideWidth * diff / RangeSize;
        }
        protected override void IncrementPreviewValue(bool Positive = true)
        {
            var test = PreviewValue;
            if (Positive) test += Increment;
            else test -= Increment;
            PreviewValue = Math.Min(Math.Max(test, MinRangeValue), MaxRangeValue);
        }
    }
    public class DoubleRange : RangeBase<double>
    {
        public DoubleRange(double minValue, double maxValue, double increment) : base(minValue, maxValue, increment) { }

        public override double RangeSize { get { return MaxRangeValue - MinRangeValue; } }

        protected override double GetRangeDisplayPart(double value)
        {
            var diff = value - MinRangeValue;
            return SlideWidth * diff / RangeSize;
        }
        protected override void IncrementPreviewValue(bool Positive = true)
        {
            var test = PreviewValue;
            if (Positive) test += Increment;
            else test -= Increment;
            PreviewValue = Math.Min(Math.Max(test, MinRangeValue), MaxRangeValue);
        }
    }
    public class SByteRange : RangeBase<sbyte>
    {
        public SByteRange(sbyte minValue, sbyte maxValue, sbyte increment) : base(minValue, maxValue, increment) { }

        public override sbyte RangeSize { get { return (sbyte)(MaxRangeValue - MinRangeValue); } }

        protected override double GetRangeDisplayPart(sbyte value)
        {
            var diff = value - MinRangeValue;
            return (double)SlideWidth * diff / RangeSize;
        }
        protected override void IncrementPreviewValue(bool Positive = true)
        {
            var test = PreviewValue;
            if (Positive) test += Increment;
            else test -= Increment;
            PreviewValue = Math.Min(Math.Max(test, MinRangeValue), MaxRangeValue);
        }
    }
    public class ByteRange : RangeBase<byte>
    {
        public ByteRange(byte minValue, byte maxValue, byte increment) : base(minValue, maxValue, increment) { }

        public override byte RangeSize { get { return (byte)(MaxRangeValue - MinRangeValue); } }

        protected override double GetRangeDisplayPart(byte value)
        {
            var diff = value - MinRangeValue;
            return (double)SlideWidth * diff / RangeSize;
        }
        protected override void IncrementPreviewValue(bool Positive = true)
        {
            var test = PreviewValue;
            if (Positive) test += Increment;
            else test -= Increment;
            PreviewValue = Math.Min(Math.Max(test, MinRangeValue), MaxRangeValue);
        }
    }
    public class Int16Range : RangeBase<short>
    {
        public Int16Range(short minValue, short maxValue, short increment) : base(minValue, maxValue, increment) { }

        public override short RangeSize { get { return (short)(MaxRangeValue - MinRangeValue); } }

        protected override double GetRangeDisplayPart(short value)
        {
            var diff = value - MinRangeValue;
            return (double)SlideWidth * diff / RangeSize;
        }
        protected override void IncrementPreviewValue(bool Positive = true)
        {
            var test = PreviewValue;
            if (Positive) test += Increment;
            else test -= Increment;
            PreviewValue = Math.Min(Math.Max(test, MinRangeValue), MaxRangeValue);
        }
    }
    public class UInt16Range : RangeBase<ushort>
    {
        public UInt16Range(ushort minValue, ushort maxValue, ushort increment) : base(minValue, maxValue, increment) { }

        public override ushort RangeSize { get { return (ushort)(MaxRangeValue - MinRangeValue); } }

        protected override double GetRangeDisplayPart(ushort value)
        {
            var diff = value - MinRangeValue;
            return (double)SlideWidth * diff / RangeSize;
        }
        protected override void IncrementPreviewValue(bool Positive = true)
        {
            var test = PreviewValue;
            if (Positive) test += Increment;
            else test -= Increment;
            PreviewValue = Math.Min(Math.Max(test, MinRangeValue), MaxRangeValue);
        }
    }
    public class Int32Range : RangeBase<int>
    {
        public Int32Range(int minValue, int maxValue, int increment) : base(minValue, maxValue, increment) { }

        public override int RangeSize { get { return MaxRangeValue - MinRangeValue; } }

        protected override double GetRangeDisplayPart(int value)
        {
            var diff = value - MinRangeValue;
            return (double)SlideWidth * diff / RangeSize;
        }
        protected override void IncrementPreviewValue(bool Positive = true)
        {
            var test = PreviewValue;
            if (Positive) test += Increment;
            else test -= Increment;
            PreviewValue = Math.Min(Math.Max(test, MinRangeValue), MaxRangeValue);
        }
    }
    public class UInt32Range : RangeBase<uint>
    {
        public UInt32Range(uint minValue, uint maxValue, uint increment) : base(minValue, maxValue, increment) { }

        public override uint RangeSize { get { return MaxRangeValue - MinRangeValue; } }

        protected override double GetRangeDisplayPart(uint value)
        {
            var diff = value - MinRangeValue;
            return (double)SlideWidth * diff / RangeSize;
        }
        protected override void IncrementPreviewValue(bool Positive = true)
        {
            var test = PreviewValue;
            if (Positive) test += Increment;
            else test -= Increment;
            PreviewValue = Math.Min(Math.Max(test, MinRangeValue), MaxRangeValue);
        }
    }
    public class Int64Range : RangeBase<long>
    {
        public Int64Range(long minValue, long maxValue, long increment) : base(minValue, maxValue, increment) { }

        public override long RangeSize { get { return MaxRangeValue - MinRangeValue; } }

        protected override double GetRangeDisplayPart(long value)
        {
            var diff = value - MinRangeValue;
            return (double)SlideWidth * diff / RangeSize;
        }
        protected override void IncrementPreviewValue(bool Positive = true)
        {
            var test = PreviewValue;
            if (Positive) test += Increment;
            else test -= Increment;
            PreviewValue = Math.Min(Math.Max(test, MinRangeValue), MaxRangeValue);
        }
    }
    public class UInt64Range : RangeBase<ulong>
    {
        public UInt64Range(ulong minValue, ulong maxValue, ulong increment) : base(minValue, maxValue, increment) { }

        public override ulong RangeSize { get { return MaxRangeValue - MinRangeValue; } }

        protected override double GetRangeDisplayPart(ulong value)
        {
            var diff = value - MinRangeValue;
            return (double)SlideWidth * diff / RangeSize;
        }
        protected override void IncrementPreviewValue(bool Positive = true)
        {
            var test = PreviewValue;
            if (Positive) test += Increment;
            else test -= Increment;
            PreviewValue = Math.Min(Math.Max(test, MinRangeValue), MaxRangeValue);
        }
    }
}
