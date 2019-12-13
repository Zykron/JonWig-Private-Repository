using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NetworkUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TankWars;

namespace Server
{
    /// <summary>
    /// This class starts up a server for tanks to connect to. It also saves to a database when games finish
    /// so that the data can be accessed online while the server is active.
    /// </summary>
    public class Server
    {
        //Each server has a world and a list of sockets states that are connected
        private static World TheWorld = new World();
        private static HashSet<SocketState> SocketConnections = new HashSet<SocketState>();

        //Settings imported from the settings xml file
        static private int UniverseSize;
        static private int MSPerFrame;
        static private int FramesPerShot;
        static private int RespawnRate;
        static private int MaxPowerups;
        static private int MaxPowerupDelay;
        static private float TankSpeed;

        //The rate that powerups spawn at
        static private int PowerUpRespawnRate = 0;



        public static void Main(string[] args)
        {
            //Read settings from file
            ReadSettingFile(@"..\\..\\..\\Resources\settings.xml");

            //Start the server and web server
            Networking.StartServer(ReceivePlayerName, 11000);
            Networking.StartServer(HandleHttpConnection, 80);

            Console.WriteLine("Server is running. Accepting clients.");

            //Start the main loop
            Thread MainThread = new Thread(FrameLoop);
            MainThread.Start();

            //Saves the game to the database and waits for an input
            Console.ReadLine();
            MainThread.Abort();
            DatabaseController.SaveGameToDatabase(TheWorld);
            Console.WriteLine("Saved game to database");
            Console.ReadLine();
        }


        /// <summary>
        /// This is the main loop where the thread updates the data and sends the data to the clients
        /// every frame. It also has a timer that makes sure each frame is a certain length
        /// </summary>
        private static void FrameLoop()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Random r = new Random();
            PowerUpRespawnRate = r.Next(1, MaxPowerupDelay);

            while (true)
            {
                while (watch.ElapsedMilliseconds < MSPerFrame)
                {
                    //Do Nothing
                }
                watch.Restart();

                UpdateData();

                SendDataToSockets();
            }
        }


        /// <summary>
        /// Helper method that calls other methods to update each game object
        /// </summary>
        private static void UpdateData()
        {
            lock(TheWorld)
            {
                UpdateTanks();
                UpdateProjectiles();
                UpdateBeams();
                UpdatePowerUps();
            }
        }


