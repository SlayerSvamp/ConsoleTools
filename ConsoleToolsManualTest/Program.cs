using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleTools;
using System.Globalization;
using System.Windows.Forms;
using System.IO;

namespace ConsoleToolsManualTest
{
    class Program
    {
        public static IEnumerable<T> EnumEnum<T>() where T : struct, IConvertible
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }
        enum Confirm : byte { No, Yes }
        enum Weapon : byte { Sword, Spear, Axe, Mace, Hammer, Flail, Bow, Crossbow, Shuriken, Blowgun, Staff, Wand, Knuckles, Claws }
        enum Armour : byte { Leather, HardLeather, StuddedLeather, Fur, Platemail, Chainmail, Scalemail, Robes }
        [Flags]
        enum Flags : ushort
        {
            First = 1 << 0,
            Second = 1 << 1,
            Third = 1 << 2,
            Fourth = 1 << 3,
            Fifth = 1 << 4,
            Sixth = 1 << 5,
            Seventh = 1 << 6,
            Eighth = 1 << 7,
            Ninth = 1 << 8,
            Tenth = 1 << 9,
            Eleventh = 1 << 10,
            Twelvth = 1 << 11,
            Thirteenth = 1 << 12,
            Fourteenth = 1 << 13,
            Fifteenth = 1 << 14,
            Sixteenth = 1 << 15,
        }
        enum FlagsDisplayStyle : byte { FlagNames, UnderlyingValue }
        [Flags]
        enum Simmärken
        {
            BaddarenGrön = 0x1,
            BaddarenBlå = 0x2,
            BaddarenGul = 0x4,
            Sköldpaddan = 0x8,
            Bläckfisken = 0x10,
            PingvinenSilver = 0x20,
            PingvinenGuld = 0x40,
            Silverfisken = 0x80,
            Guldfisken = 0x100,
            Järnmärke = 0x200,
            Bronsmärke = 0x400,
            Silvermärke = 0x800,
            Kandidaten = 0x1000,
            HajenBrons = 0x2000,
            HajenSilver = 0x4000,
            HajenGuld = 0x8000,
            SimsättsmärkeFjärilsim = 0x10000,
            SimsättsmärkeRyggsim = 0x20000,
            SimsättsmärkeBröstsim = 0x40000,
            SimsättsmärkeCrawl = 0x80000,
            Vattenprovet = 0x100000,
            VattenprovetÖppetVatten = 0x200000,
        }
        static string GetInfoString(IEnumerable<IInputTool> menuArray, IEnumSelector<FlagsDisplayStyle> flagsDisplayStyle)
        {
            var sb = new StringBuilder();
            foreach (var line in menuArray)
            {
                switch (line.Title)
                {
                    case "Flags display style":
                    case "Color":
                    case "Exit":
                    case "Console foreground color":
                    case "Console background color":
                        break;
                    default:
                        sb.Append($"{line.Title}: {line.OutputString}{Environment.NewLine}");
                        break;
                }
            }
            return sb.ToString();

        }
        public static ulong GetMaxValue<T>()
        {
            return Enum.GetValues(typeof(Flags)).Cast<T>().Select(t => Convert.ToUInt64(t)).Aggregate((a, b) => a | b);
        }
        [STAThread]
        static void Main(string[] args)
        {

            var doLoad = new EnumSelector<Confirm> { Title = "Load character", Header = "Do you want to load a character? (from file)" };
            if ((Confirm)doLoad.Select().ObjSelected == Confirm.Yes)
            {
                string[] lines;
                var dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    lines = File.ReadAllLines(dialog.FileName);
                }
            }
            Console.OutputEncoding = Encoding.Unicode;
            Console.WindowHeight = 30;
            int minAge = 1;
            int maxAge = 200;
            IEnumSelector<ConsoleColor> fcolor;
            IEnumSelector<ConsoleColor> bcolor;
            IEnumSelector<ConsoleColor> cfg;
            IEnumSelector<ConsoleColor> cbg;
            IInputToolSelector<IEnumSelector<ConsoleColor>> colors = null;

            IEnumSelector<FlagsDisplayStyle> flagsDisplayStyle;
            ulong flagsMaxValue = GetMaxValue<Flags>();
            IFlagSelector<Flags> flags;
            IFlagSelector<Simmärken> sim;

