using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using System.Diagnostics;
using System.IO;


class Tester
{
    public Tester()
    {
        m_optionTests = false;
        m_launchVisualiser = !m_optionTests; // only works if m_optionTests is false

        m_pwdir = @"C:\dev\planetwars\";

        // alternate settings
        //m_bot2 = "\"" + m_pwdir + "old/csharp_shooter.019/planetwars.exe 150\"";
        //m_bot2 = "\"" + m_pwdir + "bin/release/planetwars.exe -opts 750 300 4.5\"";
        m_bot2 = "\"java -jar " + m_pwdir + "example_bots/dualbot.jar\"";

        m_options = new Options(new Arg[] 
            { 
              new Arg(145, 0, 1)
             ,new Arg(600, 0, 1)
             ,new Arg(6.5, 0, 1) 
            });

        m_procs = new List<Process>();
    }

    public void Test()
    {
        int bestTotal = -1000;

        if (m_optionTests)
        {
            m_stopwatch = new System.Diagnostics.Stopwatch();
            m_stopwatch.Start();
            string bestOptions = "";
            File.Delete("options.txt");
            string text = "";
            foreach (object botOptions in m_options)
            {
                string bo = (string)botOptions;
                int total = PlayAllMaps(bo);
                string newtext = (bo + " = " + total.ToString() + "\n");
                Console.Error.Write(newtext);
                text += newtext;
                while (text.Length > 1024)
                {
                    try
                    {
                        File.AppendAllText("options.txt", text);
                        text = "";
                    }
                    catch (Exception ex)
                    {
                        ex.ToString();
                    }
                }
                if (total > bestTotal)
                {
                    bestTotal = total;
                    bestOptions = (string)botOptions;
                }
            }
            long precalctime = m_stopwatch.ElapsedMilliseconds;
            File.AppendAllText("options.txt", text + "\n best: " + bestOptions + " = " + bestTotal.ToString() + "\n time: " + precalctime.ToString());
        }
        else
        {
            bestTotal = PlayProxyMatch(3, "");
            //bestTotal = PlayAllMapsProxy("75,200,75");
        }

        return;
    }

    public int PlayAllMaps(string a_opts)
    {
        m_wins = 0;
        m_losses = 0;
        m_draws = 0;
        int total = 100;
        for (int m = 1; m <= total; ++m)
        {
            while (m_procs.Count >= s_numCores)
            {
                System.Threading.Thread.Sleep(5);
                for (int i = 0; i < m_procs.Count; ++i)
                {
                    if (m_procs[i].HasExited)
                    {
                        m_procs.RemoveAt(i);
                        --i;
                    }
                }
            }
            PlayMatch(m, a_opts);
        }
        while (m_procs.Count > 0)
        {
            System.Threading.Thread.Sleep(5);
            for (int i = 0; i < m_procs.Count; ++i)
            {
                if (m_procs[i].HasExited)
                {
                    m_procs.RemoveAt(i);
                    --i;
                }
            }
        }
        return m_wins - m_losses;
    }


    public int PlayAllMapsProxy(string a_opts)
    {
        int wins = 0;
        int losses = 0;
        int draws = 0;
        for (int m = 1; m <= 100; ++m)
        {
            int winner = PlayProxyMatch(m, a_opts);
            if (winner > 0)
            {
                ++wins;
            }
            else if (winner < 0)
            {
                ++losses;
            }
            else
            {
                ++draws;
            }
        }
        return wins - losses;
    }