        /// <summary>
        /// This method updates all of the tanks by going through the list of commands that the clients are inputting
        /// and applying them to the world.
        /// </summary>
        public static void UpdateTanks()
        {
            foreach (Tank t in TheWorld.Tanks.Values.ToList())
            {
                /*If a tank has been dead for a frame, remove it from the game. This only happens to disconnected tanks
                 * since otherwise*/
                if (t.Died)
                {
                    TheWorld.TankRemove(t.ID);
                    continue;
                }



                //MOVEMENT

                switch (TheWorld.PlayerCommands[t.ID].direction)
                {
                    //Updates the orientation and velocity depending on the movement keys
                    case "left":
                        TheWorld.TankSetOrientation(t.ID, new Vector2D(-1, 0));
                        TheWorld.TankSetVelocity(t.ID, t.Orientation * TankSpeed);
                        break;
                    case "right":
                        TheWorld.TankSetOrientation(t.ID, new Vector2D(1, 0));
                        TheWorld.TankSetVelocity(t.ID, t.Orientation * TankSpeed);
                        break;
                    case "up":
                        TheWorld.TankSetOrientation(t.ID, new Vector2D(0, -1));
                        TheWorld.TankSetVelocity(t.ID, t.Orientation * TankSpeed);
                        break;
                    case "down":
                        TheWorld.TankSetOrientation(t.ID, new Vector2D(0, 1));
                        TheWorld.TankSetVelocity(t.ID, t.Orientation * TankSpeed);
                        break;
                    case "none":
                        TheWorld.TankSetVelocity(t.ID, new Vector2D(0, 0));
                        break;
                }

                //Updates the location but undos that update if it collides with a wall
                TheWorld.TankSetLocation(t.ID, t.Location + t.Velocity);

                foreach (Wall w in TheWorld.Walls.Values)
                {
                    if (CollisionTankWall(t, w))
                        TheWorld.TankSetLocation(t.ID, t.Location - t.Velocity);
                }

                //Changes the tank location to the opposite side if it goes off screen
                WrapAround(t);



                //AIMING

                TheWorld.TankSetAiming(t.ID, TheWorld.PlayerCommands[t.ID].aiming);



                //FIRING

                //Calculate the cool down frames
                if (t.CooldownFrames < FramesPerShot)
                    TheWorld.TankIncrementCooldownFrames(t.ID);

                switch (TheWorld.PlayerCommands[t.ID].fire)
                {
                    case "main":
                        //If enough cool down frames have passed
                        if(t.CooldownFrames == FramesPerShot)
                        {
                            //Create a new projectile, add it to the dictionary, record the shot in the tank, and reset the cooldown frames
                            Projectile p = new Projectile(t.Location, t.Aiming, t.ID);
                            TheWorld.UpdateProjectile(p);
                            TheWorld.TankIncrementShotsFired(t.ID);

                            TheWorld.TankSetCooldownFrames(t.ID, 0);
                        }
                        break;
                    case "alt":
                        //If the tank has any beem shots
                        if(t.PowerUps > 0)
                        {
                            //Create a new beam, add it to the dictionary, and then record that the tank has shot
                            Beam b = new Beam(t.Location, t.Aiming, t.ID);
                            TheWorld.UpdateBeam(b);

                            TheWorld.TankIncrementShotsFired(t.ID);
                        }
                        break;
                    case "none":
                        break;
                }
            }
            //For each dead tank, increment the respawn count and spawn it if that count is high enough
            foreach (Tank t in TheWorld.DeadTanks.Values.ToList())
            {
                TheWorld.TankIncrementRespawnFrames(t.ID);

                if (t.RespawnFrames == RespawnRate)
                {
                    TheWorld.TankRestoreHealth(t.ID);
                    TheWorld.TankSetRespawnFrames(t.ID, 0);
                    SpawnTank(t);
                }
            }
        }


        /// <summary>
        /// This method updates all of the projectiles by going through all of them and looking for collisions with walls and tanks
        /// </summary>
        public static void UpdateProjectiles()
        {
            foreach (Projectile p in TheWorld.Projectiles.Values.ToList())
            {
                if (p.Died)
                {
                    TheWorld.ProjectileRemove(p.ID);
                    continue;
                }

                //Updates the location of the projectile
                TheWorld.ProjectileSetLocation(p.ID, p.Location + p.Velocity);

                //Kills any projectiles that go outside of the map
                if (Math.Abs(p.Location.GetX()) > UniverseSize / 2 || Math.Abs(p.Location.GetY()) > UniverseSize / 2)
                    TheWorld.ProjectileSetDied(p.ID);

                //Looks for tank collisions and applies the damage if that collision isn't with the owner of the projectile
                foreach (Tank t in TheWorld.Tanks.Values)
                {
                    if(CollisionProjectileTank(p, t) && p.OwnerID != t.ID)
                    {
                        if (TheWorld.Tanks.ContainsKey(p.OwnerID))
                            TheWorld.TankIncrementShotsHit(p.OwnerID);
                        else if(TheWorld.DeadTanks.ContainsKey(p.OwnerID))
                            TheWorld.TankDeadIncrementShotsHit(p.OwnerID);

                        TheWorld.ProjectileSetDied(p.ID);
                        TheWorld.TankProjectileDamage(t.ID, p.ID);
                    }
                }
                //Looks for wall collisions and kills the projectile if there is one
                foreach (Wall w in TheWorld.Walls.Values)
                {
                    if (CollisionProjectileWall(p, w))
                        TheWorld.ProjectileSetDied(p.ID);
                }
            }
        }


