using System;
using System.Collections.Generic;

public class Planet
{
    public int m_planetID;
    public int m_owner; // 0 is neutral, 1 is me, 2 is enemy
    public int m_numShips;
    public int m_growthRate;
    public double m_x, m_y;

    public double m_score;
    public int m_numShipsCanSafelySend;

    public Future m_originalFuture;
    public Future m_testFuture;


    public Planet(int planetID, int owner, int numShips, int growthRate, double x, double y)
    {
        this.m_planetID = planetID;
        this.m_owner = owner;
        this.m_numShips = numShips;
        this.m_growthRate = growthRate;
        this.m_x = x;
        this.m_y = y;

        m_originalFuture = new Future(m_owner, m_numShips, m_growthRate);
    }

    public void Owner(int newOwner) { m_owner = newOwner; }
    public void NumShips(int newNumShips) { m_numShips = newNumShips; }
    public void AddShips(int amount) { m_numShips += amount; }
    public void RemoveShips(int amount) { m_numShips -= amount; }

    public static int CompareScore(Planet a_1, Planet a_2)
    {
        return Math.Sign(a_1.m_score - a_2.m_score);
    }
}
