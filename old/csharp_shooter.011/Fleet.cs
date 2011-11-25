using System;
using System.Collections.Generic;

public class Fleet 
{
  public int m_owner;
  public int m_numShips;
  public int m_sourcePlanet;
  public int m_destinationPlanet;
  public int m_totalTripLength;
  public int m_turnsRemaining;

  public Fleet(int owner, int numShips, int sourcePlanet, int destinationPlanet, int totalTripLength, int turnsRemaining)
  {
	  m_owner = owner;
	  m_numShips = numShips;
	  m_sourcePlanet = sourcePlanet;
	  m_destinationPlanet = destinationPlanet;
	  m_totalTripLength = totalTripLength;
	  m_turnsRemaining = turnsRemaining;
  }

  public void RemoveShips(int amount) 
  {
  	m_numShips -= amount;
  }

  public static int CompareTurnsRemaining(Fleet a_1, Fleet a_2)
  {
    return a_1.m_turnsRemaining - a_2.m_turnsRemaining;
  }
}
