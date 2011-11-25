using System;
using System.Collections.Generic;

public class Planet 
{
  // Initializes a planet.
  public Planet(int planetID, int owner, int numShips, int growthRate, double x, double y)
  {
    this.planetID = planetID;
    this.m_owner = owner;
    this.numShips = numShips;
    this.growthRate = growthRate;
    this.x = x;
    this.y = y;
  }

  public int PlanetID() { return planetID; }
  public int Owner() { return m_owner; }
  public int NumShips() { return numShips; }
  public int GrowthRate() { return growthRate; }
  public double X() { return x; }
  public double Y() { return y; }
  public void Owner(int newOwner) { this.m_owner = newOwner; }
  public void NumShips(int newNumShips) { this.numShips = newNumShips; }
  public void AddShips(int amount) { numShips += amount; }

  public void RemoveShips(int amount) { numShips -= amount; }
  
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

  private int planetID;
  private int m_owner;
  private int numShips;
  private int growthRate;
  public double m_score;
  public int m_numShipsCanSafelySend;
  public FuturePlanet m_future;
  private double x, y;


  public int FindClosestPlanetThatCanSendShips()
  {
    List<int> distances = Precalc.Get.m_planetData[planetID].m_distances;
    int closest = -1;
//    int closestDist = 1000000;
    for (int i = 0; i < distances.Count; ++i)
    {
    }
    return closest;
  }

}
