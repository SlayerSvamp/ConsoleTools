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
        static readonly string TempFileName = $"{Guid.NewGuid().ToString()}.bmp";
        public ImageFormat ImageFormat { get; set; } = ImageFormat.BMP;
        public Color PrimaryColor { get; set; } = Color.White;
        public Color SecondaryColor { get; set; } = Color.Black;
        public double LimitAlpha { get; set; } = 0;
        public int Width { get; set; }
        public int Height { get; set; }
        public double Zoom { get; set; } = 1;
        public double ScrollX { get; set; } = 0;
        public double ScrollY { get; set; } = 0;
        public double SkewX { get; set; } = 0;
        public double SkewY { get; set; } = 0;
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

        public void CreateImage()
        {
            CreateImage(Width, Height);
        }
        private void CreateImage(int _width, int _height)
        {
            Console.Clear();
            Console.CursorTop = 1;
            Console.CursorLeft = 3;
            Console.WriteLine($"Creating Image ({Width}x{Height})");
            Console.CursorTop++;
            Graphics.Clear(SecondaryColor);

            var width = (int)(_width * Zoom);
            var height = (int)(_height * Zoom);

            var startCol = (int)((width - _width) * ScrollX);
            var startRow = (int)((height - _height) * ScrollY);

            for (int row = startRow; row < _height + startRow; row++)
            {
                for (int col = startCol; col < _width + startCol; col++)
                {
                    double c_re = (col - width / 2.0) * 4.0 / width;
                    double c_im = (row - height / 2.0) * 4.0 / width;
                    double x = 0, y = 0;
                    int iteration = 0;
                    while (x * x + y * y <= 4 && iteration < Iterations)
                    {
                        double x_new = x * x - y * y + c_re;
                        y = 2 * x * y + c_im + iteration * SkewY;
                        x = x_new + iteration * SkewX;
                        iteration++;
                    }
                    if (iteration < Iterations) ColorizePixel(col - startCol, row - startRow, iteration * 1d / Iterations);
                    else ColorizePixel(col - startCol, row - startRow, LimitAlpha);
                }
                if ((row - startRow) % (Height / 200) == 0)
                {
                    Console.CursorLeft = 3;
                    Range.DrawBar(50, 50 * ((0.0 + row - startRow) / _height), " .-x#");
                }

            }
        }

        public void SaveImage()
        {
            Console.CursorTop++;
            Console.CursorLeft = 3;
            Console.WriteLine("Saving to file...");
            Image.Save(FullFileName);
        }
        public void PreviewImage(int width, int height)
        {
            CreateImage(width, height);
            Image.Save(TempFileName);
            Process.Start(TempFileName);
        }

        internal void DeletePreviewFile()
        {
            File.Delete(TempFileName);
        }
    }
    static class Program
    {
        enum Confirm { No, Yes }
        enum Step { Exit, Color, Format, Ratio, Size, Iterations, Zoom, ScrollX, ScrollY, CreatePreview, CreateImage, Finished }
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
            var ratio = new Selector<(int RatioX, int RatioY, int Width, int Height)>(
                new[] {
                    (1, 1, 256, 256),
                    (4, 3, 320, 240),
                    (16, 9, 480, 270),
                    (16, 10, 320, 200)
                })
            { Header = "Choose image ratio", DisplayFormat = x => $"{x.RatioX}:{x.RatioY}" };
            var size = new Selector<int>(new int[] { 1, 2, 3, 4, 5, 6, 8, 12, 16, 24, 32, 48, 64 }) { Header = "Choose image size", DisplayFormat = x => $"{x * ratio.Value.Width} x {x * ratio.Value.Height}" };
            var iterations = Range.New(0, 255, 1, Default: 64, Header: "Choose level of detail (iteration count)");
            var zoom = new DoubleInput(x => x >= 1) { Header = "Input zoom factor", Value = 1, Footer = "Examples:\n1 = 100%\n0.5 = 50%", FooterSplash = name.FooterSplash };
            var scrollx = new DoubleInput(x => x <= 1 && x >= 0) { Value = .5, Header = "Input X-axis scroll factor (left = 0.0, right = 1.0)", Footer = "Examples:\n1 = 100%\n0.5 = 50%", FooterSplash = name.FooterSplash };
            var scrolly = new DoubleInput(x => x <= 1 && x >= 0) { Value = .5, Header = "Input Y-axis scroll factor (top = 0.0, bottom = 1.0)", Footer = "Examples:\n1 = 100%\n0.5 = 50%", FooterSplash = name.FooterSplash };
            var change = new EnumSelector<Confirm> { Header = "Do you want to change something before finishing up?" };

            Func<bool, BitmapDrawer> createDrawer = (isPreview) =>
             {
                 var x = ratio.Value.Width * (isPreview ? 1 : size.Value);
                 var y = ratio.Value.Height * (isPreview ? 1 : size.Value);
                 return new BitmapDrawer(name.Value, x, y)
                 {
                     Iterations = iterations.Value,
                     Zoom = zoom.Value,
                     ScrollX = scrollx.Value,
                     ScrollY = scrolly.Value,
                     PrimaryColor = Color.FromArgb(red.Value, green.Value, blue.Value),
                     SecondaryColor = Color.FromArgb(red2.Value, green2.Value, blue2.Value),
                     LimitAlpha = 1,
                     ImageFormat = format.Value
                 };
             };

            BitmapDrawer preview = null;

            for (var step = Step.Color; step != Step.Finished && step != Step.Exit;)
            {
                switch (step)
                {
                    case Step.Color:
                        imageSettings.Activate();
                        if (imageSettings.Cancel && imageSettings.Value != name)
                            step--;
                        else
                        {
                            imageSettings.Value = imageSettings.Options[0];
                            step++;
                        }
                        break;

                    case Step.Format:
                        format.Activate();
                        if (format.Cancel)
                            step--;
                        else step++;
                        break;

                    case Step.Ratio:
                        ratio.Activate();
                        if (ratio.Cancel)
                            step--;
                        else step++;
                        break;

                    case Step.Iterations:
                        iterations.Activate();
                        step++;
                        break;

                    case Step.Size:
                        size.Activate();
                        if (size.Cancel)
                            step = Step.Ratio;
                        else step++;
                        break;

                    case Step.Zoom:
                        zoom.Activate();
                        step++;
                        break;

                    case Step.ScrollX:
                        scrollx.Activate();
                        step++;
                        break;

                    case Step.ScrollY:
                        scrolly.Activate();
                        step++;
                        break;

                    case Step.CreatePreview:
                        preview = createDrawer(true);
                        preview.PreviewImage(ratio.Value.Width, ratio.Value.Height);
                        change.Activate();
                        if (Confirm.Yes == change.Value || change.Cancel)
                            step = Step.Size;
                        else step++;
                        preview.DeletePreviewFile();
                        break;

                    case Step.CreateImage:
                        var final = createDrawer(false);
                        final.CreateImage();
                        final.SaveImage();
                        Process.Start(final.FullFileName);
                        step++;
                        break;
                    case Step.Exit:
                    case Step.Finished:
                        break;
                }
            }
        }
    }
}
