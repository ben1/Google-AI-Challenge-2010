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
//    m_growthMul = double.Parse(a_args[0]);
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

    int numPlanets = a_pw.m_planets.Count;
    List<Planet> sortedScores = new List<Planet>(numPlanets);

    // calculate a score for each planet
    foreach (Planet p in a_pw.m_planets)
    {
      // add score based on growth
      double score = p.m_growthRate * m_growthMul;
      if (p.m_actualEventualOwner == 2)
      {
        score *= m_enemyPlanetMul;
      }

      // OPTIM: precalc inverse distance
      // OPTIM: only calculcate changes since last frame
      List<double> distancesD = Precalc.Get.m_planetData[p.m_planetID].m_distancesD;

      // add score based on inverse distance to my planets
      double myPlanetsDistMul = m_myPlanetsDistMul / m_state.m_myPlanets.Count;
      foreach (Planet myp in m_state.m_myPlanets)
      {
        if (myp.m_planetID == p.m_planetID)
        {
          score += myPlanetsDistMul;
          continue;
        }
        double dist = distancesD[myp.m_planetID];
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
        if (enemyp.m_planetID == p.m_planetID)
        {
          score += enemyPlanetsDistMul;
          continue;
        }
        double dist = distancesD[enemyp.m_planetID];
        if (dist == 0.0)
        {
          dist = 1.0;
        }
        score += enemyPlanetsDistMul / dist;
      }

      // subtract score based on cost to take (only subtract if taking it from neutral because taking it from enemy is good)
      if (p.m_eventualOwner == 0) // was m_owner, but we should really be checking against the owner N turns ahead where N is distance to us, so probably better to use the eventual owner anyway
      {
        score -= m_costMul * (double)p.m_numShips;
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
      // TODO: send ships if we need to re-inforce it!
      if (targetPlanet.m_actualEventualOwner == 1)
      {
        continue;
      }

      bool successfullySent = false;

      // find our closest planet then next closest planet etc until we've fulfilled the total shipsToSend
      while (m_state.m_myPlanetsWithShips.Count > 0)
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

        List<int> distances = Precalc.Get.m_planetData[targetPlanet.m_planetID].m_distances;
        for (int i = 0; i < m_state.m_myPlanetsWithShips.Count; ++i)
        {
          int pid = m_state.m_myPlanetsWithShips[i];
          Planet p = a_pw.GetPlanet(pid);

          // don't send ships from planets under attack, 
          if (a_pw.GetPlanet(pid).m_actualEventualOwner != 1)
          {
            continue;
          }

          // don't send ships from planets that could be under attack
          int numShipsAtPlanet = p.m_numShipsCanSafelySend;
          if (targetPlanet.m_actualEventualOwner == 2 && (targetPlanet.m_score - (m_enemyPlanetMul - 1.0) * (targetPlanet.m_growthRate * m_growthMul)) > 1.3 * p.m_score)
          {
            numShipsAtPlanet = p.m_numShips;
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

#if PW_DEBUG
        if (bestDistance < 1)
        {
          System.Diagnostics.Debug.Assert(false);
        }
#endif
        bestDistance = Math.Max(1, bestDistance);

        // calculate the number of ships I need to send by getting the number of enemy ships on the planet at the time we'll reach them +1
        int shipsToSend = 1;
        int futureTurns = targetPlanet.m_future.Count - 1;
        if (bestDistance <= futureTurns)
        {
          shipsToSend = targetPlanet.m_future[bestDistance].EnemyShips() + 1;
        }
        else
        {
          shipsToSend = targetPlanet.m_future[futureTurns].EnemyShips() + 1;

          // if finally owned by 2, add growth until we would get there
          if (targetPlanet.m_future[futureTurns].m_owner == 2)
          {
            int extraDistance = bestDistance - futureTurns;
            shipsToSend += extraDistance * targetPlanet.m_growthRate;
          }
        }

        // cache order, only sending ships we can afford to send to less worthy planets but sending all ships to more worthy planets
        if (bestShipsCanSend > 0)
        {
          if (shipsToSend > bestShipsCanSend)
          {
            targetPlanet.m_incomingFleets.Add(new Fleet(1, bestShipsCanSend, bestPlanetID, targetPlanet.m_planetID, bestDistance, bestDistance));
            targetPlanet.m_incomingFleets.Sort(Fleet.CompareTurnsRemaining);
            targetPlanet.CalcEventuality();
            m_state.CacheOrder(bestPlanetID, targetPlanet.m_planetID, bestShipsCanSend);
          }
          else
          {
            targetPlanet.m_incomingFleets.Add(new Fleet(1, shipsToSend, bestPlanetID, targetPlanet.m_planetID, bestDistance, bestDistance));
            targetPlanet.m_incomingFleets.Sort(Fleet.CompareTurnsRemaining);
            targetPlanet.CalcEventuality();
            m_state.CacheOrder(bestPlanetID, targetPlanet.m_planetID, shipsToSend);
            successfullySent = true;
            break;
          }
        }
      }

      if (successfullySent)
      {
        m_state.SubmitOrders();
      }
      else
      {
        m_state.RevertOrders();
        // we don't need to reset m_future or m_incomingFleets here because we don't consider this planet again
      }
    }

    // if any planets have spare ships to send, send them to higher scoring planets
    foreach (Planet p in m_state.m_myPlanets)
    {
      if (p.m_numShipsCanSafelySend > 0)
      {
        int i = 0;
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
