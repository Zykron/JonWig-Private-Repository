Authors: Ben Huenemann and Jonathan Ryan Wigderson
PS8
Version: 1.0

This client creates a form with a controller that manages the connection that it has
with the server. This controller starts by sending the player name to the specified
server. Then the server sends the player ID and the world size back. After this it
starts an event loop where it receives information, processes the information,
invalidates the form so it can be redrawn, and then sends the commands to the server.
It analyzes the information sent by deserializing the JSON text. Then it stores the
information so it can be drawn later.

Whenever the form is invalidated, it redraws each element at specified location with
the given information. For the tanks and projectiles it draws it differently depending
on the color stored by the controller. The turret is drawn on top of the tank at the
same location pointing the direction of the mouse. It also spawns an explosion when
the health is equal to 0 that consists of particles radiating out from the center at
random angles For the beam it spawns particles surrounding the beam. These particles
move in random directions for a certain amount of time to create a cool animation. For
the walls it splits them into square segments and draws them individually. It also has
elements for the health and player names that are drawn near the tank.

There are also classes inside the model that represent each of these aspects and store
the serializable properties of those elements. There's also a static class that contains
the constants for how to draw each element.


PS9
Version: 1.5

The client now also creates a server and highschore html webpage the the Tanks Wars game. 
The first thing the server does when starting the server is read the setting xml file 
withing the code to obtain information about the game. Such as MSPerFrame, Universe Size, 
Respawn Rate, Frames Per shot, Tank Speed, Max Powerups, Max Powerup Delay, and the location
of walls. The client starts a server at the given port number 11000, which is hard coded.
As well as a http connection at port 80. This then starts the main event loop for the server
where it updates the server every MSPerFrame, which is obtained from the xml setting file read
earlier. 

Inside this main event loop, which is on a seperate thread, it updates all information in
the game every frame. Which includes the tanks, turret direction, powerups, projectiles, 
and beams. Which is then followed by the server sending the updated information to all 
clients connected to the server.

To save a game, you click enter in the server, which will then send requested information
to a database controller. This database controller is responsible for handling all 
infromation to created the highschore tables on the http web page. You can get a highscore 
table for all played games by typing in <hostname>/games into a web browser, and a highscore 
table for all games of a specific player by typing in <hostname>/games?player=<playername> 
into a web browser.