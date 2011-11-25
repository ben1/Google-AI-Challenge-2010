using System;
using System.Collections.Generic;
using System.Text;

public class FuturePlanet
{
  public List<Fleet> m_incomingFleets;
  public int m_eventualOwner;
  public int m_eventualNumShips;
  public int m_futureMinNumShips;

  // incoming fleets
  // each players ships at each turn, and the owner

  // defeinite up to the number of turns to the closest player 1/2 planet with ships
  // after that it depends on whether ships are sent from that planet by me or enemy to come here.

  // we need to test multiple possible futures:
  // - what would happen if left alone
  // - what would happen for each of the combination of fleets I could send
  // - what could the enemy do to counter my sending (so make sure I send enough, or don't send anything)


  public void CalcEventuality(int a_owner, int a_growthRate, int a_numShips)
  {
    m_eventualOwner = a_owner;

    if (m_incomingFleets.Count == 0)
    {
      m_eventualNumShips = a_numShips;
      m_futureMinNumShips = a_numShips;
      if (a_owner != 0) // only players get growth
      {
        m_eventualNumShips += a_growthRate * 5; // estimate how many ships will be there in an average number of turns
      }
      return;
    }

    int[] ships = new int[3] { 0, 0, 0 };
    ships[a_owner] = a_numShips;
    m_futureMinNumShips = a_numShips;

    int turn = 0;
    int fleet = 0;
    while (fleet < m_incomingFleets.Count)
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
          ships[m_eventualOwner] += a_growthRate;
        }
        turn++;
      }

      // update min number of our ships
      m_futureMinNumShips = Math.Min(m_futureMinNumShips, ships[a_owner]);
    }

    m_eventualNumShips = ships[m_eventualOwner];
  }

}

