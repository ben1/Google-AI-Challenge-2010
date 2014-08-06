using System;
using System.Collections.Generic;
using System.Text;

struct Order
{
    public Order(int a_from, int a_to, int a_num)
    {
        m_from = a_from;
        m_to = a_to;
        m_num = a_num;
    }
    public int m_from;
    public int m_to;
    public int m_num;
};

public class State
{
    List<Order> m_orders;
    public List<int> m_myPlanetsWithShips;
    public PlanetWars m_current;
    public List<Planet> m_myPlanets;
    public List<Planet> m_enemyPlanets;
    public int m_myTotalProd;
    public int m_enemyTotalProd;

    public State()
    {
        m_orders = new List<Order>(24);
        m_myPlanetsWithShips = new List<int>(24);
        m_myTotalProd = 0;
        m_enemyTotalProd = 0;
    }


    public void InitTurn(PlanetWars a_pw)
    {
        m_current = a_pw;
        m_myPlanets = m_current.MyPlanets();
        m_enemyPlanets = m_current.EnemyPlanets();
        m_myPlanetsWithShips.RemoveRange(0, m_myPlanetsWithShips.Count);
        foreach (Planet p in m_myPlanets)
        {
            m_myPlanetsWithShips.Add(p.m_planetID);
            m_myTotalProd += p.m_growthRate;
        }
        m_orders.RemoveRange(0, m_orders.Count);

        foreach (Planet p in m_enemyPlanets)
        {
            m_enemyTotalProd += p.m_growthRate;
        }

        foreach (Fleet f in a_pw.Fleets())
        {
            a_pw.GetPlanet(f.m_destinationPlanet).m_originalFuture.m_incomingFleets.Add(f);
        }
        foreach (Planet p in a_pw.m_planets)
        {
            p.m_originalFuture.m_incomingFleets.Sort(Fleet.CompareTurnsRemaining);
            p.m_originalFuture.CalcEventuality();
            p.m_testFuture = new Future(p.m_originalFuture);

            p.m_numShipsCanSafelySend = 0;

            if (p.m_owner == 1 && p.m_originalFuture.m_eventualOwner == 1)
            {
                // get closest few enemy planets (maybe only enemy planets with a better score than me)
                // add fake fleets from them
                // recalc eventual owner and get the minimum number of ships that my planet frops to.

                p.m_numShipsCanSafelySend = Math.Max(0, Math.Min(p.m_numShips, p.m_originalFuture.m_minNumShips[1]));

                // just make sure the closest enemy planet can't nuke us
                int cepid = GetClosestEnemyPlanet(p.m_planetID);
                if (cepid >= 0)
                {
                    Planet cep = m_current.GetPlanet(cepid);
                    int dist = Precalc.Get.m_planetData[p.m_planetID].m_distances[cep.m_planetID];
                    p.m_numShipsCanSafelySend = Math.Max(0, Math.Min(p.m_numShipsCanSafelySend, p.m_numShipsCanSafelySend + dist * p.m_growthRate - cep.m_numShips));
                }
                // adding cep2 into it didn't make it better
            }
        }
    }

    public void CacheOrder(int a_from, int a_to, int a_num)
    {
        // check for planets out of range
        if (a_from < 0 || a_from >= m_current.m_planets.Count || a_to < 0 || a_to >= m_current.m_planets.Count)
        {
#if PW_DEBUG
            System.Diagnostics.Debug.Assert(false);
#endif
            return;
        }

        Planet p = m_current.GetPlanet(a_from);

        // check for not the owner or not enough ships
        if (p.m_owner != 1 || a_num <= 0 || p.m_numShips < a_num)
        {
#if PW_DEBUG
            System.Diagnostics.Debug.Assert(false);
#endif
            return;
        }

        m_orders.Add(new Order(a_from, a_to, a_num));
        p.RemoveShips(a_num);
        p.m_numShipsCanSafelySend -= a_num;
        if (p.m_numShips == 0)
        {
            m_myPlanetsWithShips.Remove(a_from);
        }
    }

    public void RevertOrders()
    {
        foreach (Order o in m_orders)
        {
            Planet p = m_current.GetPlanet(o.m_from);
            p.AddShips(o.m_num);
            p.m_numShipsCanSafelySend += o.m_num;
            if (!m_myPlanetsWithShips.Contains(o.m_from))
            {
                m_myPlanetsWithShips.Add(o.m_from);
            }
        }
        m_orders.RemoveRange(0, m_orders.Count);
    }

    public void SubmitOrders()
    {
        int maxTurns = 0;
        int totalShips = 0;

        // find the longest fleet we would send and don't send earlier that than would arrive.
        foreach (Order o in m_orders)
        {
            maxTurns = Math.Max(maxTurns, Precalc.Get.m_planetData[o.m_from].m_distances[o.m_to]);
            totalShips += o.m_num;
        }

        foreach (Order o in m_orders)
        {
            if (Precalc.Get.m_planetData[o.m_from].m_distances[o.m_to] >= maxTurns)
            {
                m_current.IssueOrder(o.m_from, o.m_to, o.m_num);
                MyBot.s_shipsSent[o.m_to] += o.m_num;
            }
            else
            {
                // remove order
                Planet p = m_current.GetPlanet(o.m_from);
                p.AddShips(o.m_num);
                //p.m_numShipsCanSafelySend += o.m_num;
                if (!m_myPlanetsWithShips.Contains(o.m_from))
                {
                    m_myPlanetsWithShips.Add(o.m_from);
                }
            }
        }
        m_orders.RemoveRange(0, m_orders.Count);
    }

    public List<int> GetClosestMyPlanets(int a_pid)
    {
        List<int> ret = new List<int>(m_myPlanets.Count);
        foreach (Planet p in m_myPlanets)
        {
            ret.Add(p.m_planetID);
        }
        s_dists = Precalc.Get.m_planetData[a_pid].m_distances;
        ret.Sort(CompareDistances);
        return ret;
    }

    public List<int> GetClosestEnemyPlanets(int a_pid)
    {
        List<int> ret = new List<int>(m_enemyPlanets.Count);
        foreach (Planet p in m_enemyPlanets)
        {
            ret.Add(p.m_planetID);
        }
        s_dists = Precalc.Get.m_planetData[a_pid].m_distances;
        ret.Sort(CompareDistances);
        return ret;
    }

    static List<int> s_dists = null;

    public static int CompareDistances(int a_1, int a_2)
    {
        return s_dists[a_1] - s_dists[a_2];
    }

    public int GetClosestEnemyPlanet(int a_pid)
    {
        int shortestDistance = 1000000;
        int closestPid = -1;
        List<int> dists = Precalc.Get.m_planetData[a_pid].m_distances;
        foreach (Planet p in m_enemyPlanets)
        {
            if (a_pid != p.m_planetID && dists[p.m_planetID] < shortestDistance)
            {
                closestPid = p.m_planetID;
                shortestDistance = dists[closestPid];
            }
        }
        return closestPid;
    }

    public int GetClosestEnemyPlanet(int a_pid, out int a_secondClosestPid)
    {
        int shortestDistance = 1000000;
        int closestPid = -1;
        a_secondClosestPid = -1;
        List<int> dists = Precalc.Get.m_planetData[a_pid].m_distances;
        foreach (Planet p in m_enemyPlanets)
        {
            if (a_pid != p.m_planetID && dists[p.m_planetID] < shortestDistance)
            {
                a_secondClosestPid = closestPid;
                closestPid = p.m_planetID;
                shortestDistance = dists[closestPid];
            }
        }
        return closestPid;
    }
}
