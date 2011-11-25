using System;
using System.Collections.Generic;
using System.Text;

class Precalc
{
  public class PlanetData
  {
    public System.Collections.Generic.List<double> m_distancesD;
    public System.Collections.Generic.List<int> m_distances;
    public double m_distToCentre;
  }

  public static Precalc Get = new Precalc();

  public List<PlanetData> m_planetData;
  public int m_maxGrowth;
  public double m_centreX;
  public double m_centreY;
  public void Init(PlanetWars pw)
  {
    int numPlanets = pw.m_planets.Count;
    m_planetData = new List<PlanetData>(numPlanets);

    // pre-calc distances and find max growth value
    m_maxGrowth = 0;
    m_centreX = 0;
    m_centreY = 0;
    foreach (Planet p in pw.m_planets)
    {
      m_maxGrowth = Math.Max(m_maxGrowth, p.m_growthRate);
      m_centreX += p.m_x;
      m_centreY += p.m_y;

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

    m_centreX /= (double)numPlanets;
    m_centreY /= (double)numPlanets;

    for(int i = 0; i < pw.m_planets.Count; ++i)
    {
      Planet p = pw.m_planets[i];
      double dx = p.m_x - m_centreX;
      double dy = p.m_y - m_centreY;
      m_planetData[i].m_distToCentre = Math.Ceiling(Math.Sqrt(dx * dx + dy * dy));
    }
    // TODO: keep a list of the planets in order of distance
  }
}
