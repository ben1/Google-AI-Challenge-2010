using System;
using System.Collections.Generic;
using System.Text;

class Precalc
{
  public class PlanetData
  {
    public System.Collections.Generic.List<double> m_distancesD;
    public System.Collections.Generic.List<int> m_distances;
  }

  public static Precalc Get = new Precalc();

  public List<PlanetData> m_planetData;
  public int m_maxGrowth;

  public void Init(PlanetWars pw)
  {
    int numPlanets = pw.m_planets.Count;
    m_planetData = new List<PlanetData>(numPlanets);

    // pre-calc distances and find max growth value
    m_maxGrowth = 0;
    foreach (Planet p in pw.m_planets)
    {
      m_maxGrowth = Math.Max(m_maxGrowth, p.m_growthRate);

      PlanetData pd = new PlanetData();
      pd.m_distancesD = new List<double>(numPlanets);
      pd.m_distances = new List<int>(numPlanets);
      m_planetData.Add(pd);

      for(int i = 0; i < p.m_planetID; ++i)
      {
        pd.m_distancesD.Add(m_planetData[i].m_distancesD[p.m_planetID]);
        pd.m_distances.Add(m_planetData[i].m_distances[p.m_planetID]);
      }
      
      pd.m_distancesD.Add(0.0);
      pd.m_distances.Add((int)0);

      for(int i = p.m_planetID + 1; i < numPlanets; ++i)
      {
        Planet dest = pw.m_planets[i];
        double dx = p.m_x - dest.m_x;
        double dy = p.m_y - dest.m_y;
        double dist = Math.Ceiling(Math.Sqrt(dx * dx + dy * dy));
        pd.m_distancesD.Add(dist);
        pd.m_distances.Add((int)dist);
      }
    }

    // TODO: keep a list of the planets in order of distance
  }
}
