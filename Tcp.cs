using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

class Tcp
{
  public static int Run()
  {
    while (true)
    {
      try
      {
        Socket sock = Connect("72.44.46.68", 995);
        string username = "ben135";
        string password = "aslenra44lkq4jh";
        if (sock != null)
        {
          Byte[] sendBuf = Encoding.ASCII.GetBytes(String.Format("USER {0} PASS {1}\n", username, password));
          sock.Send(sendBuf, sendBuf.Length, 0);

          Byte[] receiveBuf = new Byte[409600];
          int bytesReceived = sock.Receive(receiveBuf);
          string response = Encoding.ASCII.GetString(receiveBuf, 0, bytesReceived);

          int turn = 0;
          string message = "";
          MyBot bot = new MyBot();

          System.IO.StringWriter sw = new System.IO.StringWriter();
          Console.SetOut(sw);
          bool finishedGame = false;
          while (!finishedGame)
          {
            string[] lines = response.Split('\n');
            foreach (string line in lines)
            {
              if (line.Length > 0)
              {
                if (line.StartsWith("INFO "))
                {
                  Console.Error.WriteLine(line);
                  if (line.StartsWith("INFO You ") && !line.StartsWith("INFO You currently"))
                  {
                    finishedGame = true;
                  }
                }
                else
                {
                  if (line.Equals("go"))
                  {
                    turn++;
                    Console.Error.WriteLine("Turn " + turn.ToString());
                    bot.DoTurn(new PlanetWars(message, turn));
                    PlanetWars.FinishTurn();
                    message = "";

                    string s = sw.GetStringBuilder().ToString();
                    sw.GetStringBuilder().Remove(0, sw.GetStringBuilder().Length);
                    sendBuf = Encoding.ASCII.GetBytes(s);
                    sock.Send(sendBuf, sendBuf.Length, 0);

                    break;
                  }
                  else
                  {
                    if(line.StartsWith("F ") || line.StartsWith("P "))
                    {
                      message += "\n"; // put newline before any real line
                    }
                    message += line;
                  }
                }
              }
            }

            bytesReceived = sock.Receive(receiveBuf);
            response = Encoding.ASCII.GetString(receiveBuf, 0, bytesReceived);
          }

          sock.Close();
        }
      }
      catch (System.Exception ex)
      {
        System.Threading.Thread.Sleep(2000);
      }
    }

    return 0;
  }


  static Socket Connect(string a_host, int a_port)
  {
    Socket s = null;
    IPHostEntry hostEntry = null;

    // Get host related information.
    hostEntry = Dns.GetHostEntry(a_host);

    // Loop through the AddressList to obtain the supported AddressFamily. This is to avoid
    // an exception that occurs when the host IP Address is not compatible with the address family
    // (typical in the IPv6 case).
    foreach (IPAddress address in hostEntry.AddressList)
    {
      IPEndPoint ipe = new IPEndPoint(address, a_port);
      Socket tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

      tempSocket.Connect(ipe);

      if (tempSocket.Connected)
      {
        s = tempSocket;
        break;
      }
      else
      {
        continue;
      }
    }
    return s;
  }
}