using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleTools;

namespace TextfileMenu
{
    public interface IDirectoryItem
    {

    }
    class Directory : IDirectoryItem
    {
        public string Title { get { return Path.Substring(Path.LastIndexOf("\\") + 1); } }
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
            set { directories = value; }
        }
        public IEnumerable<File> Files
        {
            get
            {
                if (files == null)
                    files = System.IO.Directory.EnumerateFiles(Path).Select(s => new File() { FileName = s });
                return files;
            }
            set { files = value; }
        }
    }
    class File : IDirectoryItem
    {
        public string Title { get { return FileName.Substring(FileName.LastIndexOf("\\") + 1); } }
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
        static Selector<object> CreateMenuByDirectory(Directory dir, string extension)
        {
            var items = dir.Directories.Select((x) => CreateMenuByDirectory(x, extension)).ToList<object>();
            items.AddRange(dir.Files.Where(x => x.FileName.EndsWith(extension)));
            var dirname = new DirectoryInfo(dir.Path).Name;
            var menu = new Selector<object>(items)
            {
                Header = dirname,
                Title = dirname,
                IsMenu = true,
                PostSelectTrigger = (x) =>
                {
                    x.IfType<ISelector>(y => y.Select());
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
                        var name = y.FileName;
                        var index = name.LastIndexOf("\\");
                        if (index >= 0)
                            name = name.Substring(index + 1);
                        if (name.EndsWith(extension))
                            name = name.Substring(0, name.LastIndexOf(extension));
                        value = name.TrimEnd();
                    });
                    x.IfType<ISelector>(y => value = y.Title);
                    return value;
                }
            };
            return menu;
        }
        static void Main(string[] args)
        {
            var dir = new Directory() { Path = @"C:\Users\Linus\Documents\Rollspel\Exalted - Character" };
            var ext = ".txt";
            var menu = CreateMenuByDirectory(dir, ext);
            var exit = new EnumSelector<Confirm> { Header = "Do you want to quit?" };
            menu.CancelTrigger = x => menu.Cancel = exit.Select().Cast<Confirm>().Selected == Confirm.Yes;
            menu.Select();
        }
    }
}

