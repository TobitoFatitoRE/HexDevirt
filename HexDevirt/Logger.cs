using System;
using System.Drawing;
using HexDevirt.Core;
using Console = Colorful.Console;

namespace HexDevirt
{
    public class Logger : iLogger
    {
        public void Success(object message)
        {
            WriteLine(message, "+", Color.Aqua);
        }

        public void Warning(object message)
        {
            WriteLine(message, "#", Color.Brown);
        }

        public void Error(object message)
        {
            WriteLine(message, "!", Color.Red);
        }

        public void Info(object message)
        {
            WriteLine(message, "?", Color.Orange);
        }

        public void ShowInfo(Version version)
        {
            Console.WriteLine();
            Console.WriteLine();
            WriteLineMiddle(@" _   _          ______           _      _   ", Color.Red);
            WriteLineMiddle(@"| | | |         |  _  \         (_)    | |  ", Color.Red);
            WriteLineMiddle(@"| |_| | _____  _| | | |_____   ___ _ __| |_ ", Color.Red);
            WriteLineMiddle(@"|  _  |/ _ \ \/ / | | / _ \ \ / / | '__| __|", Color.Red);
            WriteLineMiddle(@"| | | |  __/>  <| |/ /  __/\ V /| | |  | |_ ", Color.Red);
            WriteLineMiddle(@"\_| |_/\___/_/\_\___/ \___| \_/ |_|_|   \__|", Color.Red);
            WriteLineMiddle(@"                                            ", Color.Red);
            WriteLineMiddle(@"                                            ", Color.Red);
            WriteMiddle(@"Version - ", Color.Red);
            Console.WriteLine(version.ToString(), Color.White);
            WriteMiddle(@"Developer - ", Color.Red);
            Console.WriteLine("TobitoFatito", Color.White);
            //  WriteMiddle(@"Github Repo - ",Color.Red);
            // Console.WriteLine("https://github.com/TobitoFatitoNulled/HexDevirt/",Color.White);
            WriteMiddle(@"Original Repo - ", Color.Red);
            Console.WriteLine("https://github.com/hexck/Hex-Virtualization/", Color.White);
            WriteLineMiddle(@"", Color.Red);
        }

        private void WriteMiddle(object message, Color color)
        {
            Console.Write(string.Format("{0," + (Console.WindowWidth / 2 + message.ToString().Length / 2) + "}",
                message), color);
        }

        private void WriteLineMiddle(object message, Color color)
        {
            Console.WriteLine(string.Format("{0," + (Console.WindowWidth / 2 + message.ToString().Length / 2) + "}",
                message), color);
        }

        private void WriteLine(object message, string sign, Color color)
        {
            Console.Write("[", Color.White);
            Console.Write(sign, color);
            Console.Write("] ", Color.White);
            Console.WriteLine(message.ToString(), color);
        }
    }
}