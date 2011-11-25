using System;
using System.Collections.Generic;
using System.Text;

public class FuturePlanet
{
  public int[] m_ships = new int[3];
  public int m_owner;

  public int EnemyShips()
  {
    return Math.Max(m_ships[0], m_ships[2]);
  }
}

