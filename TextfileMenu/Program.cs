using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleTools;

namespace TextfileMenu
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.ForegroundColor = ConsoleColor.White;
            var dirs = Directory.EnumerateDirectories(".");
            Func<string, string> trimPath = (x) => x.Substring(x.LastIndexOf('\\') + 1);
            Func<string, int> extStartPos = (x) => x.LastIndexOf('.') < 0 ? x.Length : x.LastIndexOf('.');
            Func<string, string> trimExt = (x) => x.Substring(0, extStartPos(x));
            var menu = new Selector<string>("", "", dirs) { DisplayFormat = trimPath };
            while (true)
            {
                menu.Select();
                var files = Directory.EnumerateFiles(menu.Selected).ToList();
                files.Add("\r\n   (back)"); 
                var fileMenu = new Selector<string>("", $"Choose {trimPath(menu.Selected)}", files) { DisplayFormat = (x) => trimExt(trimPath(x)) };
                while ((string)fileMenu.Select().ObjSelected != files.Last())
                {
                    foreach (var line in File.ReadAllLines(fileMenu.Selected))
                    {
                        Console.WriteLine(line);
                    }
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("[Press Enter or Spacebar to go back]");
                    ;
                    for(ConsoleKey key = 0; key != ConsoleKey.Enter && key != ConsoleKey.Spacebar; key = Console.ReadKey(true).Key)
                    {
                        
                    }
                }

            }
        }
    }
}