            var colorsArray = new IEnumSelector<ConsoleColor>[]
            {
                fcolor = new EnumSelector<ConsoleColor>{ Title = "Foreground color", Header = "Choose main menu footer foreground color", Footer = "Footer preview text", Selected = ConsoleColor.DarkGray, PreviewTrigger = (x) =>  colors.FooterColors.ForegroundColor = x },
                bcolor = new EnumSelector<ConsoleColor>{ Title = "Background color", Header = "Choose main menu footer background color", Footer = "Footer preview text", PreviewTrigger = (x) =>  colors.FooterColors.BackgroundColor = x },
                cfg = new EnumSelector<ConsoleColor>{ Title = "Console foreground color", Header = "Choose default foreground color for the program", Selected = ConsoleColor.Gray, PreviewTrigger = (x) => Console.ForegroundColor = x},
                cbg = new EnumSelector<ConsoleColor>{ Title = "Console background color", Header = "Choose default background color for the program", Selected = ConsoleColor.Black, PreviewTrigger = (x) => Console.BackgroundColor = x },
            };
            var decimalSeparator = Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            var menuArray = new IInputTool[]{
                new RegexInput( @"^[a-zA-Z0-9 ,]+$"){ Title = "Name", Header = "What is your name?", ErrorMessage = $"Must be alphanumeric, comma ',' or space ' '" },
                new IntegerInput((val) => val >= minAge && val <= maxAge) {Title = "Age", Header = "How old are you?", ErrorMessage = $"Must be in range {minAge}-{maxAge}" , Footer = "Limpistol för lösa tånaglar"},
                new Selector<string>(new string[] { "♂ - Male", "♀ - Female", "o - Other" }) {Title = "Gender", Header = "What's your gender?"},
                new Selector<string>(new string[] { "♣ - Killer", "♦ - Achiever", "♥ - Socializer", "♠ - Explorer" }){ Title = "Play style", Header = "Choose your playstyle" },
                new Selector<bool>(new bool[] { true, false }) { Title = "Test", Header = "test this bool", DisplayFormat = (input => input ? "Japp" : "Näpp")},
                new DoubleInput(x => x >= 0 && x <= double.MaxValue) { Title = "Double time!", Header = "What is your account balance? ($)", ErrorMessage = $"Value must be real number, with '{decimalSeparator}' as delimiter" },
                colors = new InputToolSelector<IEnumSelector<ConsoleColor>>(colorsArray){ Title = "Colors", Footer = "Footer preview text"},
                new EnumSelector<Weapon>{ Title = "Weapon", Header = "Choose your prefered weapon" },
                new EnumSelector<Armour>{ Title = "Armour", Header  = "Choose prefered armour" },
                sim = FlagSelector.New<Simmärken>(),
                flags = FlagSelector.New<Flags>(),
                flagsDisplayStyle = new EnumSelector<FlagsDisplayStyle>{ Title = "Flags display style", Header = "Display flags as:" },
                new EnumSelector<Confirm>{ Title = "Exit", Header = "Do you really wat to exit?" }
            };
            sim.Title = "Simmärken";
            sim.Header = "Vilka simmärken har du tagit?";
            sim.FooterColors.ForegroundColor = ConsoleColor.DarkMagenta;
            flags.Title = "Flags";
            flags.Header = $"Choose int -> get flags (0-{flagsMaxValue})";
            var menu = new InputToolSelector<IInputTool>(menuArray) { Title = "Main menu", Header = "Main menu" };
            colors.FooterColors = menu.FooterColors;
            fcolor.FooterColors = menu.FooterColors;
            bcolor.FooterColors = menu.FooterColors;

            foreach (var tool in new IEnumerable<IInputTool>[] { menu.Choices, new IInputTool[] { menu } }.SelectMany(x => x))
            {
                if (typeof(ISelector).IsAssignableFrom(tool.GetType()))
                {
                    (tool as ISelector).SelectedColors.ForegroundColor = ConsoleColor.Green;
                }
            }
            sim.AfterToggle = () => sim.Footer = $"Valda simmärken:{Environment.NewLine}{(string.Join("\n", sim.OutputString.Split(',').Select(s => s.Trim())))}";
            sim.AfterToggle();
            sim.SelectedColors.BackgroundColor = ConsoleColor.DarkRed;
            sim.SelectedColors.ForegroundColor = ConsoleColor.Black;
            var flagsDefaultDisplayFormat = flags.DisplayFormat;
            Console.WindowHeight += 20;
            //do
            //{
            menu.PostSelectTrigger = (_) =>
            {
                menu.FooterColors.ForegroundColor = fcolor.Selected;
                menu.FooterColors.BackgroundColor = bcolor.Selected;
                flags.DisplayFormat = flagsDisplayStyle.Selected == FlagsDisplayStyle.FlagNames ? flagsDefaultDisplayFormat : (x) => $"{(ulong)x}";
                menu.Footer = GetInfoString(menu.Choices, flagsDisplayStyle);
            };
            menu.Select();
            //} while (menu.Selected.Title != "Exit" || (Confirm)menu.Selected.ObjSelected != Confirm.Yes);
        }
    }
}
