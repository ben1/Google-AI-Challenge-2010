using System;
using System.Collections.Generic;

public class MyBot 
{
  bool m_initialised;
  State m_state;
  double m_growthMul;
  double m_myPlanetsDistMul;
  double m_enemyPlanetsDistMul;
  double m_costMul;
  double m_enemyPlanetMul;

  private void Init()
  {
    m_initialised = false;
    m_state = new State();
    m_growthMul = 150.0;
    m_myPlanetsDistMul = 400.0;
    m_enemyPlanetsDistMul = 50.0;
    m_costMul = 12.0f;
    m_enemyPlanetMul = 6.0f;
  }
  public MyBot()
  {
    Init();
  }

  public MyBot(string [] a_args)
  {
    Init();
    m_growthMul = double.Parse(a_args[0]);
  }

  public void DoTurn(PlanetWars a_pw) 
  {
    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
    stopwatch.Start(); 

    if (!m_initialised)
    {
      m_initialised = true;
      Precalc.Get.Init(a_pw);
    }
    //long precalctime = stopwatch.ElapsedMilliseconds;

    m_state.InitTurn(a_pw);
    //long inittime = stopwatch.ElapsedMilliseconds;

    int numPlanets = a_pw.Planets().Count;
    List<Planet> sortedScores = new List<Planet>(numPlanets);

    // calculate a score for each planet
    foreach (Planet p in a_pw.Planets())
    {
      // add score based on growth
      double score = p.GrowthRate() * m_growthMul;
      if (p.m_future.m_eventualOwner == 2)
      {
        score *= m_enemyPlanetMul;
      }

      // OPTIM: precalc inverse distance
      // OPTIM: only calculcate changes since last frame
      List<double> distancesD = Precalc.Get.m_planetData[p.PlanetID()].m_distancesD;

      // add score based on inverse distance to my planets
      double myPlanetsDistMul = m_myPlanetsDistMul / m_state.m_myPlanets.Count;
      foreach (Planet myp in m_state.m_myPlanets)
      {
        if (myp.PlanetID() == p.PlanetID())
        {
          score += myPlanetsDistMul;
          continue;
        }
        double dist = distancesD[myp.PlanetID()];
        if (dist == 0.0)
        {
          dist = 1.0;
        }
        score += myPlanetsDistMul / dist;
      }

      // add score based on inverse distance to enemy planets
      double enemyPlanetsDistMul = m_enemyPlanetsDistMul / m_state.m_enemyPlanets.Count;
      foreach (Planet enemyp in m_state.m_enemyPlanets)
      {
        if (enemyp.PlanetID() == p.PlanetID())
        {
          score += enemyPlanetsDistMul;
          continue;
        }
        double dist = distancesD[enemyp.PlanetID()];
        if (dist == 0.0)
        {
          dist = 1.0;
        }
        score += enemyPlanetsDistMul / dist;
      }

      // subtract score based on cost to take (only subtract if taking it from neutral because taking it from enemy is good)
      if (p.Owner() == 0)
      {
        score -= m_costMul * (double)p.m_future.m_eventualNumShips;
      }

      // safety
      if (double.IsInfinity(score))
      {
        score = 0.0f;
      }

      p.m_score = score;
      sortedScores.Add(p);
    }

    // sort
    sortedScores.Sort(Planet.CompareScore);

    //long scoretime = stopwatch.ElapsedMilliseconds;

    // keep sending ships until we don't want to send more or we can't send more
    for (int targetPlanetIteration = 1; targetPlanetIteration <= sortedScores.Count && m_state.m_myPlanetsWithShips.Count > 0; ++targetPlanetIteration)
    {
#if PW_DEBUG
#else
      // watch out for the time limit
      if (stopwatch.ElapsedMilliseconds > 800)
      {
        break;
      }
#endif
      // send the closest ships to the best scoring planet, next closest to next planet, etc.
      Planet targetPlanet = sortedScores[sortedScores.Count - targetPlanetIteration];

      // don't send more ships if we're going to own it eventually anyway
      if (targetPlanet.m_future.m_eventualOwner == 1)
      {
        continue;
      }

      // find our closest planet then next closest planet etc until we've fulfilled the total shipsToSend
      int shipsToSend = targetPlanet.m_future.m_eventualNumShips + 1;
      while (shipsToSend > 0 && m_state.m_myPlanetsWithShips.Count > 0)
      {
#if PW_DEBUG
#else
        // watch out for the time limit
        if (stopwatch.ElapsedMilliseconds > 800)
        {
          break;
        }
#endif
        // find the closest planet that can send ships
        int bestIndex = -1;
        int bestPlanetID = -1;
        int bestDistance = 1000000;
        int bestShipsCanSend = 0;

        List<int> distances = Precalc.Get.m_planetData[targetPlanet.PlanetID()].m_distances;
        for (int i = 0; i < m_state.m_myPlanetsWithShips.Count; ++i)
        {
          int pid = m_state.m_myPlanetsWithShips[i];
          Planet p = a_pw.GetPlanet(pid);

          // don't send ships from planets under attack, 
          if (a_pw.GetPlanet(pid).m_future.m_eventualOwner != 1)
          {
            continue;
          }

          // don't send ships from planets that could be under attack
          int numShipsAtPlanet = p.m_numShipsCanSafelySend;
          if (targetPlanet.m_future.m_eventualOwner == 2 && (targetPlanet.m_score - (m_enemyPlanetMul - 1.0) * (targetPlanet.GrowthRate() * m_growthMul)) > 1.3 * p.m_score) // remove the enemyPlanetMul component of the score
          {
            numShipsAtPlanet = p.NumShips();
          }
          if (numShipsAtPlanet <= 0)
          {
            continue;
          }

          if (distances[pid] < bestDistance)
          {
            bestShipsCanSend = numShipsAtPlanet;
            bestDistance = distances[pid];
            bestPlanetID = pid;
            bestIndex = i;
          }
        }

        // couldn't find a planet to send from
        if (bestPlanetID < 0)
        {
          break;
        }

        // cache order, only sending ships we can afford to send to less worthy planets but sending all ships to more worthy planets
        if (bestShipsCanSend > 0)
        {
          if (shipsToSend >= bestShipsCanSend)
          {
            m_state.CacheOrder(bestPlanetID, targetPlanet.PlanetID(), bestShipsCanSend);
            shipsToSend -= bestShipsCanSend;
          }
          else
          {
            m_state.CacheOrder(bestPlanetID, targetPlanet.PlanetID(), shipsToSend);
            shipsToSend = 0;
          }
        }
      }

      if (shipsToSend == 0)
      {
        m_state.SubmitOrders();
      }
      else
      {
        m_state.RevertOrders();
      }
    }
    //long endtime = stopwatch.ElapsedMilliseconds;
    a_pw.FinishTurn();
  }



  //-------------------------------------------------------------------------------
  public static void Main(string[] a_args)
  {
    int turn = 0;
    string line = Console.ReadLine();
    string message = "";
    MyBot bot;
    if (a_args.Length == 0)
    {
      bot = new MyBot();
    }
    else
    {
      bot = new MyBot(a_args);
    }
    while (turn >= 0)
    {
      try
      {
        while (line != null)
        {
          if (line.Equals("go"))
          {
            turn++;
            bot.DoTurn(new PlanetWars(message, turn));
            message = "";
          }
          else
          {
            message += line + "\n";
          }
          line = Console.ReadLine();
        }
        turn = -1; // finish
      }
      catch (Exception)
      {
        // Owned, but retry
      }
    }
  }

  //-------------------------------------------------------------------------------
  public static void DebugTurn(int a_turnNum, string a_turnString)
  {
    MyBot bot = new MyBot();
    PlanetWars pw = new PlanetWars(a_turnString, a_turnNum); // may need to pass the turn number in to reproduce the bug
    bot.DoTurn(pw);
  }
}
