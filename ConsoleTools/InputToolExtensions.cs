using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTools
{
    public static class InputToolExtensions
    {
        public static IInputTool<T> Cast<T>(this IInputTool tool)
        {
            return (IInputTool<T>)tool;
        }
        public static void ActUponInputToolTree(this IInputTool tool, Action<IInputTool> act)
        {
            act(tool);

            if (tool is IInputToolSelector)
            {
                foreach (var choice in (tool as ISelector).ObjChoices.Cast<IInputTool>())
                    choice.ActUponInputToolTree(act);
            }
        }
        public static T IfType<T>(this object tool, Action<T> act) where T : class
        {
            if (tool is T)
            {
                act(tool as T);
                return tool as T;
            }
            return null;
        }
    }
}
