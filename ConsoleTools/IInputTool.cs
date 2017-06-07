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
        string InputMessage { get; set; }
        string OutputString { get; }
        IInputTool Select();
        object ObjSelected { get; }
    }
    public interface IInputTool<T>
    {
        T Selected { get; }
    }
    public interface ITextInput : IInputTool
    {
        string ErrorMessage { get; set; }
    }
    public interface ITextInput<T> : ITextInput, IInputTool<T>
    {

    }
    public interface ISelector : IInputTool
    {
        string Footer { get; set; }
        ConsoleColor FooterForegroundColor { get; set; }
        ConsoleColor FooterBackgroundColor { get; set; }
        ConsoleColor SelectedForegroundColor { get; set; }
        ConsoleColor SelectedBackgroundColor { get; set; }
    }
    public interface ISelector<T> : ISelector, IInputTool<T>
    {
        List<T> Choices { get; }
        Func<T, string> DisplayFormat { get; set; }
        Func<T, bool> Filter { get; set; }
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
