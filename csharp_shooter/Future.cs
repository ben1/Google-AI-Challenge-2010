using System;
using System.Collections.Generic;
using System.Text;

public class Future
{

    public List<Fleet> m_incomingFleets;
    public int m_eventualOwner;
    public int[] m_minNumShips = new int[3];
    public int[] m_minNumShipsTurn = new int[3];
    public List<FuturePlanet> m_future;
    int m_originalOwner;
    int m_originalNumShips;
    int m_originalGrowthRate;

    public Future(Future a_copy)
    {
        // should we do a deep copy of the incoming fleets??
        m_incomingFleets = new List<Fleet>(a_copy.m_incomingFleets);
        m_eventualOwner = a_copy.m_eventualOwner;
        m_minNumShips = new int[3];
        m_minNumShips[0] = a_copy.m_minNumShips[0];
        m_minNumShips[1] = a_copy.m_minNumShips[1];
        m_minNumShips[2] = a_copy.m_minNumShips[2];
        m_minNumShipsTurn = new int[3];
        m_minNumShipsTurn[0] = a_copy.m_minNumShipsTurn[0];
        m_minNumShipsTurn[1] = a_copy.m_minNumShipsTurn[1];
        m_minNumShipsTurn[2] = a_copy.m_minNumShipsTurn[2];
        // should we do a deep copy of the future??
        m_future = new List<FuturePlanet>(a_copy.m_future);
        m_originalOwner = a_copy.m_originalOwner;
        m_originalNumShips = a_copy.m_originalNumShips;
        m_originalGrowthRate = a_copy.m_originalGrowthRate;
    }

    public Future(int a_owner, int a_numShips, int a_growthRate)
    {
        m_incomingFleets = new List<Fleet>();
        m_originalOwner = a_owner;
        m_originalNumShips = a_numShips;
        m_originalGrowthRate = a_growthRate;
    }

    public void CalcFocus()
    {
        m_minNumShips[0] = 0;
        m_minNumShips[1] = 0;
        m_minNumShips[2] = 0;
        m_minNumShips[m_originalOwner] = m_originalNumShips;
        m_minNumShipsTurn[0] = 0;
        m_minNumShipsTurn[1] = 0;
        m_minNumShipsTurn[2] = 0;

        m_eventualOwner = m_originalOwner;

        m_future = new List<FuturePlanet>();

        // add turn 0
        FuturePlanet fpTurn0 = new FuturePlanet();
        fpTurn0.m_owner = m_eventualOwner;
        fpTurn0.m_ships[0] = 0;
        fpTurn0.m_ships[1] = 0;
        fpTurn0.m_ships[2] = 0;
        fpTurn0.m_ships[fpTurn0.m_owner] = m_originalNumShips;
        m_future.Add(fpTurn0);

        int[] ships = new int[3] { 0, 0, 0 };
        ships[m_originalOwner] = m_originalNumShips;

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
                    fp.m_ships[fp.m_owner] = ships[fp.m_owner];
                    m_future.Add(fp);

                    // update min number of ships for each possible owner
                    if (ships[0] < m_minNumShips[0])
                    {
                        m_minNumShips[0] = ships[0];
                        m_minNumShipsTurn[0] = turn;
                    }
                    if (ships[1] < m_minNumShips[1])
                    {
                        m_minNumShips[1] = ships[1];
                        m_minNumShipsTurn[1] = turn;
                    }
                    if (ships[2] < m_minNumShips[2])
                    {
                        m_minNumShips[2] = ships[2];
                        m_minNumShipsTurn[2] = turn;
                    }
                }
            }
            else
            {
                // process a turn - only players get growth
                if (m_eventualOwner != 0)
                {
                    ships[m_eventualOwner] += m_originalGrowthRate;
                }
                turn++;

                // save a turn if there are no fleets going to arrive
                if (turn < f.m_turnsRemaining)
                {
                    FuturePlanet fp = new FuturePlanet();
                    fp.m_owner = m_eventualOwner;
                    fp.m_ships[0] = 0;
                    fp.m_ships[1] = 0;
                    fp.m_ships[2] = 0;
                    fp.m_ships[fp.m_owner] = ships[fp.m_owner];
                    m_future.Add(fp);
                }
            }
        }
    }

    public void CalcEventuality()
    {
        m_minNumShips[0] = 0;
        m_minNumShips[1] = 0;
        m_minNumShips[2] = 0;
        m_minNumShips[m_originalOwner] = m_originalNumShips;
        m_minNumShipsTurn[0] = 0;
        m_minNumShipsTurn[1] = 0;
        m_minNumShipsTurn[2] = 0;

        m_eventualOwner = m_originalOwner;

        m_future = new List<FuturePlanet>();

        // add turn 0
        FuturePlanet fpTurn0 = new FuturePlanet();
        fpTurn0.m_owner = m_eventualOwner;
        fpTurn0.m_ships[0] = 0;
        fpTurn0.m_ships[1] = 0;
        fpTurn0.m_ships[2] = 0;
        fpTurn0.m_ships[fpTurn0.m_owner] = m_originalNumShips;
        m_future.Add(fpTurn0);

        int[] ships = new int[3] { 0, 0, 0 };
        ships[m_originalOwner] = m_originalNumShips;

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
                    fp.m_ships[fp.m_owner] = ships[fp.m_owner];
                    m_future.Add(fp);

                    // update min number of ships for each possible owner
                    if (ships[0] < m_minNumShips[0])
                    {
                        m_minNumShips[0] = ships[0];
                        m_minNumShipsTurn[0] = turn;
                    }
                    if (ships[1] < m_minNumShips[1])
                    {
                        m_minNumShips[1] = ships[1];
                        m_minNumShipsTurn[1] = turn;
                    }
                    if (ships[2] < m_minNumShips[2])
                    {
                        m_minNumShips[2] = ships[2];
                        m_minNumShipsTurn[2] = turn;
                    }
                }
            }
            else
            {
                // process a turn - only players get growth
                if (m_eventualOwner != 0)
                {
                    ships[m_eventualOwner] += m_originalGrowthRate;
                }
                turn++;

                // save a turn if there are no fleets going to arrive
                if (turn < f.m_turnsRemaining)
                {
                    FuturePlanet fp = new FuturePlanet();
                    fp.m_owner = m_eventualOwner;
                    fp.m_ships[0] = 0;
                    fp.m_ships[1] = 0;
                    fp.m_ships[2] = 0;
                    fp.m_ships[fp.m_owner] = ships[fp.m_owner];
                    m_future.Add(fp);
                }
            }
        }
    }


    static public void ResolveBattle(int[] a_ships, ref int a_eventualOwner)
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
};


public class FuturePlanet
{
    public int[] m_ships = new int[3];
    public int m_owner;

    public int EnemyShips()
    {
        return Math.Max(m_ships[0], m_ships[2]);
    }
}

