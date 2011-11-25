using System;
using System.Collections.Generic;

public class MyBot 
{
  bool m_initialised;
  public State m_state;
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

      if(m_state.m_current.m_turn == 1)
      {
        List<int> dists = Precalc.Get.m_planetData[targetPlanet.m_planetID].m_distances;
        if(dists[m_state.m_enemyPlanets[0].m_planetID] <= dists[m_state.m_myPlanets[0].m_planetID] - 5)
        {
          continue;
        }
      }

      if ((int)((double)targetPlanetIteration * 2.5) > sortedScores.Count)
      {
        break;
      }
      if (targetPlanet.m_score < targetPlanet.m_growthRate * -150 + 150)
      {
        break;
      }

      // Threaten the planet with enemy ships from the closest planet
      if (targetPlanet.m_owner != 0)
      {
        int cepid2 = -1;
        int cepid = m_state.GetClosestEnemyPlanet(targetPlanet.m_planetID, ref cepid2);
        if (cepid >= 0)
        {
          Planet e = a_pw.GetPlanet(cepid);
          // commenting this out got more wins.... if (e.m_score < targetPlanet.m_score)
          {
            targetPlanet.m_incomingFleets.Add(new Fleet(2, e.m_numShips, e.m_planetID, targetPlanet.m_planetID, Precalc.Get.m_planetData[cepid].m_distances[targetPlanet.m_planetID], Precalc.Get.m_planetData[cepid].m_distances[targetPlanet.m_planetID]));
            targetPlanet.m_incomingFleets.Sort(Fleet.CompareTurnsRemaining);
            targetPlanet.CalcEventuality();
          }
        }
        if (cepid2 >= 0)
        {
          Planet e = a_pw.GetPlanet(cepid2);
          // commenting this out got more wins.... if (e.m_score < targetPlanet.m_score)
          {
            targetPlanet.m_incomingFleets.Add(new Fleet(2, e.m_numShips, e.m_planetID, targetPlanet.m_planetID, Precalc.Get.m_planetData[cepid2].m_distances[targetPlanet.m_planetID], Precalc.Get.m_planetData[cepid2].m_distances[targetPlanet.m_planetID]));
            targetPlanet.m_incomingFleets.Sort(Fleet.CompareTurnsRemaining);
            targetPlanet.CalcEventuality();
          }
        }
      }

      // don't send more ships if we're going to own it eventually anyway
      if (targetPlanet.m_eventualOwner == 1)
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

          if(pid == targetPlanet.m_planetID)
          {
            continue;
          }

          Planet p = a_pw.GetPlanet(pid);

          // don't send ships from planets under attack, 
          if (a_pw.GetPlanet(pid).m_eventualOwner != 1)
          {
            continue;
          }

          // don't send ships from planets that could be under attack
          int numShipsAtPlanet = p.m_numShipsCanSafelySend;
          if (targetPlanet.m_eventualOwner == 2 && (targetPlanet.m_score - (m_enemyPlanetMul - 1.0) * (targetPlanet.m_growthRate * m_growthMul)) > 1.3 * p.m_score)
          {
            numShipsAtPlanet = p.m_numShips;
          }
          if (numShipsAtPlanet <= 0)
          {
            continue;
          }

          // don't send ships to neutral planets if there are any enemy fleets and our fleet would arrive before it is taken
          if (targetPlanet.m_owner == 0 && targetPlanet.m_actualEventualOwner == 2)
          {
            if (distances[pid] < targetPlanet.m_actualFuture.Count)
            {
              if (targetPlanet.m_actualFuture[distances[pid]].m_owner == 0)
              {
                continue;
              }
            }
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
            shipsToSend += extraDistance * (targetPlanet.m_growthRate + 3);
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

        // TODO: remove any incomingFleets that have same value for trip length and turns remaining
      }

      // TODO targetPlanet.CalcEventuality();
    }

    // if any planets have spare ships to send, send them to higher scoring planets
    foreach (Planet p in m_state.m_myPlanets)
    {
      if (p.m_numShipsCanSafelySend > 0)
      {
        List<double> dists = Precalc.Get.m_planetData[p.m_planetID].m_distancesD;
        Planet bestPlanet = null;
        double bestScore = 0;
        foreach (Planet p2 in m_state.m_current.m_planets)
        {
          if (p.m_planetID != p2.m_planetID && (p2.m_owner == 1 || p2.m_eventualOwner == 1))
          {
            double score = p2.m_score - Math.Max(0, 5500.0 / dists[p2.m_planetID]);
            if (score > p.m_score)
            {
              bestScore = score;
              bestPlanet = p2;
            }
          }
        }
        if (bestPlanet != null)
        {
          //bestPlanet.m_incomingFleets.Add(new Fleet(1, p.m_numShipsCanSafelySend, p.m_planetID, bestPlanet.m_planetID, 0, 0));
          //bestPlanet.m_incomingFleets.Sort(Fleet.CompareTurnsRemaining);
          //bestPlanet.CalcEventuality(); wont work anyway because distance for the fleet is 0
          m_state.CacheOrder(p.m_planetID, bestPlanet.m_planetID, p.m_numShipsCanSafelySend);
          m_state.SubmitOrders();
        }
      }
    }

    //long endtime = stopwatch.ElapsedMilliseconds;
    PlanetWars.FinishTurn();
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
        PlanetWars.FinishTurn();
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
