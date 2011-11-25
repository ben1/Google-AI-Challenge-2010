// Contestants do not need to worry about anything in this file. This is just
// helper code that does the boring stuff for you, so you can focus on the
// interesting stuff. That being said, you're welcome to change anything in
// this file if you know what you're doing.

using System;
using System.IO;
using System.Collections.Generic;

public class PlanetWars 
{
  // Constructs a PlanetWars object instance, given a string containing a
  // description of a game state.
  public PlanetWars(string gameStatestring, int a_turn) 
  {
    m_turn = a_turn;
    m_planets = new List<Planet>();
    m_fleets = new List<Fleet>();
    ParseGameState(gameStatestring);
  }

  // Returns the number of planets. Planets are numbered starting with 0.
  public int NumPlanets() 
  {
    return m_planets.Count;
  }

  // Returns the planet with the given planet_id. There are NumPlanets()
  // planets. They are numbered starting at 0.
  public Planet GetPlanet(int planetID) 
  {
    return m_planets[planetID];
  }

    // Returns the number of fleets.
  public int NumFleets() 
  {
    return m_fleets.Count;
  }

  // Returns the fleet with the given fleet_id. Fleets are numbered starting
  // with 0. There are NumFleets() fleets. fleet_id's are not consistent from
  // one turn to the next.
  public Fleet GetFleet(int fleetID) 
  {
    return m_fleets[fleetID];
  }

  // Return a list of all the planets owned by the current player. By
  // convention, the current player is always player number 1.
  public List<Planet> MyPlanets() 
  {
    List<Planet> r = new List<Planet>();
    foreach (Planet p in m_planets) 
    {
      if (p.m_owner == 1) 
      {
        r.Add(p);
      }
    }
    return r;
  }

  // Return a list of all neutral planets.
  public List<Planet> NeutralPlanets() 
  {
    List<Planet> r = new List<Planet>();
    foreach (Planet p in m_planets) 
    {
      if (p.m_owner == 0) 
      {
        r.Add(p);
      }
    }
    return r;
  }

  // Return a list of all the planets owned by rival players. This excludes
  // planets owned by the current player, as well as neutral planets.
  public List<Planet> EnemyPlanets() 
  {
    List<Planet> r = new List<Planet>();
    foreach (Planet p in m_planets) 
    {
      if (p.m_owner >= 2) 
      {
        r.Add(p);
      }
    }
    return r;
  }

  // Return a list of all the fleets.
  public List<Fleet> Fleets() 
  {
    List<Fleet> r = new List<Fleet>();
    foreach (Fleet f in m_fleets) 
    {
      r.Add(f);
    }
    return r;
  }

  // Return a list of all the fleets owned by the current player.
  public List<Fleet> MyFleets() 
  {
    List<Fleet> r = new List<Fleet>();
    foreach (Fleet f in m_fleets) 
    {
      if (f.m_owner == 1) 
      {
        r.Add(f);
      }
    }
    return r;
  }

  // Return a list of all the fleets owned by enemy players.
  public List<Fleet> EnemyFleets() 
  {
    List<Fleet> r = new List<Fleet>();
    foreach (Fleet f in m_fleets) 
    {
      if (f.m_owner != 1) 
      {
        r.Add(f);
      }
    }
    return r;
  }

  // Sends an order to the game engine. An order is composed of a source
  // planet number, a destination planet number, and a number of ships. A
  // few things to keep in mind:
  //   * you can issue many orders per turn if you like.
  //   * the planets are numbered starting at zero, not one.
  //   * you must own the source planet. If you break this rule, the game
  //     engine kicks your bot out of the game instantly.
  //   * you can't move more ships than are currently on the source planet.
  //   * the ships will take a few turns to reach their destination. Travel
  //     is not instant. See the Distance() function for more info.
  public void IssueOrder(int sourcePlanet, int destinationPlanet, int numShips) 
  {
    // safety checks
    if (sourcePlanet == destinationPlanet)
    {
#if PW_DEBUG
      System.Diagnostics.Debug.Assert(false);
#endif
     return;
    }
    Planet from = GetPlanet(sourcePlanet);
    if (from.m_owner != 1)
    {
#if PW_DEBUG
      System.Diagnostics.Debug.Assert(false);
#endif
      return;
    }

    Console.WriteLine("" + sourcePlanet + " " + destinationPlanet + " " + numShips);

    // do we really need this flush?
    Console.Out.Flush();
  }

  // Sends the game engine a message to let it know that we're done sending
  // orders. This signifies the end of our turn.
  public static void FinishTurn() 
  {
    Console.WriteLine("go");
    Console.Out.Flush();
  }

  // Parses a game state from a string. On success, returns 1. On failure,
  // returns 0.
  private int ParseGameState(string s) 
  {
    m_planets.Clear();
    m_fleets.Clear();
    int planetID = 0;
    string[] lines = s.Split('\n');
    for (int i = 0; i < lines.Length; ++i) 
    {
      string line = lines[i];
      int commentBegin = line.IndexOf('#');
      if (commentBegin >= 0) 
      {
        line = line.Substring(0, commentBegin);
      }
      if (line.Trim().Length == 0) 
      {
        continue;
      }
      string[] tokens = line.Split(' ');
      if (tokens.Length == 0) 
      {
        continue;
      }
      if (tokens[0].Equals("P")) 
      {
        if (tokens.Length != 6) 
        {
          return 0;
        }
        double x = double.Parse(tokens[1]);
        double y = double.Parse(tokens[2]);
        int owner = int.Parse(tokens[3]);
        int numShips = int.Parse(tokens[4]);
        int growthRate = int.Parse(tokens[5]);
        Planet p = new Planet(planetID++, owner, numShips, growthRate, x, y);
        m_planets.Add(p);
      } 
      else if (tokens[0].Equals("F")) 
      {
        if (tokens.Length != 7) 
        {
          return 0;
        }
        int owner = int.Parse(tokens[1]);
        int numShips = int.Parse(tokens[2]);
        int source = int.Parse(tokens[3]);
        int destination = int.Parse(tokens[4]);
        int totalTripLength = int.Parse(tokens[5]);
        int turnsRemaining = int.Parse(tokens[6]);
        Fleet f = new Fleet(owner, numShips, source, destination, totalTripLength, turnsRemaining);
        m_fleets.Add(f);
      } 
      else 
      {
        return 0;
      }
    }
    return 1;
  }

  public List<Planet> m_planets;
  public List<Fleet> m_fleets;
  public int m_turn;
}