        /// <summary>
        /// This method updates all of the beams by checking for collisions with each tank
        /// </summary>
        public static void UpdateBeams()
        {
            foreach(Beam b in TheWorld.Beams.Values.ToList())
            {
                //Extra booleon to make sure that it only increments the owner shot record once
                bool shotHit = false;

                foreach(Tank t in TheWorld.Tanks.Values)
                {
                    if(!shotHit)
                    {
                        TheWorld.TankIncrementShotsHit(b.OwnerID);
                        shotHit = true;
                    }

                    if (CollisionBeamTank(b, t))
                        TheWorld.TankBeamDamage(t.ID, b.ID);
                }


                //If the beam hasn't already been around for one frame it is removved from the game.
                if (!b.Spawned)
                    TheWorld.BeamSetSpawnedTrue(b.ID);
                else
                {
                    TheWorld.BeamRemove(b.ID);
                    TheWorld.TankDecrementPowerUps(b.OwnerID);
                }
            }
        }


        /// <summary>
        /// This method updates all of the powerups by checking to see if they collide with tanks.
        /// </summary>
        public static void UpdatePowerUps()
        {
            //Spawn the powerups once a certain amount of frames have passed
            if (TheWorld.powerUpFrames >= PowerUpRespawnRate)
            {
                PowerUp p = new PowerUp();
                TheWorld.UpdatePowerUp(p);
                SpawnPowerUp(p);
                TheWorld.powerUpFrames = 0;
            }

            //Only incremeent the frames if there are less than two powerups
            if (TheWorld.PowerUps.Count < MaxPowerups)
                TheWorld.powerUpFrames++;

            //Go through each powerup and remove it if it collides with a tank
            foreach(PowerUp p in TheWorld.PowerUps.Values.ToList())
            {
                if (p.Died)
                {
                    TheWorld.PowerUpRemove(p.ID);
                    continue;
                }

                foreach (Tank t in TheWorld.Tanks.Values)
                {
                    if(CollisionPowerUpTank(p, t))
                    {
                        Random r = new Random();

                        TheWorld.PowerUpSetDied(p.ID);
                        TheWorld.TankIncrementPowerUps(t.ID);

                        //Assign a random frame to spawn the next powerup
                        PowerUpRespawnRate = r.Next(1, MaxPowerupDelay);
                    }
                }
            }
        }


        /// <summary>
        /// Method for sending the data to the client sockets. It goes through the list of tanks, powerups, projectiles, and beams
        /// and appends them to a stringbuilder to send. It also handles sockets that have disconnected.
        /// </summary>
        private static void SendDataToSockets()
        {
            lock (TheWorld)
            {
                foreach (SocketState s in SocketConnections.ToList())
                {
                    if (!s.TheSocket.Connected)
                    {
                        int tankID = (int)s.ID;

                        //Makes sure that the dictionary contains the right key. If not, the tank must have died and rage quit
                        if (TheWorld.Tanks.ContainsKey(tankID))
                        {
                            Console.WriteLine("Player(" + tankID + ") " + "\"" + TheWorld.Tanks[(int)s.ID].Name + "\" disconnected");

                            TheWorld.TankDisconnect(tankID);
                            TheWorld.TankKill(tankID);
                        }

                        if (TheWorld.DeadTanks.ContainsKey(tankID))
                        {
                            Console.WriteLine("Player(" + tankID + ") " + "\"" + TheWorld.DeadTanks[(int)s.ID].Name + "\" disconnected");
                            TheWorld.TankDeadRemove(tankID);
                        }

                        SocketConnections.Remove(s);
                    }
                }

                foreach (SocketState s in SocketConnections.ToList())
                {
                    StringBuilder frameMessage = new StringBuilder();


                    lock (TheWorld)
                    {
                        foreach (Tank t in TheWorld.Tanks.Values)
                            frameMessage.Append(JsonConvert.SerializeObject(t) + "\n");

                        foreach (PowerUp p in TheWorld.PowerUps.Values)
                            frameMessage.Append(JsonConvert.SerializeObject(p) + "\n");

                        foreach (Projectile p in TheWorld.Projectiles.Values)
                            frameMessage.Append(JsonConvert.SerializeObject(p) + "\n");

                        foreach (Beam b in TheWorld.Beams.Values)
                            frameMessage.Append(JsonConvert.SerializeObject(b) + "\n");
                    }

                    if (!Networking.Send(s.TheSocket, frameMessage.ToString()))
                        Console.WriteLine("Error occured while sending data");
                }
            }
        }


