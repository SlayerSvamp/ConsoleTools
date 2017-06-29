using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleTools;
using System.Drawing;
using System.IO;
using System.Diagnostics;

namespace BitmapGenerator
{
    enum ImageFormat { BMP, JPG, PNG, GIF, TIFF, ICO }
    class BitmapDrawer
    {
        public ImageFormat ImageFormat { get; set; } = ImageFormat.BMP;
        public Color PrimaryColor { get; set; } = Color.White;
        public Color SecondaryColor { get; set; } = Color.Black;
        public double LimitAlpha { get; set; } = 0;
        public int Width { get; set; }
        public int Height { get; set; }
        public double Zoom { get; set; } = 1;
        public double XScroll { get; set; } = 0;
        public double YScroll { get; set; } = 0;
        public int Iterations { get; set; } = 8;
        public string FileName { get; set; }
        public string FullFileName { get { return $"{FileName}.{ImageFormat.ToString().ToLower()}"; } }
        Bitmap Image { get; set; }
        Graphics Graphics { get; set; }
        public BitmapDrawer(string filename, int width, int height)
        {
            FileName = filename;
            Width = width;
            Height = height;
            Image = new Bitmap(Width, Height);
            Graphics = Graphics.FromImage(Image);
            ColorizePixel(1, 1, 1);
        }
        private void ColorizePixel(int x, int y, double alpha)
        {
            var color = Color.FromArgb((int)(alpha * 255), PrimaryColor.R, PrimaryColor.G, PrimaryColor.B);
            var brush = new SolidBrush(color);
            Graphics.FillRectangle(brush, x, y, 1, 1);
        }

        internal void CreateImage()
        {
            Console.Clear();
            Console.CursorTop = 1;
            Console.CursorLeft = 3;
            Console.WriteLine($"Creating Image ({Width}x{Height})");
            Console.CursorTop++;
            Graphics.Clear(SecondaryColor);

            var width = (int)(Width * Zoom);
            var height = (int)(Height * Zoom);

            var startCol = (int)((width - Width) * XScroll);
            var startRow = (int)((height - Height) * YScroll);

            for (int row = startRow; row < Height + startRow; row++)
            {
                for (int col = startCol; col < Width + startCol; col++)
                {
                    double c_re = (col - width / 2.0) * 4.0 / width;
                    double c_im = (row - height / 2.0) * 4.0 / width;
                    double x = 0, y = 0;
                    int iteration = 0;
                    while (x * x + y * y <= 4 && iteration < Iterations)
                    {
                        double x_new = x * x - y * y + c_re;
                        y = 2 * x * y + c_im;
                        x = x_new;
                        iteration++;
                    }
                    if (iteration < Iterations) ColorizePixel(col - startCol, row - startRow, iteration * 1d / Iterations);
                    else ColorizePixel(col - startCol, row - startRow, LimitAlpha);
                }
                if ((row - startRow) % (Height / 20) == 0)
                {
                    Console.CursorLeft = 3;
                    Range.DrawBar(50, 50 * ((0.0 + row - startRow) / Height), " .-x#");
                }

            }
        }

        internal void SaveImage()
        {
            Console.CursorTop++;
            Console.CursorLeft = 3;
            Console.WriteLine("Saving to file...");
            Image.Save(FullFileName);
        }
    }
    static class Program
    {
        enum Confirm { No, Yes }
        static void Main(string[] args)
        {
            Range.DefaultSlideWidth = 64;
            var red = Range.New(0, 255, 1, Default: 0xFF, Title: "Primary color Red");
            var green = Range.New(0, 255, 1, Default: 0xFF, Title: "Primary color Green");
            var blue = Range.New(0, 255, 1, Default: 0xFF, Title: "Primary color Blue");
            var red2 = Range.New(0, 255, 1, Default: 0, Title: "Secondary color Red");
            var green2 = Range.New(0, 255, 1, Default: 0, Title: "Secondary color Green");
            var blue2 = Range.New(0, 255, 1, Default: 0, Title: "Secondary color Blue");

            red.SlideSplash = red2.SlideSplash = new Splash() { ForegroundColor = ConsoleColor.Red, BackgroundColor = ConsoleColor.DarkRed };
            green.SlideSplash = green2.SlideSplash = new Splash() { ForegroundColor = ConsoleColor.Green, BackgroundColor = ConsoleColor.DarkGreen };
            blue.SlideSplash = blue2.SlideSplash = new Splash() { ForegroundColor = ConsoleColor.Blue, BackgroundColor = ConsoleColor.DarkBlue };
            TextInput name = null;
            name = new TextInput(x =>
            {
                var chars = x.Intersect(Path.GetInvalidFileNameChars());

                if (x.Length == 0)
                {
                    name.ErrorMessage = $"Value cannot be empty";
                }
                else if (chars.Any())
                    name.ErrorMessage = $"Remove: {Environment.NewLine}{string.Join(Environment.NewLine, chars)}";
                else
                    return true;
                return false;
            })
            {
                Title = "Finish",
                Header = "Choose filename:",
                Footer = "extension will be added (for example '.bmp')"
            };

            var options = new IInputTool[] { red, green, blue, red2, green2, blue2, name };
            var imageSettings = new InputToolSelector<IInputTool>(options);
            var cancel = new Selector<bool>(new bool[] { false, true }) { Header = "Do you want to exit without saving?", DisplayFormat = x => x ? "Yes" : "No" };
            imageSettings.CancelTrigger = x => imageSettings.Cancel = cancel.Activate().IfType<Selector<bool>>(y => { }).Value;
            var bgsplash = new Splash() { ForegroundColor = ConsoleColor.DarkGray, BackgroundColor = ConsoleColor.Gray };
            imageSettings.ActUponInputToolTree(tool => tool.IfType<IRange<int>>(x =>
            {
                x.IncrementByModifiers[ConsoleModifiers.Control] = 5;
                x.IncrementByModifiers[ConsoleModifiers.Shift | ConsoleModifiers.Control] = 20;
                x.SlideBackgroundSplash = bgsplash;
                x.Header = x.Title;
            }));
            imageSettings.InputSplash.ForegroundColor = ConsoleColor.Cyan;
            name.PostActivateTrigger = x => imageSettings.Cancel = true;
            name.FooterSplash.ForegroundColor = ConsoleColor.DarkGray;

            var format = new EnumSelector<ImageFormat> { Header = "Choose image format", InputSplash = imageSettings.InputSplash };
            while (true)
            {
                imageSettings.Activate();
                if (imageSettings.Cancel)
                    return;
                format.Activate();
                if (format.Cancel)
                    continue;

                var size = 16;
                var drawer = new BitmapDrawer(name.Value, 32 * 8 * size, 32 * 5 * size)
                {
                    Iterations = 64,
                    Zoom = 4,
                    XScroll = 0.45,
                    YScroll = 0.05,
                    PrimaryColor = Color.FromArgb(red.Value, green.Value, blue.Value),
                    SecondaryColor = Color.FromArgb(red2.Value, green2.Value, blue2.Value),
                    LimitAlpha = 1,
                    ImageFormat = format.Value
                };

                drawer.CreateImage();
                drawer.SaveImage();
                Process.Start($"{Directory.GetCurrentDirectory()}\\{drawer.FullFileName}");
                break;
            }
        }
    }
}
