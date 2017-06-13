using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleTools;
using System.Globalization;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;

namespace ConsoleToolsManualTest
{
    enum Confirm : byte { No, Yes }
    enum Exit : byte { Cancel, SaveAndQuit, Quit }
    enum Weapon : byte { Sword, Spear, Axe, Mace, Hammer, Flail, Bow, Crossbow, Shuriken, Blowgun, Staff, Wand, Knuckles, Claws }
    enum Armour : byte { Leather, HardLeather, StuddedLeather, Fur, Platemail, Chainmail, Scalemail, Robes }
    enum Color : byte { ConsoleFG, ConsoleBG, FooterFG, FooterBG }
    enum PlayStyle : byte { Killer, Achiever, Socializer, Explorer }
    [Flags]
    enum Badges
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
    enum Option { Name, Age, Gender, Style, Colors, Weapon, Armour, Badges }
    class Character
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public byte Gender { get; set; }
        public byte Style { get; set; }
        public Weapon Weapon { get; set; }
        public Armour Armour { get; set; }
        public Badges Badges { get; set; }

    }
    static class Program
    {
        static void Init()
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.WindowHeight = 50;
        }

        static void Load(IDictionary<Option, IInputTool> options, IEnumSelector<Confirm> doLoad = null)
        {
            OpenFileDialog dialog;
            Character loaded = null;
            if (doLoad == null)
                doLoad = new EnumSelector<Confirm> { Title = "Load character", Header = "Do you want to load a character? (from file)" };
            if (doLoad.Select().Cast<Confirm>().Selected == Confirm.Yes)
            {
                dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var json = File.ReadAllText(dialog.FileName);
                    loaded = JsonConvert.DeserializeObject<Character>(json);
                }

                if (loaded != null)
                {
                    Option opt = 0;
                    try
                    {
                        opt = Option.Name;
                        options[Option.Name].Cast<string>().Selected = loaded.Name;
                        opt = Option.Age;
                        options[Option.Age].Cast<int>().Selected = loaded.Age;
                        opt = Option.Gender;
                        (options[Option.Gender] as ISelector).Index = loaded.Gender;
                        opt = Option.Style;
                        (options[Option.Style] as ISelector).Index = loaded.Style;
                        opt = Option.Weapon;
                        options[Option.Weapon].Cast<Weapon>().Selected = loaded.Weapon;
                        opt = Option.Armour;
                        options[Option.Armour].Cast<Armour>().Selected = loaded.Armour;
                        opt = Option.Badges;
                        options[Option.Badges].Cast<Badges>().Selected = loaded.Badges;
                    }
                    catch (ArgumentException)
                    {
                        doLoad.ErrorMessage = $"Data '{opt}' was corrupted in {dialog.FileName}.";
                        doLoad.HasError = true;
                        Load(options, doLoad);
                    }
                }
            }
        }
        static Selector<PlayStyle> GeneratePlayStyleSelector()
        {

            var sel = new EnumSelector<PlayStyle>
            {
                Title = "Play style",
                Header = "Choose your playstyle",
                DisplayFormat = (style) =>
                {
                    switch (style)
                    {

                        case PlayStyle.Killer: return "♣ - Killer";
                        case PlayStyle.Achiever: return "♦ - Achiever";
                        case PlayStyle.Socializer: return "♥ - Socializer";
                        case PlayStyle.Explorer: return "♠ - Explorer";
                        default: return "";
                    }
                }
            };

            sel.PreviewTrigger = (s) =>
            {
                switch (s)
                {
                    case PlayStyle.Killer:
                        sel.Footer = "Clubs (Killers) (♣) enjoy competition and take pleasure in causing physical destruction in the virtual environment."; break;
                    case PlayStyle.Achiever:
                        sel.Footer = "Diamonds(Achievers) (♦) enjoy gaining points, levels, or any physical measure of their in-game achievement."; break;
                    case PlayStyle.Socializer:
                        sel.Footer = "Hearts(Socializers) (♥) enjoy playing games for the social aspect or by interacting with other players."; break;
                    case PlayStyle.Explorer:
                        sel.Footer = "Spades(Explorers) (♠) enjoy digging around, discovering new areas, or learning about easter eggs or glitches in the game."; break;
                    default: break;
                }
            };
            return sel;
        }
        static IDictionary<Option, IInputTool> GenerateOptions(IDictionary<Color, IEnumSelector<ConsoleColor>> colors)
        {
            int minAge = 1;
            int maxAge = 200;
            return new Dictionary<Option, IInputTool> {
                { Option.Name, new RegexInput(@"^[a-zA-Z0-9 ,]+$") { Title = "Name", Header = "What is your name?", ErrorMessage = $"Must be alphanumeric, comma ',' or space ' '" } },
                { Option.Age, new IntegerInput((val) => val >= minAge && val <= maxAge) { Title = "Age", Header = "How old are you?", ErrorMessage = $"Must be in range {minAge}-{maxAge}", Footer = "Limpistol för lösa tånaglar" } },
                { Option.Gender, new Selector<string>(new string[] { "♂ - Male", "♀ - Female", "o - Other" }) { Title = "Gender", Header = "What's your gender?" } },
                { Option.Style, GeneratePlayStyleSelector()},
                { Option.Colors, new InputToolSelector<IEnumSelector<ConsoleColor>>(colors.Values) { Title = "Colors", Footer = "Footer preview text" } },
                { Option.Weapon, new EnumSelector<Weapon> { Title = "Weapon", Header = "Choose your prefered weapon" } },
                { Option.Armour, new EnumSelector<Armour> { Title = "Armour", Header = "Choose prefered armour" } },
                { Option.Badges, FlagSelector.New<Badges>(Title: "Simmärken", Header: "Vilka simmärken har du tagit?") }
            };
        }
        static IDictionary<Color, IEnumSelector<ConsoleColor>> GenerateColorMenuItems()
        {
            return new Dictionary<Color, IEnumSelector<ConsoleColor>>
            {
                { Color.FooterFG, new EnumSelector<ConsoleColor> { Title = "Foreground color", Header = "Choose main menu footer foreground color", Footer = "Footer preview text", Selected = ConsoleColor.DarkGray } },
                { Color.FooterBG, new EnumSelector<ConsoleColor> { Title = "Background color", Header = "Choose main menu footer background color", Footer = "Footer preview text" } },
                { Color.ConsoleFG, new EnumSelector<ConsoleColor> { Title = "Console foreground color", Header = "Choose default foreground color for the program", Selected = ConsoleColor.Gray, PreviewTrigger = (x) => Console.ForegroundColor = x, CancelTrigger = (x) => Console.ForegroundColor = x } },
                { Color.ConsoleBG, new EnumSelector<ConsoleColor> { Title = "Console background color", Header = "Choose default background color for the program", Selected = ConsoleColor.Black, PreviewTrigger = (x) => Console.BackgroundColor = x, CancelTrigger = (x) => Console.BackgroundColor = x } }
            };
        }
        static void Finalize(IInputToolSelector menu, IDictionary<Option, IInputTool> options, IDictionary<Color, IEnumSelector<ConsoleColor>> colors)
        {
            options[Option.Colors].FooterColors = menu.FooterColors;

            colors[Color.FooterFG].PreviewTrigger = (x) => options[Option.Colors].FooterColors.ForegroundColor = x;
            colors[Color.FooterBG].PreviewTrigger = (x) => options[Option.Colors].FooterColors.BackgroundColor = x;

            options[Option.Badges].FooterColors.ForegroundColor = ConsoleColor.DarkMagenta;
            colors[Color.FooterFG].FooterColors = menu.FooterColors;
            colors[Color.FooterBG].FooterColors = menu.FooterColors;

            //badges
            var badges = (IFlagSelector<Badges>)options[Option.Badges];
            badges.AfterToggle = (x) => badges.Footer = $"Valda simmärken:{Environment.NewLine}{(string.Join("\n", badges.DisplayFormat(x).Split(',').Select(s => s.Trim())))}";
            badges.AfterToggle(badges.PreviewSelected);
            badges.SelectedColors.BackgroundColor = ConsoleColor.DarkRed;
            badges.SelectedColors.ForegroundColor = ConsoleColor.Black;
        }
        static string GetInfoString(IDictionary<Option, IInputTool> options)
        {
            var rows = options.Where(o => o.Key != Option.Colors)
                              .Select(o => $"{o.Value.Title}: {o.Value.OutputString}{Environment.NewLine}");
            return string.Concat(rows);
        }
        static void Save(IDictionary<Option, IInputTool> options)
        {
            var dialog = new SaveFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var json = JsonConvert.SerializeObject(new
                {
                    Name = options[Option.Name].Cast<string>().Selected,
                    Age = options[Option.Age].Cast<int>().Selected,
                    Gender = (byte)(options[Option.Gender] as ISelector).Index,
                    Style = (byte)(options[Option.Style] as ISelector).Index,
                    Weapon = options[Option.Weapon].Cast<Weapon>().Selected,
                    Armour = options[Option.Armour].Cast<Armour>().Selected,
                    Badges = options[Option.Badges].Cast<Badges>().Selected
                });

                File.WriteAllText(dialog.FileName, json);
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            Init();
            var colors = GenerateColorMenuItems();
            var options = GenerateOptions(colors);
            var menu = new InputToolSelector<IInputTool>(options.Values) { Title = "Main menu", Header = "Main menu" };
            Finalize(menu, options, colors);
            Load(options);

            menu.PreSelectTrigger = (x) => menu.Footer = GetInfoString(options);
            var exit = new EnumSelector<Exit> { Header = "Exiting character creation" };
            Func<string> saveHeader = () => $"Do you want to save '{options[Option.Name].Cast<string>().Selected}'?";
            var save = new EnumSelector<Confirm>() { Header = saveHeader() };
            save.PostSelectTrigger = (x) => { if (x == Confirm.Yes) Save(options); };
            menu.KeyActionDictionary[ConsoleKey.S] = (m) => save.Select();
            menu.CancelTrigger = (x) =>
            {
                exit.Select();
                menu.Cancel = exit.Selected != Exit.Cancel;
                if (exit.Selected == Exit.SaveAndQuit)
                    Save(options);
            };
            menu.Select();
        }
    }
}
