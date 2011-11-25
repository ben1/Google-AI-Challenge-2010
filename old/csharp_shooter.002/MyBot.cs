using System;
using System.Collections.Generic;

public class MyBot 
{
  bool m_firstTurn;
  Precalc m_precalc;
  State m_state;
  double m_growthMul;
  double m_myPlanetsDistMul;
  double m_enemyPlanetsDistMul;
  double m_costMul;
  double m_enemyPLanetMul;

  public MyBot()
  {
    m_firstTurn = true;
    m_precalc = new Precalc();
    m_state = new State(m_precalc);
    m_growthMul = 100.0;
    m_myPlanetsDistMul = 200.0;
    m_enemyPlanetsDistMul = 200.0;
    m_costMul = 10.0f;
    m_enemyPLanetMul = 4.0f;
  }

  public void DoTurn(PlanetWars a_pw) 
  {
    if (m_firstTurn)
    {
    //  m_firstTurn = false;
      m_precalc.Init(a_pw);

      // HACK: wait and see if we get under threat
      //a_pw.FinishTurn();
      //return;
    }

    m_state.InitTurn(a_pw);

    if (m_firstTurn)
    {
      m_firstTurn = false;
      Planet home = m_state.m_myPlanets[0];
      home.m_numShipsCanSafelySend = Math.Min(home.m_numShipsCanSafelySend, home.GrowthRate() * m_precalc.m_planetData[home.PlanetID()].m_distances[m_state.m_enemyPlanets[0].PlanetID()]);
    }

    int numPlanets = a_pw.Planets().Count;
    List<Planet> sortedScores = new List<Planet>(numPlanets);

    // calculate a score for each planet
    foreach (Planet p in a_pw.Planets())
    {
      // add score based on growth
      double score = p.GrowthRate() * m_growthMul;
      if (p.m_eventualOwner == 2)
      {
        score *= m_enemyPLanetMul;
      }

      // OPTIM: precalc inverse distance
      // OPTIM: only calculcate changes since last frame
      List<double> distancesD = m_precalc.m_planetData[p.PlanetID()].m_distancesD;

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

      // subtract score based on cost to take
      if (p.Owner() != 1 || p.m_eventualOwner != 1)
      {
        score -= m_costMul * (double)p.m_eventualNumShips;
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

    // keep sending ships until we don't want to send more or we can't send more
    for (int targetPlanetIteration = 1; targetPlanetIteration <= sortedScores.Count && m_state.m_myPlanetsWithShips.Count > 0; ++targetPlanetIteration)
    {
      // send the closest ships to the best scoring planet, next closest to next planet, etc.
      Planet targetPlanet = sortedScores[sortedScores.Count - targetPlanetIteration];

      // don't send more ships if we're going to own it eventually anyway
      if (targetPlanet.m_eventualOwner == 1)
      {
        continue;
      }

      // find our closest planet then next closest planet etc until we've fulfilled the total shipsToSend
      // TODO: don't send ships if our planet is in danger
      int shipsToSend = targetPlanet.m_eventualNumShips + 1;
      while (shipsToSend > 0 && m_state.m_myPlanetsWithShips.Count > 0)
      {
        // find the closest planet that can send ships
        int bestIndex = -1;
        int bestPlanetID = -1;
        int bestDistance = 1000000;
        int bestShipsCanSend = 0;

        List<int> distances = m_precalc.m_planetData[targetPlanet.PlanetID()].m_distances;
        for (int i = 0; i < m_state.m_myPlanetsWithShips.Count; ++i)
        {
          int pid = m_state.m_myPlanetsWithShips[i];
          Planet p = a_pw.GetPlanet(pid);

          // don't send ships from planets under attack, 
          if (a_pw.GetPlanet(pid).m_eventualOwner != 1)
          {
            continue;
          }

          // don't send ships from planets that could be under attack
          int numShipsAtPlanet = (targetPlanet.m_score > m_enemyPLanetMul * p.m_score) ? p.NumShips() : p.m_numShipsCanSafelySend;
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

    a_pw.FinishTurn();
  }



//-------------------------------------------------------------------------------
  public static void Main(string[] a_args)
  {
    int turn = 0;
    string line = Console.ReadLine();
    string message = "";
    MyBot bot = new MyBot();
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
    } 
    catch (Exception) 
    {
      // Owned.
    }
  }
}

