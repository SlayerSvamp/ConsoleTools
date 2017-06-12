using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleTools;

namespace TextfileMenu
{
    enum Confirm { No, Yes }
    class Program
    {
        static void Main(string[] args)
        {
            Action<string> showFile = (file) =>
            {
                Console.Clear();
                Console.CursorTop = 1;
                Console.CursorLeft = 3;
                foreach (var line in File.ReadAllLines(file))
                {
                    Console.WriteLine(line);
                    Console.CursorLeft = 3;
                }

                Console.CursorTop += 2;
                Console.WriteLine("[Press Escape to go back]");
                for (ConsoleKey key = 0; key != ConsoleKey.Escape; key = Console.ReadKey(true).Key) ;
            };
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.ForegroundColor = ConsoleColor.White;
            Func<string, string> trimPath = (x) => x.Substring(x.LastIndexOf('\\') + 1);
            Func<string, int> extStartPos = (x) => x.LastIndexOf('.') < 0 ? x.Length : x.LastIndexOf('.');
            Func<string, string> trimFile = (x) => trimPath(x.Substring(0, extStartPos(x)));

            var fSels = Directory.EnumerateDirectories(".").Select(dir =>
                new Selector<string>(Directory.EnumerateFiles(dir))
                {
                    Title = trimPath(dir),
                    Header = $"Choose {trimPath(dir)}",
                    DisplayFormat = trimFile,
                    PostSelectTrigger = showFile,
                    IsMenu = true
                }).ToList();
            var sel = new EnumSelector<ConsoleColor>(x => x.ToString().Contains("Dark")) { Index = 1 };
            sel.Title = sel.OutputString;
            sel.PreSelectTrigger = (x) => { sel.Index++; sel.Cancel = true; };
            sel.CancelTrigger = (x) => { sel.Title = $"BGColor: {sel.OutputString}"; Console.BackgroundColor = x; };
            var menu = new InputToolSelector<IInputTool<string>>(fSels);
            menu.KeyActionDictionary[ConsoleKey.C] = (m) => sel.Select();
            var quit = new EnumSelector<Confirm> { Header = "Do you really want to quit?" };
            menu.CancelTrigger = (x) => menu.Cancel = quit.Select().Cast<Confirm>().Selected.Equals(Confirm.Yes);
            menu.Select();
        }
    }
}

