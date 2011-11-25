using System;
using System.Collections.Generic;

public class Planet {
    // Initializes a planet.
    public Planet(int planetID,
                  int owner,
		  int numShips,
		  int growthRate,
		  double x,
		  double y) {
	this.planetID = planetID;
	this.m_owner = owner;
	this.numShips = numShips;
	this.growthRate = growthRate;
	this.x = x;
	this.y = y;
    }

    // Accessors and simple modification functions. These should be mostly
    // self-explanatory.
    public int PlanetID() {
	return planetID;
    }

    public int Owner() {
	return m_owner;
    }

    public int NumShips() {
	return numShips;
    }

    public int GrowthRate() {
	return growthRate;
    }

    public double X() {
	return x;
    }

    public double Y() {
	return y;
    }

    public void Owner(int newOwner) {
	this.m_owner = newOwner;
    }

    public void NumShips(int newNumShips) {
	this.numShips = newNumShips;
    }

    public void AddShips(int amount) {
	numShips += amount;
    }

    public void RemoveShips(int amount) {
	numShips -= amount;
    }


    public void CalcEventuality()
    {
      m_eventualOwner = m_owner;

      if (m_incomingFleets.Count == 0)
      {
        m_eventualNumShips = numShips;
        if (m_owner != 0) // only players get growth
        {
          m_eventualNumShips += growthRate * 5; // estimate how many ships will be there in an average number of turns
        }
        return;
      }

      int[] ships = new int[3] { 0, 0, 0 };
      ships[m_owner] = numShips;
      int turn = 0;
      int fleet = 0;
      while(fleet < m_incomingFleets.Count)
      {
        Fleet f = m_incomingFleets[fleet];
        if (f.TurnsRemaining() == 0)
        {
          return;
        }

        if (f.TurnsRemaining() == turn)
        {
          // process a fleet
          ships[f.Owner()] += f.NumShips();
          fleet++;

          // if it's the last fleet this turn, process the battle
          if (fleet == m_incomingFleets.Count || m_incomingFleets[fleet].TurnsRemaining() > f.TurnsRemaining())
          {
            if (ships[0] > ships[1])
            {
              // 0 > 1
              if (ships[0] > ships[2])
              {
                // 0 > 1 && 0 > 2
                ships[0] -= Math.Max(ships[1], ships[2]);
                ships[1] = 0;
                ships[2] = 0;
                m_eventualOwner = 0;
              }
              else
              {
                // 0 > 1 && 2 >= 0
                ships[2] -= ships[0];
                ships[0] = 0;
                ships[1] = 0;
                if (ships[2] > 0)
                {
                  m_eventualOwner = 2;
                }
                // else owner doesn't change
              }
            }
            else
            {
              // 1 >= 0
              if (ships[0] > ships[2])
              {
                // 1 >= 0 && 0 > 2
                ships[1] -= ships[0];
                ships[0] = 0;
                ships[2] = 0;
                if (ships[1] > 0)
                {
                  m_eventualOwner = 1;
                }
                // else owner doesn't change
              }
              else
              {
                // 1 >= 0 && 2 >= 0
                if (ships[1] > ships[2])
                {
                  ships[1] -= ships[2];
                  ships[0] = 0;
                  ships[2] = 0;
                  m_eventualOwner = 1;
                }
                else
                {
                  ships[2] -= ships[1];
                  ships[0] = 0;
                  ships[1] = 0;
                  if (ships[2] > 0)
                  {
                    m_eventualOwner = 2;
                  }
                  // else owner doesn't change
                }
              }
            }
          }
        }
        else
        {
          // process a turn - only players get growth
          if (m_eventualOwner != 0)
          {
            ships[m_eventualOwner] += growthRate;
          }
          turn++;
        }
      }

      m_eventualNumShips = ships[m_eventualOwner];
    }

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
//    public int m_numEnemyShips;
//    public int m_numNeutralShips;
//    public int m_numMyShips;
//    public int m_numFinalShips;
    public int m_eventualOwner;
    public int m_eventualNumShips;
    public int m_numShipsCanSafelySend;
	public List<Fleet> m_incomingFleets;
    private double x, y;

    private Planet (Planet _p) {
	planetID = _p.planetID;
	m_owner = _p.m_owner;
	numShips = _p.numShips;
	growthRate = _p.growthRate;
	x = _p.x;
	y = _p.y;
    }
}
