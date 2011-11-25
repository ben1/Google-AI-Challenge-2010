using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using System.IO;

namespace proxy_bot
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.Sleep(50);

            NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "testpipe", PipeDirection.InOut);
            // Connect to the pipe or wait until the pipe is available.
            pipeClient.Connect();

            StreamWriter sw = new StreamWriter(pipeClient);
            sw.AutoFlush = true;
            StreamReader sr = new StreamReader(pipeClient);

            // read console and write to pipe, then read pipe and write to console
            string message = "";

            try
            {
                string line = Console.ReadLine();
                while (line != null)
                {
                    // uncomment to debug proxy bot
                    //System.IO.File.AppendAllText("proxylog.txt", line + "\n");
                    if (line.Equals("go"))
                    {
                        sw.Write(message);
                        sw.Write("go" + "\n");
                        message = "";

                        // wait for reply through pipe and send to console
                        string pipeMessage = "";
                        while (true)
                        {
                            string pipeLine = sr.ReadLine();
                            if (pipeLine == null || pipeLine == "go")
                            {
                                break;
                            }
                            pipeMessage += pipeLine + "\n";
                        }
                        pipeMessage += "go\n";
                        Console.Write(pipeMessage);
                    }
                    else
                    {
                        message += line + "\n";
                    }
                    line = Console.ReadLine();
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
