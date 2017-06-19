using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleTools;
using System.Windows.Forms;

namespace TextfileMenu
{
    public interface IDirectoryItem
    {

    }
    class Directory : IDirectoryItem
    {
        string title;
        public string Title
        {
            get
            {
                if (title == null)
                    title = Path.Substring(Path.LastIndexOf("\\") + 1);
                return title;
            }
        }
        IEnumerable<File> files;
        IEnumerable<Directory> directories;
        public string Path { get; set; }
        public IEnumerable<Directory> Directories
        {
            get
            {
                if (directories == null)
                    directories = System.IO.Directory.EnumerateDirectories(Path).Select(s => new Directory() { Path = s });
                return directories;
            }
        }
        public IEnumerable<File> Files
        {
            get
            {
                if (files == null)
                    files = System.IO.Directory.EnumerateFiles(Path).Select(s => new File() { FileName = s });
                return files;
            }
        }
    }
    class File : IDirectoryItem
    {
        string title;
        public string Title
        {
            get
            {
                if (title == null)
                    title = FileName.Substring(FileName.LastIndexOf("\\") + 1);
                return title;
            }
        }
        private string[] content;
        public string FileName { get; set; }
        public string[] Content
        {
            get
            {
                if (content == null)
                    content = System.IO.File.ReadAllLines(FileName);
                return content;
            }
            set { content = value; }
        }
    }
    enum Confirm { No, Yes }
    static class Program
    {
        static Selector<object> CreateMenuByDirectory(Directory dir, IEnumerable<string> extensions)
        {
            var dirsplash = new Splash() { ForegroundColor = ConsoleColor.Cyan };
            var filesplash = new Splash() { ForegroundColor = ConsoleColor.Green };
            var items = dir.Directories
                .Select((x) => CreateMenuByDirectory(x, extensions))
                .Where(x => x != null)
                .ToList<object>();
            items.AddRange(dir.Files.Where(x => extensions.Any(e => x.FileName.EndsWith(e))));
            var dirname = new DirectoryInfo(dir.Path).Name;
            if (items.Any())
            {
                var menu = new Selector<object>(items)
                {
                    Header = dirname,
                    Title = dirname,
                    IsMenu = true,
                    PostActivateTrigger = (x) =>
                    {
                        x.IfType<ISelector>(y => y.Activate());
                        if (x is File)
                        {
                            Console.Clear();
                            Console.CursorTop = 1;
                            Console.CursorLeft = 3;
                            foreach (var line in x.IfType<File>((_) => { }).Content)
                            {
                                Console.WriteLine(line);
                                Console.CursorLeft = 3;
                            }

                            Console.CursorTop += 2;
                            Console.WriteLine("[Press Escape to go back]");
                            for (ConsoleKey key = 0; key != ConsoleKey.Escape; key = Console.ReadKey(true).Key) ;
                        }
                    },
                    DisplayFormat = x =>
                    {
                        string value = null;
                        x.IfType<File>(y =>
                        {
                            value = y.FileName;
                            var index = value.LastIndexOf("\\");
                            if (index >= 0)
                                value = value.Substring(index + 1);
                            if (value.EndsWith("."))
                                value = value.Substring(0, value.LastIndexOf("."));
                        });
                        x.IfType<ISelector>(y => value = y.Title);
                        return value;
                    },
                    ContentSplashSelector = y =>
                    {
                        if (y is ISelector)
                            return dirsplash;
                        return filesplash;
                    }
                };
                return menu;
            }
            return null;
        }
        public static Selector<object> CreateMenu(string path, EnumSelector<Confirm> exit, IEnumerable<string> extensions)
        {
            var dir = new Directory() { Path = path };
            var menu = CreateMenuByDirectory(dir, extensions);
            if (menu != null)
            {

                menu.CancelTrigger = x => menu.Cancel = exit.Activate().Cast<Confirm>().Value == Confirm.Yes;
                menu.KeyPressActions[ConsoleKey.O] = (m) =>
                {
                    menu.Cancel = true;
                    menu.CancelTrigger = (_) => { };
                };
            }
            return menu;
        }
        [Flags]
        enum Extensions
        {
            txt = 1,
            html = 2,
            css = 4,
            js = 8,
            htm = 16,
            php = 32,

        }
        [STAThread]
        static void Main(string[] args)
        {
              try
            {
                var root = new FolderBrowserDialog() { SelectedPath = "." };
                var exit = new EnumSelector<Confirm> { Header = "Do you want to quit?" };
                var extensions = FlagSelector.New<Extensions>();
                var exts = Enum.GetValues(typeof(Extensions)).Cast<Extensions>();
                extensions.AllowCancel = false;
                Selector<object> menu;
                do
                {
                    extensions.Activate();
                    root.ShowDialog();
                    menu = CreateMenu(root.SelectedPath, exit, exts.Where(x => extensions.Value.HasFlag(x)).Select(x => $".{x}"));
                    if (menu == null)
                    {
                        Console.Clear();
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.CursorTop = 1;
                        Console.CursorLeft = 3;
                        Console.Write($"Cannot select folder. It contains no textfiles.");
                        Console.CursorTop++;
                        Console.CursorLeft = 3;
                        Console.Write("[Press enter to load another folder]");
                        Console.ReadLine();
                        Console.ResetColor();
                        continue;
                    }
                    menu.Activate();
                } while (exit.Value != Confirm.Yes);
            }
            catch(Exception ex)
            {
                Console.Clear();
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.CursorTop = 1;
                Console.CursorLeft = 3;
                Console.Write($"An error has occured:");
                Console.CursorTop++;
                Console.CursorLeft = 3;
                Console.WriteLine(ex.Message);
                Console.CursorTop++;
                Console.CursorLeft = 3;
                Console.Write("[Press enter to exit]");
                Console.ReadLine();
            }

        }
    }
}

