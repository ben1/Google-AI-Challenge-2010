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
    int numPlanets = pw.Planets().Count;
    m_planetData = new List<PlanetData>(numPlanets);

    // pre-calc distances and find max growth value
    m_maxGrowth = 0;
    foreach (Planet p in pw.Planets())
    {
      m_maxGrowth = Math.Max(m_maxGrowth, p.GrowthRate());

      PlanetData pd = new PlanetData();
      pd.m_distancesD = new List<double>(numPlanets);
      pd.m_distances = new List<int>(numPlanets);
      m_planetData.Add(pd);

      for(int i = 0; i < p.PlanetID(); ++i)
      {
        pd.m_distancesD.Add(m_planetData[i].m_distancesD[p.PlanetID()]);
        pd.m_distances.Add(m_planetData[i].m_distances[p.PlanetID()]);
      }
      
      pd.m_distancesD.Add(0.0);
      pd.m_distances.Add((int)0);

      for(int i = p.PlanetID() + 1; i < numPlanets; ++i)
      {
        Planet dest = pw.Planets()[i];
        double dx = p.X() - dest.X();
        double dy = p.Y() - dest.Y();
        double dist = Math.Ceiling(Math.Sqrt(dx * dx + dy * dy));
        pd.m_distancesD.Add(dist);
        pd.m_distances.Add((int)dist);
      }
    }

    // TODO: keep a list of the planets in order of distance
  }
}
