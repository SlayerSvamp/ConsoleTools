using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTools
{
    public static class InputToolExtentions
    {
        public static IInputTool<T> Cast<T>(this IInputTool tool)
        {
            return (IInputTool<T>)tool;
        }
    }
}