    public int PlayProxyMatch(int a_map, string a_opts)
    {
        // create a the named pipe server so that the proxy bot can connect to it
        using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("testpipe", PipeDirection.InOut))
        {
            // start the game play process that will create the proxy bot that will connect to us
            string bot1 = m_pwdir + "proxy_bot/bin/release/proxy_bot.exe";
            Process p = CreateProcess(a_map, bot1);
            pipeServer.WaitForConnection();

            // setup writer so that our stdout (from MyBot) goes to the pipe.
            StreamWriter sw = new StreamWriter(pipeServer);
            sw.AutoFlush = true;
            Console.SetOut(sw);

            try
            {
                StreamReader sr = new StreamReader(pipeServer);
                string line = sr.ReadLine();
                string message = "";
                int turn = 0;
                MyBot bot = new MyBot(a_opts);
                while (line != null)
                {
                    if (line.Equals("go"))
                    {
                        turn++;
                        bot.DoTurn(new PlanetWars(message, turn));
                        System.Diagnostics.Debug.Assert(turn == bot.m_state.m_current.m_turn);
                        PlanetWars.FinishTurn();
                        message = "";
                    }
                    else
                    {
                        message += line + "\n";
                    }
                    line = sr.ReadLine();
                }
            }
            catch (IOException io)
            {
                string error = io.Message;
            }

            CheckLaunchVisualiser(p);

            string err = p.StandardError.ReadToEnd();

            return Winner(err);
        }
    }


    public void PlayMatch(int a_map, string a_opts)
    {
        string bot1 = "\"" + m_pwdir + "/bin/release/planetwars.exe -opts" + ((a_opts != null) ? " " + a_opts : "") + "\"";
        Process p = CreateProcess(a_map, bot1);
        m_procs.Add(p);
        p.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataHandler);
        p.BeginErrorReadLine();
    }


    void ErrorDataHandler(object a_sendingProcess, DataReceivedEventArgs a_outLine)
    {
        if (!String.IsNullOrEmpty(a_outLine.Data))
        {
            if (a_outLine.Data == "Player 1 Wins!")
            {
                ++m_wins;
            }
            else if (a_outLine.Data == "Player 2 Wins!")
            {
                ++m_losses;
            }
            else if (a_outLine.Data == "Draw!")
            {
                ++m_draws;
            }
        }
    }


    Process CreateProcess(int a_map, string a_bot1)
    {
        string map = m_pwdir + "maps/map" + a_map.ToString() + ".txt";

        ProcessStartInfo processStartInfo = new ProcessStartInfo();
        processStartInfo.FileName = "java.exe";
        processStartInfo.Arguments = "-jar " + m_pwdir + "tools/PlayGame.jar " + map + " 1000000 200 : " + a_bot1 + " " + m_bot2;
        processStartInfo.UseShellExecute = false;
        processStartInfo.CreateNoWindow = true;
        processStartInfo.RedirectStandardOutput = m_launchVisualiser;
        processStartInfo.RedirectStandardError = true;

        return Process.Start(processStartInfo);
    }


    void CheckLaunchVisualiser(Process a_proc)
    {
        if (m_launchVisualiser)
        {
            // pipe output to visualiser
            ProcessStartInfo processStartInfo2 = new ProcessStartInfo();
            processStartInfo2.FileName = "java.exe";
            processStartInfo2.Arguments = "-jar " + m_pwdir + "tools/ShowGame.jar";
            processStartInfo2.UseShellExecute = false;
            processStartInfo2.RedirectStandardInput = true;
            Process p2 = Process.Start(processStartInfo2);

            p2.StandardInput.Write(a_proc.StandardOutput.ReadToEnd());
            p2.StandardInput.Flush();
        }
    }


    int Winner(string a_err)
    {
        if (a_err.Contains("Player 1 Wins!"))
        {
            return 1;
        }
        else if (a_err.Contains("Player 2 Wins!"))
        {
            return -1;
        }
        else
        {
            return 0;
        }
    }

    int m_wins;
    int m_losses;
    int m_draws;
    List<Process> m_procs;
    System.Diagnostics.Stopwatch m_stopwatch;
    Options m_options;
    string m_bot2;
    string m_pwdir;
    bool m_launchVisualiser;
    bool m_optionTests;

    static int s_numCores = Math.Min(5, Math.Max(1, System.Environment.ProcessorCount - 1));
}
