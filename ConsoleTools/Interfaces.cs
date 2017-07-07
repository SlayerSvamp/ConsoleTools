using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTools
{
    public interface IInputTool
    {
        string Title { get; set; }
        string Header { get; set; }
        string ErrorMessage { get; set; }
        string Footer { get; set; }
        Splash HeaderSplash { get; set; }
        Splash ErrorMessageSplash { get; set; }
        Splash InputSplash { get; set; }
        Splash FooterSplash { get; set; }
        bool HasError { get; set; }
        IInputTool Activate();
        string ValueAsString { get; }
        object ObjValue { get; }
    }
    public interface IInputTool<T> : IInputTool
    {
        T Value { get; set; }
        Action<T> PreActivateTrigger { get; set; }
        Action<T> PostActivateTrigger { get; set; }
        Func<T, string> DisplayFormat { get; set; }
        Func<T, Splash> ContentSplashSelector { get; set; }
    }
    public interface ITextInput : IInputTool
    {
    }
    public interface ITextInput<T> : ITextInput, IInputTool<T>
    {

    }
    public interface ISelector : IInputTool
    {
        bool AllowCancel { get; set; }
        bool Cancel { get; set; }
        Dictionary<ConsoleKey, Action<ConsoleModifiers>> KeyPressActions { get; }
        IEnumerable<object> ObjOptions { get; }
        int Index { get; set; }
        int PreviewIndex { get; set; }
    }
    public interface ISelector<T> : ISelector, IInputTool<T>
    {
        T PreviewValue { get; set; }
        Action<T> PreviewTrigger { get; set; }
        Action<T> CancelTrigger { get; set; }
        List<T> Options { get; }
    }
    public interface IInputToolSelector : ISelector
    {
    }
    public interface IInputToolSelector<T> : IInputToolSelector, ISelector<T> where T : IInputTool
    {

    }
    public interface IEnumSelector : ISelector
    {

    }
    public interface IEnumSelector<T> : IEnumSelector, ISelector<T>
    {

    }
    public interface IFlagSelector : IEnumSelector
    {

    }
    public interface IFlagSelector<T> : IFlagSelector, IEnumSelector<T>
    {
        Action<T> PostToggleTrigger { get; set; }
    }
    public interface IRange : IInputTool
    {
        double SlideValueWidth { get; }
        int SlideWidth { get; set; }
        string SlideSymbols { get; set; }
        Splash SlideSplash { get; set; }
        Splash SlideBackgroundSplash { get; set; }
    }
    public interface IRange<T> : IInputTool<T>, IRange
    {
        T PreviewValue { get; set; }
        Action<T> PreviewTrigger { get; set; }
        Action<T> CancelTrigger { get; set; }
        T MinRangeValue { get; set; }
        T MaxRangeValue { get; set; }
        T RangeSize { get; }
        T Increment { get; set; }
        Dictionary<ConsoleModifiers, T> IncrementByModifiers { get; set; }
    }
}