        /// <summary>
        /// Begining of the handshake between the server and a client. This part waits for the client to send player name
        /// </summary>
        /// <param name="ss">Socket state for the connection</param>
        private static void ReceivePlayerName(SocketState ss)
        {
            if (ss.ErrorOccured == true)
            {
                Console.WriteLine("Error occured while accepting: \"" + ss.ErrorMessage + "\"");
                return;
            }
            Console.WriteLine("Accepted new client");
            ss.OnNetworkAction = SendStartupInfo;
            Networking.GetData(ss);
        }


        /// <summary>
        /// This part sets up a tank with the player name and sends the startup info to the client including the world size, player ID,
        /// and walls.
        /// </summary>
        /// <param name="ss">Socket state for the connection</param>
        private static void SendStartupInfo(SocketState ss)
        {
            if (ss.ErrorOccured == true)
            {
                Console.WriteLine("Error occured while accepting: \"" + ss.ErrorMessage + "\"");
                return;
            }

            //Gets the name and ID from the socket and removes the name from the socket
            string tankName = ss.GetData();
            int tankID = (int)ss.ID;
            ss.RemoveData(0, tankName.Length);


            lock(TheWorld)
            {
                /*This sets up the tank, sets the cooldown frames so it can fire, adds a filler command to the dictionary, and
                 * spawns the tank at a random location.*/
                Tank t = new Tank(tankName.Substring(0, tankName.Length - 1), tankID);
                TheWorld.UpdateTank(t);
                TheWorld.TankSetCooldownFrames(t.ID, FramesPerShot);
                TheWorld.UpdateCommand(tankID, new ControlCommands());
                SpawnTank(t);

                Console.WriteLine("Player(" + tankID + ") " + "\"" + t.Name + "\" joined");

            }

            //Changes the delegate
            ss.OnNetworkAction = ReceiveCommandData;


            //Sends the tank ID and the world size
            string message = tankID + "\n" + UniverseSize.ToString() + "\n";
            if(!Networking.Send(ss.TheSocket, message))
                Console.WriteLine("Error occured while sending data");

            //Sends the walls to the client
            lock (TheWorld)
            {
                StringBuilder wallMessage = new StringBuilder();
                foreach (Wall w in TheWorld.Walls.Values)
                    wallMessage.Append(JsonConvert.SerializeObject(w) + "\n");
                if(!Networking.Send(ss.TheSocket, wallMessage.ToString()))
                    Console.WriteLine("Error occured while sending data");
            }


            //Adds the socket state to the list of connections
            SocketConnections.Add(ss);


            Networking.GetData(ss);
        }


        /// <summary>
        /// This method Processes the commands received from the client through the socket state
        /// </summary>
        /// <param name="ss">Socket state for the connection</param>
        private static void ReceiveCommandData(SocketState ss)
        {
            if (ss.ErrorOccured == true)
                return;

            ProcessData(ss);

            Networking.GetData(ss);
        }


        /// <summary>
        /// This method parses the commands received and calls a method to deserialize them.
        /// </summary>
        /// <param name="ss">Socket state to process the data from</param>
        private static void ProcessData(SocketState ss)
        {
            //Splits the string but keeps the '\n' characters
            string totalData = ss.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            lock (TheWorld)
            {
                string lastCommand = null;

                foreach (string p in parts)
                {
                    //This is to ignore empty strings
                    if (p.Length == 0)
                        continue;
                    //This is so it ignores the last string if it doesn't end in '\n'
                    if (p[p.Length - 1] != '\n')
                        break;

                    lastCommand = p;
                    ss.RemoveData(0, p.Length);
                }

                if (lastCommand != null)
                    //Calls a method to deserialize the data and then removes the data from the buffer
                    UpdateObject(lastCommand, (int)ss.ID);
            }
        }


