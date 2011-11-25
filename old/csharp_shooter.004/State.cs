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
  private Precalc m_precalc;
  public PlanetWars m_current;
  public List<Planet> m_myPlanets;
  public List<Planet> m_enemyPlanets;
  public List<int> m_myPlanetsWithShips;
  List<Order> m_orders;

  public State(Precalc a_precalc)
  {
    m_precalc = a_precalc;
    m_myPlanetsWithShips = new List<int>(32);
    m_orders = new List<Order>(32);
  }

  public void InitTurn(PlanetWars a_pw)
  {
    m_current = a_pw;
    m_myPlanets = m_current.MyPlanets();
    m_enemyPlanets = m_current.EnemyPlanets();
    m_myPlanetsWithShips.RemoveRange(0, m_myPlanetsWithShips.Count);
    foreach (Planet p in m_myPlanets)
    {
      m_myPlanetsWithShips.Add(p.PlanetID());
    }
    m_orders.RemoveRange(0, m_orders.Count);

    foreach (Planet p in a_pw.Planets())
    {
      p.m_incomingFleets = new List<Fleet>();
    }
    foreach (Fleet f in a_pw.Fleets())
    {
      a_pw.GetPlanet(f.DestinationPlanet()).m_incomingFleets.Add(f);
    }
    foreach (Planet p in a_pw.Planets())
    {
      p.m_incomingFleets.Sort(Fleet.CompareTurnsRemaining);
      p.CalcEventuality();

      p.m_numShipsCanSafelySend = 0;
      if (p.Owner() == 1 && p.m_eventualOwner == 1)
      {
        // get closest few enemy planets (maybe only enemy planets with a better score than me)
        // add fake fleets from them
        // recalc eventual owner and get the minimum number of ships that my planet frops to.

        p.m_numShipsCanSafelySend = Math.Max(0, Math.Min(p.NumShips(), p.m_eventualNumShips));

        // just make sure the closest enemy planet can't nuke us
        int cepid = GetClosestEnemyPlanet(p.PlanetID());
        if (cepid >= 0)
        {
          Planet cep = m_current.GetPlanet(cepid);
          int dist = m_precalc.m_planetData[p.PlanetID()].m_distances[cep.PlanetID()];
          p.m_numShipsCanSafelySend = Math.Max(0, Math.Min(p.m_numShipsCanSafelySend, p.m_numShipsCanSafelySend + dist * p.GrowthRate() - cep.NumShips()));
        }
      }
    }
  }

  public void CacheOrder(int a_from, int a_to, int a_num)
  {
    m_orders.Add(new Order(a_from, a_to, a_num));
    Planet p = m_current.GetPlanet(a_from);
    if (a_num <= 0 || p.NumShips() < a_num)
    {
      return;
    }
    p.RemoveShips(a_num);
    p.m_numShipsCanSafelySend -= a_num;
    if (p.NumShips() == 0)
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

  int GetClosestEnemyPlanet(int a_myPlanet)
  {
    int shortestDistance = 1000000;
    int ep = -1;
    List<int> dists = m_precalc.m_planetData[a_myPlanet].m_distances;
    foreach(Planet p in m_enemyPlanets)
    {
      if(dists[p.PlanetID()] < shortestDistance)
      {
        ep = p.PlanetID();
        shortestDistance = dists[ep];
      }
    }
    return ep;
  }
}
