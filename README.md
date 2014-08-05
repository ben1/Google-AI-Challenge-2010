Google AI Challenge 2010
==========
The [challenge](http://planetwars.aichallenge.org/) is basically a strategic conquest of the galaxy between 2 forces on a randomly generated map. The AI can see exactly how many ships are on each planet and are in flight. Each turn, the AI chooses how many ships to send from each of their planets to other planets.

My entry for the challenge was a straight-forward heuristic approach with some brute-force optimization of parameters. It came 363 out of 4600 which I think is decent given the short amount of time I worked on it. 

##Disclaimer

This code was not intended to live longer than the duration of the challenge, or be modified by others, and is therefore not a good example of maintainable code. But it does demonstrate some interesting techniques.

##Core Algorithm

* Calculate a score for each planet
* Sort by score
* Send ships to the most valuable planets without weakening defences too much.

###Scoring

Planet score was based on a few simple factors:

* production rate
* proximity to allied planets
* proximity to enemy planets
* cost to conquer (if a neutral planet)
* how much we have been trying to conquer the planet in the past (this state provides some *hysteresis* in the decision making)

###Future State

A key technique used was take the current number of ships on each planet, and the current fleets in motion, and then calculate the owner and number of ships at the planet for each future turn as well as recording the final eventual owner of the planet.

This future simulation was simple and doesn't try to anticipate new enemy commands, but give an accurate short-term picture of who will own the planet on each turn in the future and how many ships will be there. This is used to calculate how many ships to send to reinforce or conquer a planet in order to maintain control, without sending too many ships.

###Sending Ships

Start with the highest scoring planet and work down the list. For each target planet:

* calculate the future state of this planet N turns ahead, given the current state and fleets in motion.
* find our closest planet to it and consider sending some ships 
  * only send as many ships that would not leave our planet vulnerable to incoming enemy fleets or nearby enemy planets, but include incoming reinforcements in the defences.
  * if we haven't sent enough ships to the target planet to be the final owner, loop to our next closest planet to the target planet
* If we can send enough ships to be the eventual owner of the target planet, then commit to the order, otherwise cancel all attack orders directed at this planet.

Finally:

* Send any reinforcements we can afford from our own low scoring planets to our high scoring planets.

There are a number of special-case rules that were added to this primary algorithm:

* If an enemy is already attacking a neutral planet, delay sending ships until they would arrive one turn after their battle ends.
* Don't send ships from my high-scoring planets that are threatened by enemy forces
* Don't send ships from planets that will be lost to the enemy.

##Debugging

The bots were played against each other by running the Java planetwars game which would in turn call the executables for the AIs. This was terrible for debugging because it was difficult to attach a debugger to the spawned process before it completed or was shut down by the game for taking longer than 1 second to perform a turn. To debug easily, I created a command line proxy AI which piped gamestate and commands to and from my already-running bot in the debugger. 

##Testing

In order to test the AI again other versions of itself as well as some other opponents, I wrote a harness that would run AIs against each other on 100 random maps. I also added a TCP/IP option for testing against other bots that people ran via a server.

##Training

I parametrized the main 'magic numbers' the algorithm used, and made the test harness run multiple competitions, each with a different combination of parameters. I could quite quickly narrow in on the best combination of parameters. There were some local minima, but with a wade enough range of values, these were avoided as much as possible.

##Optimization

Because the algorithm is not calculating an exponentially large set of possible futures, optimization was not a large concern, but sometimes the AI would use up the 1 second of time allowed for a turn. This was because there was a large number of fleets moving around. To prevent automatic disqualification, the AI would monitor the time it has spent and go with the best moves it calculated within the time allowed.

Also, all of the distances and inverse distances between each pair of planets were pre-calculated in the first turn.

