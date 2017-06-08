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
        ConsoleTextBlock TitleBlock { get; set; }
        ConsoleTextBlock HeaderBlock { get; set; }
        ConsoleTextBlock ErrorMessageBlock { get; set; }
        ConsoleTextBlock FooterBlock { get; set; }
        string OutputString { get; }
        IInputTool Select();
        object ObjSelected { get; }
    }
    public interface IInputTool<T>
    {
        T Selected { get; set; }
    }
    public interface ITextInput : IInputTool
    {
    }
    public interface ITextInput<T> : ITextInput, IInputTool<T>
    {

    }
    public interface ISelector : IInputTool
    {
        ConsoleColor SelectedForegroundColor { get; set; }
        ConsoleColor SelectedBackgroundColor { get; set; }
    }
    public interface ISelector<T> : ISelector, IInputTool<T>
    {
        List<T> Choices { get; }
        Func<T, string> DisplayFormat { get; set; }
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
        Action AfterToggle { get; set; }
    }
}
