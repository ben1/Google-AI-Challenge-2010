using System;
using System.Collections.Generic;

public class Planet 
{
  public int m_planetID;
  public int m_owner;
  public int m_numShips;
  public int m_growthRate;
  public double m_x, m_y;
  public double m_score;

  public int m_futureMinNumShips;
  public int m_numShipsCanSafelySend;

  public List<Fleet> m_actualIncomingFleets;
  public List<Fleet> m_incomingFleets;
  public List<FuturePlanet> m_actualFuture;
  public List<FuturePlanet> m_future;
  public int m_actualEventualOwner;
  public int m_eventualOwner;


  public Planet(int planetID, int owner, int numShips, int growthRate, double x, double y)
  {
    this.m_planetID = planetID;
    this.m_owner = owner;
    this.m_numShips = numShips;
    this.m_growthRate = growthRate;
    this.m_x = x;
    this.m_y = y;
  }

  public void Owner(int newOwner) { m_owner = newOwner; }
  public void NumShips(int newNumShips) { m_numShips = newNumShips; }
  public void AddShips(int amount) { m_numShips += amount; }
  public void RemoveShips(int amount) { m_numShips -= amount; }
  
  public static int CompareScore(Planet a_1, Planet a_2)
  {
    if (a_1.m_score > a_2.m_score)
    {
      return 1;
    }
    else if (a_1.m_score < a_2.m_score)
    {
      return -1;
    }
    return 0;
  }

//  public int FindClosestPlanetThatCanSendShips()
//  {
//    List<int> distances = Precalc.Get.m_planetData[m_planetID].m_distances;
//    int closest = -1;
////    int closestDist = 1000000;
//    for (int i = 0; i < distances.Count; ++i)
//    {
//    }
//    return closest;
//  }

  public void CalcEventuality()
  {
    m_eventualOwner = m_owner;
    m_future = new List<FuturePlanet>();

    // add turn 0
    FuturePlanet fpTurn0 = new FuturePlanet();
    fpTurn0.m_owner = m_eventualOwner;
    fpTurn0.m_ships[0] = 0;
    fpTurn0.m_ships[1] = 0;
    fpTurn0.m_ships[2] = 0;
    fpTurn0.m_ships[fpTurn0.m_owner] = m_numShips;
    m_future.Add(fpTurn0);

    int[] ships = new int[3] { 0, 0, 0 };
    ships[m_owner] = m_numShips;
    m_futureMinNumShips = m_numShips;

    int turn = 0;
    int fleet = 0;
    while (fleet < m_incomingFleets.Count)
    {
      Fleet f = m_incomingFleets[fleet];
      if (f.m_turnsRemaining == 0)
      {
        return;
      }

      if (f.m_turnsRemaining == turn)
      {
        // process a fleet
        ships[f.m_owner] += f.m_numShips;
        fleet++;

        // if it's the last fleet this turn, process the battle
        if (fleet == m_incomingFleets.Count || m_incomingFleets[fleet].m_turnsRemaining > f.m_turnsRemaining)
        {
          ResolveBattle(ships, ref m_eventualOwner);

          FuturePlanet fp = new FuturePlanet();
          fp.m_owner = m_eventualOwner;
          fp.m_ships[0] = 0;
          fp.m_ships[1] = 0;
          fp.m_ships[2] = 0;
          fp.m_ships[fp.m_owner] = m_numShips;
          m_future.Add(fp);
        }
      }
      else
      {
        // process a turn - only players get growth
        if (m_eventualOwner != 0)
        {
          ships[m_eventualOwner] += m_growthRate;
        }
        turn++;
      }

      // update min number of our ships
      m_futureMinNumShips = Math.Min(m_futureMinNumShips, ships[m_owner]);
    }
  }

  private void ResolveBattle(int[] a_ships, ref int a_eventualOwner)
  {
    if (a_ships[0] > a_ships[1])
    {
      // 0 > 1
      if (a_ships[0] > a_ships[2])
      {
        // 0 > 1 && 0 > 2
        a_ships[0] -= Math.Max(a_ships[1], a_ships[2]);
        a_ships[1] = 0;
        a_ships[2] = 0;
        a_eventualOwner = 0;
      }
      else
      {
        // 0 > 1 && 2 >= 0
        a_ships[2] -= a_ships[0];
        a_ships[0] = 0;
        a_ships[1] = 0;
        if (a_ships[2] > 0)
        {
          a_eventualOwner = 2;
        }
        // else owner doesn't change
      }
    }
    else
    {
      // 1 >= 0
      if (a_ships[0] > a_ships[2])
      {
        // 1 >= 0 && 0 > 2
        a_ships[1] -= a_ships[0];
        a_ships[0] = 0;
        a_ships[2] = 0;
        if (a_ships[1] > 0)
        {
          a_eventualOwner = 1;
        }
        // else owner doesn't change
      }
      else
      {
        // 1 >= 0 && 2 >= 0
        if (a_ships[1] > a_ships[2])
        {
          a_ships[1] -= a_ships[2];
          a_ships[0] = 0;
          a_ships[2] = 0;
          a_eventualOwner = 1;
        }
        else
        {
          a_ships[2] -= a_ships[1];
          a_ships[0] = 0;
          a_ships[1] = 0;
          if (a_ships[2] > 0)
          {
            a_eventualOwner = 2;
          }
          // else owner doesn't change
        }
      }
    }

  }
}
