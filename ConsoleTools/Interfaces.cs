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
        ColorWriter TitleColors { get; set; }
        ColorWriter HeaderColors { get; set; }
        ColorWriter ErrorMessageColors { get; set; }
        ColorWriter FooterColors { get; set; }
        string OutputString { get; }
        IInputTool Select();
        object ObjSelected { get; }
    }
    public interface IInputTool<T> : IInputTool
    {
        T Selected { get; set; }
        Action<T> PreSelectTrigger { get; set; }
        Action<T> PostSelectTrigger { get; set; }
    }
    public interface ITextInput : IInputTool
    {
    }
    public interface ITextInput<T> : ITextInput, IInputTool<T>
    {

    }
    public interface ISelector : IInputTool
    {
        ColorWriter SelectedColors { get; set; }
    }
    public interface ISelector<T> : ISelector, IInputTool<T>
    {
        T PreviewSelected { get; set; }
        Action<T> PreviewTrigger { get; set; }
        List<T> Choices { get; }
        Func<T, string> DisplayFormat { get; set; }
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
        Action<T> AfterToggle { get; set; }
    }
}
