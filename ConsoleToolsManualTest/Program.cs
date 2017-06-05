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
        static string GetInfoString(IEnumerable<IInputTool> menuArray, EnumSelector<FlagsDisplayStyle> flagsDisplayStyle)
        {
            var sb = new StringBuilder();
            foreach (var line in menuArray)
            {
                switch (line.Title)
                {
                    case "Flags display style":
                    case "Color":
                    case "Exit":
                        break;
                    default:
                        sb.AppendLine($"   {line.Title}: {line.OutputString}");
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
            var doLoad = new EnumSelector<Confirm>("Load character", "Do you want to load a character?");
            if ((Confirm)doLoad.Select().ObjSelected == Confirm.Yes)
            {
                string[] lines;
                var dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    lines = File.ReadAllLines(dialog.FileName);
                }
            }
            var hej = new CustomInput<ulong>("GetUlong", "Write an ulong value", "wtf?!", u => u > 3 && u < 15, s => UInt64.Parse(s));
            Console.OutputEncoding = Encoding.Unicode;
            Console.WindowHeight = 30;
            int minAge = 1;
            int maxAge = 200;
            EnumSelector<ConsoleColor> color;
            EnumSelector<FlagsDisplayStyle> flagsDisplayStyle;
            ulong flagsMaxValue = GetMaxValue<Flags>();
            IFlagSelector<Flags> flags;
            IFlagSelector<Simmärken> sim;
            var decimalSeparator = Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            Type type = typeof(IInputTool);
            var menuArray = new IInputTool[]
            {
                new RegexInput("Name", "What is your name?",$"Must be alphanumeric, comma ',' or space ' '", @"^[a-zA-Z0-9 ,]+$"),
                new IntegerInput("Age", "How old are you?", $"Must be in range {minAge}-{maxAge}", (val) => val >= minAge && val <= maxAge),
                new Selector<string>("Gender", "What's your gender?", new string[] { "♂ - Male", "♀ - Female", "o - Other" }),
                new Selector<string>("Play style", "Choose your playstyle", new string[] { "♣ - Killer", "♦ - Achiever", "♥ - Socializer", "♠ - Explorer" }),
                new Selector<bool>("Test", "test this bool", new bool[] { true, false }) {DisplayFormat = (input => input ? "Japp" : "Näpp")},
                new DoubleInput("Double time!", "What is your account balance? ($)", $"Value must be real number, with '{decimalSeparator}' as delimiter", x => x >= 0 && x <= double.MaxValue),
                color = new EnumSelector<ConsoleColor>("Color", "Choose display color") { Filter = (c) => !c.ToString().StartsWith("Dark") && c != ConsoleColor.Black },
                new EnumSelector<Weapon>("Weapon", "Choose your prefered weapon"),
                new EnumSelector<Armour>("Armour", "Choose prefered armour"),
                sim = FlagSelector.New<Simmärken>("Simmärken", "Vilka simmärken har du tagit?"),
                flags = FlagSelector.New<Flags>("Flags", $"Choose int -> get flags (0-{flagsMaxValue})"),
                flagsDisplayStyle = new EnumSelector<FlagsDisplayStyle>("Flags display style", "Display flags as:"),
                new EnumSelector<Confirm>("Exit", "Do you really wat to exit?")
            };

            var menu = new Selector<IInputTool>("Main menu", "Main menu", menuArray);

            var flagsDefaultDisplayFormat = flags.DisplayFormat;

            do
            {
                menu.FooterColor = color.Selected;
                flags.DisplayFormat = flagsDisplayStyle.Selected == FlagsDisplayStyle.FlagNames ? flagsDefaultDisplayFormat : (x) => $"{(ulong)x}";
                menu.Footer = GetInfoString(menu.Choices, flagsDisplayStyle);
                menu.Select();
                menu.Selected.Select();
            } while (menu.Selected.Title != "Exit" || ((EnumSelector<Confirm>)menu.Selected).Selected != Confirm.Yes);
        }
    }
}
