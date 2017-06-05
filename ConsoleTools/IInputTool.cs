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
}
