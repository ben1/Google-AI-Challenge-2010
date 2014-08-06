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
    public static List<double> s_shipsSent = new List<double>(30);

#if PW_DEBUG
    List<double> m_opts;
#endif

    private void Init()
    {
        m_initialised = false;
        m_state = new State();
        m_growthMul = 151.0;
        m_myPlanetsDistMul = 875.0;
        m_enemyPlanetsDistMul = 50.0;
        m_costMul = 12.0f;
        m_enemyPlanetMul = 6.1f;
    }
    public MyBot()
    {
        Init();
    }

#if PW_DEBUG
    public MyBot(string a_opts)
    {
        Init();

        string[] opts = a_opts.Trim().Split(',');
        m_opts = new List<double>();
        foreach (string a in opts)
        {
            m_opts.Add(double.Parse(a));
        }
    }
#endif

    public void DoTurn(PlanetWars a_pw)
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        if (!m_initialised)
        {
            m_initialised = true;
            Precalc.Get.Init(a_pw);
            for (int i = 0; i < a_pw.NumPlanets(); ++i)
            {
                s_shipsSent.Add(0);
            }
        }

        m_state.InitTurn(a_pw);

        int numPlanets = a_pw.m_planets.Count;
        List<Planet> sortedScores = new List<Planet>(numPlanets);

        double invEnemyCount = 1.0 / (double)m_state.m_enemyPlanets.Count;
        double invMyCount = 1.0 / (double)m_state.m_myPlanets.Count;

        int pnum = 0;
        // calculate a score for each planet
        foreach (Planet p in a_pw.m_planets)
        {
            // add score based on growth
            double score = p.m_growthRate * m_growthMul;
            if (p.m_originalFuture.m_eventualOwner == 2)
            {
                score *= m_enemyPlanetMul;
            }

            List<double> invDistancesD = Precalc.Get.m_planetData[p.m_planetID].m_invDistancesD;

            // add score based on inverse distance to my planets
            double myPlanetsDistMul = m_myPlanetsDistMul * invMyCount;
            foreach (Planet myp in m_state.m_myPlanets)
            {
                if (myp.m_planetID == p.m_planetID)
                {
                    score += myPlanetsDistMul;
                    continue;
                }
                double invDist = invDistancesD[myp.m_planetID];
                score += myPlanetsDistMul * invDist;
            }

            // add score based on inverse distance to enemy planets
            double enemyPlanetsDistMul = m_enemyPlanetsDistMul * invEnemyCount;
            foreach (Planet enemyp in m_state.m_enemyPlanets)
            {
                if (enemyp.m_planetID == p.m_planetID)
                {
                    score += enemyPlanetsDistMul;
                    continue;
                }
                double invDist = invDistancesD[enemyp.m_planetID];
                score += enemyPlanetsDistMul * invDist;
            }

            // subtract score based on cost to take (only subtract if taking it from neutral because taking it from enemy is good)
            if (p.m_testFuture.m_eventualOwner == 0) // was m_owner, but we should really be checking against the owner N turns ahead where N is distance to us, so probably better to use the eventual owner anyway
            {
                score -= m_costMul * (double)p.m_numShips;
            }

            score += (s_shipsSent[pnum] * 1000);
            pnum++;

            // safety
            if (double.IsInfinity(score))
            {
                score = 0.0f;
            }

            p.m_score = score;
            sortedScores.Add(p);
        }

        // inverse exponential decrease in the weight we give to planets we are already sending ships to
        for (int i = 0; i < a_pw.NumPlanets(); ++i)
        {
            s_shipsSent[i] *= 0.5;
        }

        // sort
        sortedScores.Sort(Planet.CompareScore);

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

            // don't attempt to go directly for the enemy home base on the first turn if they are very close, it is a massive gamble.
            if (m_state.m_current.m_turn == 1)
            {
                List<int> dists = Precalc.Get.m_planetData[targetPlanet.m_planetID].m_distances;
                if (dists[m_state.m_enemyPlanets[0].m_planetID] <= dists[m_state.m_myPlanets[0].m_planetID] - 5)
                {
                    continue;
                }
            }
#if PW_DEBUG
            if ((double)targetPlanetIteration * (m_opts[0] - ((double)m_state.m_current.m_turn / m_opts[1])) > (double)sortedScores.Count)
#else
      if ((double)targetPlanetIteration * (2.3 - ((double)m_state.m_current.m_turn * s_inv190)) > (double)sortedScores.Count)
#endif
            {
                break;
            }
#if PW_DEBUG
            if (targetPlanet.m_score < targetPlanet.m_growthRate * -m_opts[0] + m_opts[1])
#else
      if (targetPlanet.m_score < targetPlanet.m_growthRate * -80 + 260)
