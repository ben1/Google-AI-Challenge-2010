Google AI Challenge 2010
==========
The [challenge](http://planetwars.aichallenge.org/) is basically a strategic conquest of the galaxy between 2 forces on a randomly generated map. The AI can see exactly how many ships are on each planet and are in flight. Each turn, the AI chooses how many ships to send from each of their planets to other planets.

My entry for the challenge was a simple heuristic-based approach with some brute-force optimization of parameters that balance the heuristic. It came 363 out of 4600 which I think is decent given the short amount of time I worked on it. Looking at the code a few years later, I would do it again quite differently, but many of the ideas would be reused.

##Disclaimer

This code was not intended to live longer than the duration of the challenge, or be modified by others, and is therefore not a good example of maintainable code. But it does demonstrate some interesting techniques, so I thought it would be worth sharing.

##Core Algorithm

* Calculate the future state of the game N turns ahead, based on current knowledge
* Calculate a score for each planet
* Sort by score
* Send enough ships to the most valuable planets but stop before weakening our defences too much.

###Future State

A key technique used was take the current number of ships on each planet, and the current fleets in motion, and then calculate the owner and number of ships at the planet for each future turn as well as recording the final eventual owner of the planet.

This future simulation was simple and doesn't try to anticipate new enemy commands, but gives an accurate (at least short-term) picture of who will most likely keep control of the planet, and how many ships would need to arrive at each turn in the future to gain or maintain control of it. This way, we can calculate how many ships we would need to send and when to send them so that they arrive when they are needed.

###Scoring

Planet score was based on a few simple factors:

* production rate
* proximity to allied planets
* proximity to enemy planets
* cost to conquer (if a neutral planet)
* how many ships we have been sending to the planet recently (this state provides some small but useful *hysteresis* in choosing which planets to send ships to)

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

The bots are played against each other by running the official Java planetwars game which would in turn spawn the executables for the AIs. This was terrible for debugging because it was difficult to attach a debugger to the spawned process before it had completed (and if you stalled the AI process intentionally, the Java controller process would disqualify it for taking too long). To debug more easily, I created a command line proxy AI executable which piped gamestate and commands to and from my already-running bot in the debugger. 

##Testing

In order to test the AI again other versions of itself as well as some other opponents, I wrote a harness that would run AIs against each other on 100 random maps. I also added a TCP/IP option for testing against other bots that people ran via a server.

##Training

I parametrized the main 'magic numbers' the algorithm used, and made the test harness run many tournaments, to cover a range of values for each parameter (a combinatorial explosion). These were queued up and run in a number of parallel threads equal to the number of cores on the machine. I could quite quickly narrow in on the best combination of parameters. There were some local maxima, but with a wide enough range of values to start with, these were hopefully avoided as much as possible.

##Optimization

Because the algorithm is not calculating an exponentially large set of possible futures, optimization was not a large concern, but sometimes the AI would use up the 1 second of time allowed for a turn. This was because there was a large number of fleets moving around. To prevent automatic disqualification, the AI would monitor the time it has spent and go with the best moves it calculated within the time allowed.

Also, all of the distances and inverse distances between each pair of planets were pre-calculated in the first turn.