        /// <summary>
        /// This takes in a JSON string and parses it to add to the dictionary of player commands
        /// </summary>
        /// <param name="serializedObject">JSON string of the serialized object</param>
        /// <param name="ID">ID of the socket state for the command</param>
        private static void UpdateObject(string serializedObject, int ID)
        {
            JObject obj = JObject.Parse(serializedObject);
            JToken token = obj["moving"];
            if (token != null)
            {
                ControlCommands com = JsonConvert.DeserializeObject<ControlCommands>(serializedObject);
                TheWorld.UpdateCommand(ID, com);
                return;
            }
        }


        /// <summary>
        /// This method reads the settings from the XML file
        /// </summary>
        /// <param name="fileName">File to read the data from</param>
        private static void ReadSettingFile(string fileName)
        {
            try
            {
                // Create an XmlReader inside this block, and automatically Dispose() it at the end.
                using (XmlReader reader = XmlReader.Create(fileName))
                {
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name)
                            {
                                case "UniverseSize":
                                    reader.Read();
                                    UniverseSize = Int32.Parse(reader.Value);
                                    break;

                                case "MSPerFrame":
                                    reader.Read();
                                    MSPerFrame = Int32.Parse(reader.Value);
                                    break;

                                case "FramesPerShot":
                                    reader.Read();
                                    FramesPerShot = Int32.Parse(reader.Value);
                                    break;

                                case "RespawnRate":
                                    reader.Read();
                                    RespawnRate = Int32.Parse(reader.Value);
                                    break;

                                case "TankSpeed":
                                    reader.Read();
                                    TankSpeed = float.Parse(reader.Value);
                                    break;

                                case "MaxPowerups":
                                    reader.Read();
                                    MaxPowerups = Int32.Parse(reader.Value);
                                    break;

                                case "MaxPowerupDelay":
                                    reader.Read();
                                    MaxPowerupDelay = Int32.Parse(reader.Value);
                                    break;

                                case "Wall":
                                    //Sets up the first end point of the wall
                                    reader.ReadToFollowing("x");
                                    reader.Read(); //gets x
                                    double p1X = Double.Parse(reader.Value);
                                    reader.ReadToFollowing("y");
                                    reader.Read(); //gets y
                                    double p1Y = Double.Parse(reader.Value);
                                    Vector2D p1V = new Vector2D(p1X, p1Y);

                                    //Sets up the second end point of the wall
                                    reader.ReadToFollowing("x");
                                    reader.Read(); //gets x
                                    double p2X = Double.Parse(reader.Value);
                                    reader.ReadToFollowing("y");
                                    reader.Read(); //gets y
                                    double p2Y = Double.Parse(reader.Value);
                                    Vector2D p2V = new Vector2D(p2X, p2Y);

                                    //Creates a wall with those endpoints and adds it to the dictionary
                                    Wall w = new Wall(p1V, p2V);
                                    TheWorld.UpdateWall(w);
                                    break;
                            }
                        }
                    }
                }
            }

            catch
            {
                throw new Exception("There was a problem opening the saved file");
            }
        }


        /// <summary>
        /// Callback for handling the web server connection. This waits for the server to send it's HTTP request
        /// </summary>
        /// <param name="ss">Socket state for the connection</param>
        public static void HandleHttpConnection(SocketState ss)
        {
            if (ss.ErrorOccured == true)
            {
                Console.WriteLine("Error occured while accepting: \"" + ss.ErrorMessage + "\"");
                return;
            }
            Console.WriteLine("Accepted new web client");
            ss.OnNetworkAction = ServeHttpRequest;
            Networking.GetData(ss);
        }


        /// <summary>
        /// This handles the HTTP request and makes the web page depending on the request
        /// </summary>
        /// <param name="ss">Socket state for the connection</param>
        public static void ServeHttpRequest(SocketState ss)
        {
            if (ss.ErrorOccured == true)
            {
                Console.WriteLine("Error occured while accepting: \"" + ss.ErrorMessage + "\"");
                return;
            }

            string request = ss.GetData();

            Console.WriteLine(request);

            //Player request
            if (request.Contains("GET /games?player="))
            {
                //Finds the player name with substring
                int start = request.IndexOf("=") + 1;
                int length = request.IndexOf(" HTTP/1.1") - start;
                string name = request.Substring(start, length);

                //Gets all of the players in the form of a dictionary
                Dictionary<uint, PlayerModel> playersDictionary = DatabaseController.GetAllPlayerGames(name);

                //Creates list of sessions that the player has been in by getting the game durations from the database
                List<SessionModel> SessionList = new List<SessionModel>();
                foreach (KeyValuePair<uint, PlayerModel> player in playersDictionary)
                    SessionList.Add(new SessionModel(player.Key, DatabaseController.GetGameDuration(player.Key), player.Value.Score, player.Value.Accuracy));

                //Sends the list so it can be formatted into a table
                Networking.SendAndClose(ss.TheSocket, WebViews.GetPlayerGames(name, SessionList));
            }
            //Games request
            else if(request.Contains("GET /games HTTP/1.1"))
            {
                //Creates a table with each of the games and all of their data
                Networking.SendAndClose(ss.TheSocket, WebViews.GetAllGames(DatabaseController.GetAllGames()));
            }
            //If there aren't any slashes it goes to the home page
            else if (request.Contains("GET / HTTP/1.1"))
            {
                Networking.SendAndClose(ss.TheSocket, WebViews.GetHomePage(0));
            }
            //Otherwise it throws a 404 error
            else
            {
                Networking.SendAndClose(ss.TheSocket, WebViews.Get404());
            }
        }


        /// <summary>
        /// This method continuously tries to set the tank location to different coordinates until it finds a place with no collisions
        /// </summary>
        /// <param name="t">Tank that is being spawned</param>
        public static void SpawnTank(Tank t)
        {
            Random random = new Random();

            do
            {
                int xLocation = random.Next(-UniverseSize / 2, UniverseSize / 2);
                int yLocation = random.Next(-UniverseSize / 2, UniverseSize / 2);

                TheWorld.TankSetLocation(t.ID, new Vector2D(xLocation, yLocation));
            }
            while (TankSpawnCollisions(t));
        }


        /// <summary>
        /// This makes sure that the spot the tank has spawned at doesn't collide with any walls, projectiles, and beams
        /// </summary>
        /// <param name="t">Tank that is being spawned</param>
        /// <returns>Whether or not it collides with anything</returns>
        public static bool TankSpawnCollisions(Tank t)
        {
            foreach(Wall w in TheWorld.Walls.Values)
            {
                if (CollisionTankWall(t, w))
                    return true;
            }
            foreach(Projectile p in TheWorld.Projectiles.Values)
            {
                if (CollisionProjectileTank(p, t))
                    return true;
            }
            foreach(Beam b in TheWorld.Beams.Values)
            {
                if (CollisionBeamTank(b, t))
                    return true;
            }

            return false;
        }


        /// <summary>
        /// Similar to the tank spawning but instead with the powerup
        /// </summary>
        /// <param name="p">Powerup that is being spawned</param>
        public static void SpawnPowerUp(PowerUp p)
        {
            Random random = new Random();

            do
            {
                int xLocation = random.Next(-UniverseSize / 2, UniverseSize / 2);
                int yLocation = random.Next(-UniverseSize / 2, UniverseSize / 2);

                TheWorld.PowerUpSetLocation(p.ID, new Vector2D(xLocation, yLocation));
            }
            while (PowerUpSpawnCollisions(p));
        }


        /// <summary>
        /// Similar to tank spawn collisions but this one makes sure that powerups don't spawn inside tanks or walls
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool PowerUpSpawnCollisions(PowerUp p)
        {
            foreach (Wall w in TheWorld.Walls.Values)
            {
                if (CollisionPowerUpWall(p, w))
                    return true;
            }
            foreach (Tank t in TheWorld.Tanks.Values)
            {
                if (CollisionPowerUpTank(p, t))
                    return true;
            }
            return false;
        }


        /// <summary>
        /// Method for seeing if a tank collides with a wall. This checks to see if the tank coordinate is within a specified hitbox
        /// around the wall that depends on the wall size and the tank size.
        /// </summary>
        /// <param name="t">Tank that is being tested</param>
        /// <param name="w">Wall that is being tested</param>
        /// <returns>Whether or not there is a collision</returns>
        public static bool CollisionTankWall(Tank t, Wall w)
        {
            //Finds the minimums and maximums
            double minX = Math.Min(w.EndPoint1.GetX(), w.EndPoint2.GetX());
            double minY = Math.Min(w.EndPoint1.GetY(), w.EndPoint2.GetY());
            double maxX = Math.Max(w.EndPoint1.GetX(), w.EndPoint2.GetX());
            double maxY = Math.Max(w.EndPoint1.GetY(), w.EndPoint2.GetY());

            //Checks to see if the x and the y values intersect
            bool xCollide = (t.Location.GetX() >= minX - Constants.WallSize / 2 - Constants.TankSize / 2 &&
                t.Location.GetX() <= maxX + Constants.WallSize / 2 + Constants.TankSize / 2);
            bool yCollide = (t.Location.GetY() >= minY - Constants.WallSize / 2 - Constants.TankSize / 2 &&
                t.Location.GetY() <= maxY + Constants.WallSize / 2 + Constants.TankSize / 2);

            //If both the x and y values intersect there must be a collision
            if (xCollide && yCollide)
                return true;

            return false;
        }


        /// <summary>
        /// Method for seeing if there is a collision betweeen a tank and a projectile. This just sees if the projectile location is
        /// inside of the tank
        /// </summary>
        /// <param name="p">Projectile that is being tested</param>
        /// <param name="t">Tank that is being tested</param>
        /// <returns>Whether or not there is a collision</returns>
        public static bool CollisionProjectileTank(Projectile p, Tank t)
        {
            return (p.Location - t.Location).Length() < Constants.TankSize / 2;
        }


        /// <summary>
        /// Method for seeing if there is a collision between a tank and a powerup. This just sees if the powerup location is
        /// inside of the tank
        /// </summary>
        /// <param name="p">Powerup that is being tested</param>
        /// <param name="t">Tank that is being tested</param>
        /// <returns>Whether or not there is a collision</returns>
        public static bool CollisionPowerUpTank(PowerUp p, Tank t)
        {
            return (p.Location - t.Location).Length() < Constants.TankSize / 2;
        }


        /// <summary>
        /// Method for seeing if there is a collision between a powerup and a wall. This checks to see if the powerup point is inside
        /// the specified wall range.
        /// </summary>
        /// <param name="p">Powerup that is being tested</param>
        /// <param name="w">Wall that is being tested</param>
        /// <returns>Whether or not there is a collision</returns>
        public static bool CollisionPowerUpWall(PowerUp p, Wall w)
        {
            double minX = Math.Min(w.EndPoint1.GetX(), w.EndPoint2.GetX());
            double minY = Math.Min(w.EndPoint1.GetY(), w.EndPoint2.GetY());
            double maxX = Math.Max(w.EndPoint1.GetX(), w.EndPoint2.GetX());
            double maxY = Math.Max(w.EndPoint1.GetY(), w.EndPoint2.GetY());
            if (p.Location.GetX() >= minX - Constants.WallSize / 2 && p.Location.GetX() <= maxX + Constants.WallSize / 2)
            {
                if (p.Location.GetY() >= minY - Constants.WallSize / 2 && p.Location.GetY() <= maxY + Constants.WallSize / 2)
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Method for seeing if there is a collision between a projectile and a wall. This checks to see if the projectile point is inside
        /// the specified wall range.
        /// </summary>
        /// <param name="p">Projectile that is being tested</param>
        /// <param name="w">Wall that is being tested</param>
        /// <returns>Whether or not there is a collision</returns>
        public static bool CollisionProjectileWall(Projectile p, Wall w)
        {
            double minX = Math.Min(w.EndPoint1.GetX(), w.EndPoint2.GetX());
            double minY = Math.Min(w.EndPoint1.GetY(), w.EndPoint2.GetY());
            double maxX = Math.Max(w.EndPoint1.GetX(), w.EndPoint2.GetX());
            double maxY = Math.Max(w.EndPoint1.GetY(), w.EndPoint2.GetY());
            if (p.Location.GetX() >= minX - Constants.WallSize / 2 && p.Location.GetX() <= maxX + Constants.WallSize / 2)
            {
                if (p.Location.GetY() >= minY - Constants.WallSize / 2 && p.Location.GetY() <= maxY + Constants.WallSize / 2)
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Method to see if there is a collision between a beam and a tank. This is just a convenience helper method for the
        /// intersects method below.
        /// </summary>
        /// <param name="b">Beam that is being tested</param>
        /// <param name="t">Tank that is being tested</param>
        /// <returns>Whether or not there is a collision</returns>
        public static bool CollisionBeamTank(Beam b, Tank t)
        {
            return Intersects(b.Origin, b.Orientation, t.Location, Constants.TankSize / 2);
        }


        /// <summary>
        /// Determines if a ray interescts a circle
        /// </summary>
        /// <param name="rayOrig">The origin of the ray</param>
        /// <param name="rayDir">The direction of the ray</param>
        /// <param name="center">The center of the circle</param>
        /// <param name="r">The radius of the circle</param>
        /// <returns>Whether or not the beam intersects</returns>
        public static bool Intersects(Vector2D rayOrig, Vector2D rayDir, Vector2D center, double r)
        {
            // ray-circle intersection test
            // P: hit point
            // ray: P = O + tV
            // circle: (P-C)dot(P-C)-r^2 = 0
            // substitute to solve for t gives a quadratic equation:
            // a = VdotV
            // b = 2(O-C)dotV
            // c = (O-C)dot(O-C)-r^2
            // if the discriminant is negative, miss (no solution for P)
            // otherwise, if both roots are positive, hit

            double a = rayDir.Dot(rayDir);
            double b = ((rayOrig - center) * 2.0).Dot(rayDir);
            double c = (rayOrig - center).Dot(rayOrig - center) - r * r;

            // discriminant
            double disc = b * b - 4.0 * a * c;

            if (disc < 0.0)
                return false;

            // find the signs of the roots
            // technically we should also divide by 2a
            // but all we care about is the sign, not the magnitude
            double root1 = -b + Math.Sqrt(disc);
            double root2 = -b - Math.Sqrt(disc);

            return (root1 > 0.0 && root2 > 0.0);
        }


        /// <summary>
        /// Method for teleporting the tank to the opposite side of the world if it goes outside of the world border
        /// </summary>
        /// <param name="t">Tank that is moving</param>
        public static void WrapAround(Tank t)
        {
            if(Math.Abs(t.Location.GetX()) + Constants.TankSize/2  > UniverseSize/2)
                //Keeps the same y value but flips the x value depending on the sign
                TheWorld.TankSetLocation(t.ID, new Vector2D(Math.Sign(t.Location.GetX()) * (-UniverseSize / 2 + Constants.TankSize/2), t.Location.GetY()));

            else if(Math.Abs(t.Location.GetY()) + Constants.TankSize/2 > UniverseSize/2)
                //Keeps the same x value but flips the y value depending on the sign
                TheWorld.TankSetLocation(t.ID, new Vector2D(t.Location.GetX(), Math.Sign(t.Location.GetY()) * (-UniverseSize / 2 + Constants.TankSize / 2)));
        }
    }
}
