using AudioSwitcher.AudioApi.CoreAudio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Overlord_Warden {
    class SpeechRecognizer {

        static SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        bool CanCommand = true;
        private string pDir = Directory.GetCurrentDirectory() + @"\c.dat";
        private string rDir = Directory.GetCurrentDirectory() + @"\r.dat";
        private string trollDir = Directory.GetCurrentDirectory() + @"\troll.txt";
        private string configDir = Directory.GetCurrentDirectory() + @"\config.dat";

        private string[] rPunishments;

        Dictionary<string, string> commands = new Dictionary<string, string>();
        Dictionary<string, char> commandType = new Dictionary<string, char>();
        Dictionary<char, Action<string>> punishments = new Dictionary<char, Action<string>>() {
            { 'S', (words) => { if (!Speech || synthesizer.State != SynthesizerState.Ready) return; Console.ForegroundColor = ConsoleColor.Magenta; Console.WriteLine($"{(KeyPressLastGiven ? "\n" : "")}Said: {words}"); KeyPressLastGiven = false; _ = Task.Run(() => synthesizer.SpeakAsync(words));  } },
            { 'V', (volume) => { if (!Volume) return; new CoreAudioController().DefaultPlaybackDevice.Volume = int.Parse(volume); Console.ForegroundColor = ConsoleColor.Magenta; Console.WriteLine($"{(KeyPressLastGiven ? "\n" : "")}Volume: {volume}");KeyPressLastGiven = false;  } },
            { 'W', (process) => { if (!WebLinks) return; Console.ForegroundColor = ConsoleColor.Magenta; Console.WriteLine($"{(KeyPressLastGiven ? "\n" : "")}Link opened: {process}"); System.Diagnostics.Process.Start(process); KeyPressLastGiven = false;} },

        };

        SpeechRecognitionEngine recognizer;
        System.Timers.Timer randomEventTimer = new System.Timers.Timer();
        Random random = new Random();

        private static bool WebLinks;
        private static bool KeyInput;
        private static bool Speech;
        private static bool Volume;

        private static bool KeyPressLastGiven = false;


        // For keylogging to use as punishment material
        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(Int32 KeyboardKeyDown);

        public SpeechRecognizer() {
            synthesizer.SetOutputToDefaultAudioDevice();

            if (!File.Exists(configDir)) {
                File.WriteAllLines(configDir, new string[] {
                    "W:True",
                    "K:True",
                    "S:True",
                    "V:True"
                });
            }

            if (!File.Exists(trollDir)) { File.WriteAllText(trollDir, "Lol :D"); }
            if (!File.Exists(pDir)) {
                File.WriteAllLines(pDir, new string[] {
                    "phantom,fountain,and some|S|You must buy the vandal as your primary weapon",
                    "vandal|S|You must buy the phantom as your primary weapon",
                    "full buy|S|Nah. Tell them to save",
                    "save,save round|S|Nah. Full buy",
                    "jet|S|You must blow into your mic for 10 seconds",
                    "food,hungry,hunger,hanger|S|Do 5 pushups immediately",
                    "slow|S|You must rush the enemy",
                    @"roll,top,frank|W|https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                    "bot,bottom|S|You are shit. You suck. Get some kills. Dogwater. Learn to play. You're being diff'd. Get good before you come back. You should have left before we launched in. You were carried to bronze",
                    "credit,credits|S|You may not spend any more than $2000",
                    "afk,i ask|S|You must remain in spawn for 40 seconds before entering the map"

                });
            }
            if (!File.Exists(rDir)) {
                File.WriteAllLines(rDir, new string[] {
                    "S|You cannot use your current weapon. Drop them and find another",
                    "S|You cannot use any abilities this round",
                    "S|You cannot press Right Mouse Button",
                    "S|You cannot use your scroll wheel",
                    "S|Jump has been disabled",
                    "S|You may not give any callouts this round. Not that you do anyway",
                    "S|You must buy the most expensive weapon avaliable at the next opportunity",
                    "S|You may not speak at all this round.",
                    "S|You may not pick up the spike",
                    "S|You must say \"pew pew pew\" each time you shoot",
                    "S|You must show off your knife for the next 15 seconds",
                    "S|If you have a doppelganger, killing them is your highest priority",
                    "V|0",
                    "V|15",
                    "V|100",
                    $"W|{trollDir}",
                    @"W|https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                    "S|You must talk without your lips touching",
                    "S|You must buy and use the sheriff only",
                    "S|You must buy light armour",
                    "S|You must run to each spike site before continuing to play the game",
                    "S|You must play the entire round crouched down",
                    "S|You must use only surpressed weapons",
                    "S|Pick a player, and guard them with your life",
                    "S|You may not use your gun until you have run out of abilities",
                    "S|You must try to be as far away from everyone as possible for the rest of this round",
                    "S|Go flank them",
                    "S|Go back to spawn",
                    "S|Go to A site",
                    "S|Go to B site",
                    "S|Go to C site, else the enemy spawn",
                    "S|Buy the frenzy, ares, or odin only",
                    "S|You must aim down sights for the rest of the round",
                    "S|You may only make animal noises",
                    "S|You must give false callouts when you next die. Sabotage",
                    "S|You must place your spray on another player's spray before continuing on with the game",
                    "S|You must use the same loadout as another player",
                    "S|Shotguns only. No shotgun no kills",
                    "S|Snipers only",
                    "S|Spectre only",
                    "S|Stinger only",
                    "S|You can only callout using real life place names",
                    "S|Press right =",
                    "S|You must teabag the nearest corpse before continuing on with the game",
                    "S|Use your ult instantly, if you have it",
                    "S|When the spike is planted, press =",
                    @"W|https://www.youtube.com/watch?v=0ynT_2DDBZg",
                    "S|Make your mouse sensitivity maximum",
                    "S|You may not reload your weapons",
                    "S|You must stand ontop of spike",
                    "S|Remain crouched at all times",
                    "S|You must whisper something weird to another player",
                    "S|The floor is lava",
                    "S|You may not kill an enemy until an ally has died",
                    "S|You must kill the next enemy you see. You attack any other player until they are dead",
                    "S|After getting a kill, you must remain in place",
                    "S|Knife only",
                    "S|Pistol only",
                    "S|Reload has been disabled",
                    "S|You're bad lol",
                    @"W|https://www.youtube.com/watch?v=7rbgYh9fbkA",
                    @"W|https://pbs.twimg.com/media/FDdpjdGXEAA98K5.jpg",
                    @"W|C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                    @"W|https://www.youtube.com/watch?v=_vBVGjFdwk4",
                    "S|Throw all your abilities at a teammate",
                    "S|You can only walk backwards",
                    "S|Spend all your money",
                    "S|Set your sensitivity to 0.01",
                    "V|69",
                    "V|42",
                    "S|You cannot stop talking until the end of the round",
                    "S|You cannot kill anyone other than their topfrag",
                    "S|You are explicitly banned from using shotguns",
                    "S|Pick one player - they are exempt from Warden's commands for one round",
                    "S|Go to your school's hub",
                    "S|Take a tour of the map",
                    "S|Type AFK in chat",
                    "S|Try to get a knife kill",
                    "S|Hold team chat for the rest of the round",
                    "S|Hold party chat for the rest of ther round",
                    "S|Join the enemy team's discord call, if possible",
                    "S|You must hold left for the rest of the round",
                    "S|You must hold right for the rest of the round",
                    "S|Rate everyone on the team in terms of looks",
                    "S|Do 5 pushups",
                    "S|Watch a youtube video",
                    "S|Double the volume of any music or videos playing",
                    "S|Jump into the spike explosion at the end of the round",
                    "S|Unload your clip into the ground",
                    "S|Call out an allie's position in all chat", 
                });
            }


            foreach (string s in File.ReadAllLines(pDir)) {
                try {
                    string keys = s.Split('|')[0];
                    char type = char.Parse(s.Split('|')[1]);
                    string value = s.Split('|')[2];
                    foreach (string key in keys.Split(',')) {
                        commandType.Add(key, type);
                        commands.Add(key, value);
                    }
                }
                catch {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{(KeyPressLastGiven ? "\n" : "")}Invalid command line of {s} given");
                    KeyPressLastGiven = false;
                }
            }

            rPunishments = File.ReadAllLines(rDir);

            bool[] config = File.ReadAllLines(configDir).Select(x => bool.Parse(x.Trim().Split(':')[1])).ToArray();
            WebLinks = config[0];
            KeyInput = config[1];
            Speech = config[2];
            Volume = config[3];
        }


        public void AsyncStart(bool internalCall = false) {

            if (!internalCall) {
                recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-UK"));
                recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(Recognizer_SpeechRecognized);
                recognizer.LoadGrammar(new DictationGrammar());
                recognizer.SetInputToDefaultAudioDevice();
                randomEventTimer = new System.Timers.Timer();
                randomEventTimer.Interval = 27000;
                randomEventTimer.Elapsed += RandomTimerEventHandler;
                randomEventTimer.Start();
            }
            recognizer.RecognizeAsync(RecognizeMode.Single);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Started Listener");


            // Technically a keylogger lol
            if (!internalCall)
                _ = Task.Run(() => {
                    if (!KeyInput) return;
                    KeyEventLogger();
                });
        }

        private void KeyEventLogger() {
            DateTime lastKeyPressed = DateTime.Now;
            while (true) {
                for (int i = 0; i < 255; i++) {
                    int wasKeypressed = GetAsyncKeyState(i);
                    if (wasKeypressed == 1 || wasKeypressed == -32767 || wasKeypressed == 32769) {
                        string key = KeyValue.VerifyKey(i);
                        lastKeyPressed = key == "[1]" || key == "[2]" ? lastKeyPressed : DateTime.Now;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(key);
                        KeyPressLastGiven = true;

                        KeyEventPunishments(key);
                    }
                }

                if ((DateTime.Now - lastKeyPressed).TotalSeconds > 25) {
                    AFKPunishments();
                    lastKeyPressed = DateTime.Now;
                }
            }
        }

        private void AFKPunishments() {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{(KeyPressLastGiven ? "\n" : "")}AFK Punished");
            KeyPressLastGiven = false;

            int rnd = random.Next(0, 2);
            if (rnd == 0) punishments['W'](@"https://www.youtube.com/watch?v=f2bHoTUiMpI");
            else if (rnd == 1) {
                punishments['V']("100");
                punishments['S']("MOVE YOUR ASS");
            }
            else if (rnd == 2) {
                punishments['W'](@"https://www.youtube.com/shorts/otM070WxMf4");
            }
            else {
                punishments['V']("0");
            }
        }

        private void KeyEventPunishments(string key) {
            // Key punishments
            if (random.Next(0, 100) == 0 && key == "s") punishments['S']("You must hold W for the rest of the round. Coward");
            if (random.Next(0, 250) == 0 && key == "[Shift]") punishments['S']("Charge forwards. No more being quiet");
            if (random.Next(0, 25) == 0 && key == "b") punishments['S']("You cannot buy anything this round");
            if (key == "y") punishments['S']("Stop showing off your skin. Noob");
            if (random.Next(0, 75) == 0 && key == "[Control]") punishments['S']("You are no longer allowed to crouch");
            if (random.Next(0, 75) == 0 && key == "[Control]") punishments['S']("You must only crouch");
            if (random.Next(0, 10) == 0 && key == "t") punishments['S']("You must spray the enemy spawn before killing anyone");
            if (key == "[Enter]") punishments['S']("Stop typing start playing");
            if (random.Next(0, 25) == 0 && key == "[Caps Lock]") punishments['S']("You may not use the map for the next 5 rounds");
            if (key == "=") RandomPunishment();
            if (random.Next(0, 30) == 0 && key == "r") punishments['S']("Reload has been disabled");
            if (key == "[Down]") punishments['V']("100");
        }

        private void RandomPunishment() {
            int pos = random.Next(0, rPunishments.Length);
            try {
                punishments[char.Parse(rPunishments[pos].Split('|')[0])](rPunishments[pos].Split('|')[1]);
            }
            catch (Exception ex) {
                WriteError(ex);
            }
        }

        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {
            string speech = e.Result.Text.ToLower();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{(KeyPressLastGiven ? "\n" : "")}\"{speech}\" -> P = {CanCommand}");
            KeyPressLastGiven = false;

            AsyncStart(true);
            if (!CanCommand) return;

            foreach (string key in commands.Keys) {
                if (speech.Contains(key)) {
                    try {
                        HandleTrigger(key);
                        CanCommand = false;
                    }
                    catch (Exception ex) {
                        WriteError(ex);
                    }
                }
            }
            if (CanCommand) return;

            System.Timers.Timer t = new System.Timers.Timer();
            t.Elapsed += (eh1, eh2) => { CanCommand = true; Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"{(KeyPressLastGiven ? "\n" : "")}Punishments Are Enabled"); KeyPressLastGiven = false; (eh1 as System.Timers.Timer).Dispose(); };
            t.Interval = 10000;
            t.Start();
        }

        private static void WriteError(Exception ex) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{(KeyPressLastGiven ? "\n" : "")}Error from {ex.StackTrace}: {ex.Message}");
            KeyPressLastGiven = false;
        }

        private void RandomTimerEventHandler(object sender, System.Timers.ElapsedEventArgs e) {
            Console.Title = DateTime.UtcNow.ToString();
            Console.WriteLine($"{(KeyPressLastGiven ? "\n" : "")}Random timer event trigger");
            KeyPressLastGiven = false;
            if (random.Next(0, 3) == 0) {
                RandomPunishment();
            }
        }



        private void HandleTrigger(string key) {
            string command = commands[key];
            char type = commandType[key];
            punishments[type](command);
        }
    }
}