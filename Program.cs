using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace planetwars
{
    class Program
    {
        static void Main(string[] a_args)
        {
            if (a_args.Count() == 0)
            {
                Tester t = new Tester();
                t.Test();
            }
            else if (a_args[0] == "-tcp")
            {
                Tcp.Run();
            }
            else if (a_args[0] == "-turn")
            {
                DebugTurn(int.Parse(a_args[1]), a_args[2]);
            }
            else if (a_args[0] == "-webturn")
            {
                DebugWebTurn(int.Parse(a_args[1]), a_args[2]);
            }
            else if (a_args[0] == "-opts")
            {
                MyBot.Main(a_args);
            }
        }

        // debug a particular turn
        public static void DebugTurn(int a_turnNum, string a_turnString)
        {
            MyBot bot = new MyBot();
            PlanetWars pw = new PlanetWars(a_turnString, a_turnNum); // may need to pass the turn number in to reproduce the bug
            bot.DoTurn(pw);
            PlanetWars.FinishTurn();
        }

        // debug a particular turn against a web bot
        public static void DebugWebTurn(int a_turnNum, string a_webTurnString)
        {
            string[] s = a_webTurnString.Split('|');
            string[] planets = s[0].Split(':');
            string[] turns = s[1].Split(':');

            string turnStr = "";
            foreach (string p in planets)
            {
                turnStr += "P " + p.Replace(',', ' ') + "\n";
            }
            turnStr += "go\n\n";

            DebugTurn(a_turnNum, turnStr);
        }
    }
}