#endif
            {
                break;
            }

            // Threaten the planet with enemy ships from the closest planet
            // THIS WHOLE BLOCK IS WRONG BUT GETS THE BEST SCORE!
            if (targetPlanet.m_owner != 0)
            {
                int cepid2 = -1;
                int cepid = m_state.GetClosestEnemyPlanet(targetPlanet.m_planetID, out cepid2);
                if (cepid >= 0)
                {
                    Planet e = a_pw.GetPlanet(cepid);
                    targetPlanet.m_testFuture.m_incomingFleets.Add(new Fleet(2, e.m_numShips, e.m_planetID, targetPlanet.m_planetID, Precalc.Get.m_planetData[cepid].m_distances[targetPlanet.m_planetID], Precalc.Get.m_planetData[cepid].m_distances[targetPlanet.m_planetID]));
                    targetPlanet.m_testFuture.m_incomingFleets.Sort(Fleet.CompareTurnsRemaining);
                    targetPlanet.m_testFuture.CalcEventuality();
                }

                if (cepid2 >= 0)
                {
                    Planet e = a_pw.GetPlanet(cepid2);
                    targetPlanet.m_testFuture.m_incomingFleets.Add(new Fleet(2, e.m_numShips, e.m_planetID, targetPlanet.m_planetID, Precalc.Get.m_planetData[cepid2].m_distances[targetPlanet.m_planetID], Precalc.Get.m_planetData[cepid2].m_distances[targetPlanet.m_planetID]));
                    targetPlanet.m_testFuture.m_incomingFleets.Sort(Fleet.CompareTurnsRemaining);
                    targetPlanet.m_testFuture.CalcEventuality();
                }
            }

            // don't send more ships if we're going to own it eventually anyway
            if (targetPlanet.m_testFuture.m_eventualOwner == 1)
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

                    if (pid == targetPlanet.m_planetID)
                    {
                        continue;
                    }

                    Planet p = a_pw.GetPlanet(pid);

                    // don't send ships from planets under attack, 
                    if (a_pw.GetPlanet(pid).m_testFuture.m_eventualOwner != 1)
                    {
                        continue;
                    }

                    // don't send ships from planets that could be under attack
                    int numShipsAtPlanet = p.m_numShipsCanSafelySend;
                    if (targetPlanet.m_testFuture.m_eventualOwner == 2 && (targetPlanet.m_score - (m_enemyPlanetMul - 1.0) * (targetPlanet.m_growthRate * m_growthMul)) > 1.3 * p.m_score)
                    {
                        numShipsAtPlanet = p.m_numShips;
                    }
                    if (numShipsAtPlanet <= 0)
                    {
                        continue;
                    }

                    // don't send ships to neutral planets if there are any enemy fleets and our fleet would arrive before it is taken
                    if (targetPlanet.m_owner == 0 && targetPlanet.m_testFuture.m_eventualOwner == 2)
                    {
                        if (distances[pid] < targetPlanet.m_testFuture.m_future.Count)
                        {
                            if (targetPlanet.m_testFuture.m_future[distances[pid]].m_owner == 0)
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
                int futureTurns = targetPlanet.m_testFuture.m_future.Count - 1;
                if (bestDistance <= futureTurns)
                {
                    shipsToSend = targetPlanet.m_testFuture.m_future[bestDistance].EnemyShips() + 1;
                }
                else
                {
                    shipsToSend = targetPlanet.m_testFuture.m_future[futureTurns].EnemyShips() + 1;

                    // if finally owned by 2, add growth until we would get there
                    if (targetPlanet.m_testFuture.m_future[futureTurns].m_owner == 2)
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
                        targetPlanet.m_testFuture.m_incomingFleets.Add(new Fleet(1, bestShipsCanSend, bestPlanetID, targetPlanet.m_planetID, bestDistance, bestDistance));
                        targetPlanet.m_testFuture.m_incomingFleets.Sort(Fleet.CompareTurnsRemaining);
                        targetPlanet.m_testFuture.CalcEventuality();
                        m_state.CacheOrder(bestPlanetID, targetPlanet.m_planetID, bestShipsCanSend);
                    }
                    else
                    {
                        targetPlanet.m_testFuture.m_incomingFleets.Add(new Fleet(1, shipsToSend, bestPlanetID, targetPlanet.m_planetID, bestDistance, bestDistance));
                        targetPlanet.m_testFuture.m_incomingFleets.Sort(Fleet.CompareTurnsRemaining);
                        targetPlanet.m_testFuture.CalcEventuality();
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
                    if (p.m_planetID != p2.m_planetID && (p2.m_owner == 1 || p2.m_testFuture.m_eventualOwner == 1))
                    {
                        //double score2 = p2.m_score - m_opts[0] * dists[p2.m_planetID] - m_opts[1] * Precalc.Get.m_planetData[p.m_planetID].m_distToCentre + m_opts[2];
                        double score2 = p2.m_score - 140 * dists[p2.m_planetID] - 120 * Precalc.Get.m_planetData[p.m_planetID].m_distToCentre + 900;
                        if (score2 > p.m_score)
                        {
                            bestScore = score2;
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
    }


    //-------------------------------------------------------------------------------
    public static void Main(string[] a_args)
    {
        int turn = 0;
        string line = Console.ReadLine();
        string message = "";
        MyBot bot = null;
#if PW_DEBUG
        if (a_args.Length > 1 && a_args[0] == "-opts")
        {
            bot = new MyBot(a_args[1]);
        }
        else
#endif
        {
            bot = new MyBot();
        }

        while (turn >= 0)
        {
            PlanetWars pw = null;
            try
            {
                while (line != null)
                {
                    if (line.Equals("go"))
                    {
                        turn++;
                        pw = new PlanetWars(message, turn);
                        if (pw.m_turn == turn)
                        {
                            bot.DoTurn(pw);
                        }
                        PlanetWars.FinishTurn();
                        message = "";
                    }
                    else
                    {
                        message += line + "\n";
                    }
                    line = Console.ReadLine();
                }
                turn = -1000; // finish
            }
            catch (Exception)
            {
                // Owned, but retry
                PlanetWars.FinishTurn();
            }
        }
    }
}
