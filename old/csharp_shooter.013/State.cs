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

class State
{
  List<Order> m_orders;
  public List<int> m_myPlanetsWithShips;
  public PlanetWars m_current;
  public List<Planet> m_myPlanets;
  public List<Planet> m_enemyPlanets;

  public State()
  {
    m_orders = new List<Order>(32);
    m_myPlanetsWithShips = new List<int>(32);
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
    }
    m_orders.RemoveRange(0, m_orders.Count);

    foreach (Planet p in a_pw.m_planets)
    {
      p.m_actualIncomingFleets = new List<Fleet>();
    }
    foreach (Fleet f in a_pw.Fleets())
    {
      a_pw.GetPlanet(f.m_destinationPlanet).m_actualIncomingFleets.Add(f);
    }
    foreach (Planet p in a_pw.m_planets)
    {
      p.m_actualIncomingFleets.Sort(Fleet.CompareTurnsRemaining);
      p.m_incomingFleets = new List<Fleet>();
      p.m_incomingFleets.AddRange(p.m_actualIncomingFleets);
      p.CalcEventuality();
      p.m_actualEventualOwner = p.m_eventualOwner;
      p.m_actualFuture = new List<FuturePlanet>(p.m_future.Count);
      p.m_actualFuture.AddRange(p.m_future);

      p.m_numShipsCanSafelySend = 0;
      if (p.m_owner == 1 && p.m_actualEventualOwner == 1)
      {
        // get closest few enemy planets (maybe only enemy planets with a better score than me)
        // add fake fleets from them
        // recalc eventual owner and get the minimum number of ships that my planet frops to.

        p.m_numShipsCanSafelySend = Math.Max(0, Math.Min(p.m_numShips, p.m_futureMinNumShips));

        // just make sure the closest enemy planet can't nuke us
        int cepid = GetClosestEnemyPlanet(p.m_planetID);
        if (cepid >= 0)
        {
          Planet cep = m_current.GetPlanet(cepid);
          int dist = Precalc.Get.m_planetData[p.m_planetID].m_distances[cep.m_planetID];
          p.m_numShipsCanSafelySend = Math.Max(0, Math.Min(p.m_numShipsCanSafelySend, p.m_numShipsCanSafelySend + dist * p.m_growthRate - cep.m_numShips));
        }
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
    foreach (Order o in m_orders)
    {
      m_current.IssueOrder(o.m_from, o.m_to, o.m_num);
    }
    m_orders.RemoveRange(0, m_orders.Count);
  }

  public int GetClosestEnemyPlanet(int a_myPlanet)
  {
    int shortestDistance = 1000000;
    int ep = -1;
    List<int> dists = Precalc.Get.m_planetData[a_myPlanet].m_distances;
    foreach(Planet p in m_enemyPlanets)
    {
      if(dists[p.m_planetID] < shortestDistance)
      {
        ep = p.m_planetID;
        shortestDistance = dists[ep];
      }
    }
    return ep;
  }
}
