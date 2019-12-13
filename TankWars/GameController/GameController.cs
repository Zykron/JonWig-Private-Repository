//Authors: Ben Huenemann and Jonathan Wigderson

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows.Forms;
using NetworkUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TankWars
{

    public class GameController
    {
        //Keeps track of the world that the game takes place in
        public World TheWorld { get; private set; } = new World();

        //Set of commands to send the controller
        public ControlCommands commands { get; private set; } = new ControlCommands();

        //Events for errors and frames
        public delegate void ErrorHandler(string errorMessage = "");
        public event ErrorHandler ErrorEvent;
        public delegate void OnActionHandler();
        public event OnActionHandler OnFrameEvent;

        //Variable to keep track of if the walls have been drawn
        public bool wallsDone = false;

        //Variables for keeping track of the colors of the tanks
        private Dictionary<int, int> TankColorRecord = new Dictionary<int, int>();
        private int SeenPlayers = 0;

        //Variables to keep track of the name and ID of the tank being controlled
        private string tankName;
        private int tankID;

        //Variables to keep track of which keys are being pressed
        private bool upKey = false;
        private bool downKey = false;
        private bool leftKey = false;
        private bool rightKey = false;


        /// <summary>
        /// Convenience method for getting the tank that's being controlled
        /// </summary>
        /// <returns></returns>
        public Tank GetPlayerTank()
        {
            lock(TheWorld)
            {
                if (TheWorld.Tanks.ContainsKey(tankID))
                    return TheWorld.Tanks[tankID];
                else
                    return null;
            }
        }


        /// <summary>
        /// Method for handling the movement when a key is pressed down
        /// </summary>
        /// <param name="key"></param>
        public void ProcessKeyDown(Keys key)
        {
            //Contains inner logic for key priorities
            CalculateMovement();

            switch (key)
            {
                case Keys.W:
                    upKey = true;
                    commands.direction = "up";
                    break;
                case Keys.S:
                    downKey = true;
                    commands.direction = "down";
                    break;
                case Keys.A:
                    leftKey = true;
                    commands.direction = "left";
                    break;
                case Keys.D:
                    rightKey = true;
                    commands.direction = "right";
                    break;
            }
        }


        /// <summary>
        /// Method for clearing the movement when a key is released
        /// </summary>
        /// <param name="key"></param>
        public void ProcessKeyUp(Keys key)
        {
            switch (key)
            {
                case Keys.W:
                    upKey = false;
                    commands.direction = "none";
                    break;
                case Keys.S:
                    downKey = false;
                    commands.direction = "none";
                    break;
                case Keys.A:
                    leftKey = false;
                    commands.direction = "none";
                    break;
                case Keys.D:
                    rightKey = false;
                    commands.direction = "none";
                    break;
            }

            //Contains inner logic for key priorities
            CalculateMovement();
        }


        /// <summary>
        /// If the up and down key aren't equal to each other, it sets the command to whatever
        /// key is being pressed. It also does the same thing with the left and right key.
        /// </summary>
        private void CalculateMovement()
        {
            if (upKey != downKey)
                commands.direction = (upKey) ? "up" : "down";
            if (leftKey != rightKey)
                commands.direction = (leftKey) ? "left" : "right";
        }


        /// <summary>
        /// Method for handling tank firing when the mouse buttons are pressed
        /// </summary>
        /// <param name="button"></param>
        public void ProcessMouseDown(MouseButtons button)
        {
            if (button.Equals(MouseButtons.Left))
                commands.fire = "main";
            else if (button.Equals(MouseButtons.Right))
                commands.fire = "alt";
        }


        /// <summary>
        /// Method for clearing the firing when the mouse buttons are released
        /// </summary>
        public void ProcessMouseUp()
        {
            commands.fire = "none";
        }


        /// <summary>
        /// Method for calculating the tank turret direction based on the mouse location in order to send
        /// to the server.
        /// </summary>
        /// <param name="x">X location of the mouse</param>
        /// <param name="y">Y location of the mouse</param>
        public void ProcessMouseMove(double x, double y)
        {
            Vector2D loc = new Vector2D(x - Constants.ViewSize / 2, y - Constants.ViewSize / 2);
            loc.Normalize();

            commands.aiming = new Vector2D(loc.GetX(), loc.GetY());
        }


        /// <summary>
        /// This method tries to start the connection with the server given a specified player name, server
        /// address and port.
        /// </summary>
        /// <param name="name">Player name</param>
        /// <param name="server">Server address</param>
        /// <param name="port">Port to connect to the server</param>
        public void TryConnect(string name, string server, int port)
        {
            //Keeps track of the name
            if (name.Length <= 16)
                tankName = name;
            //If the name is too long it triggers an event
            else
            {
                ErrorEvent("Name is longer than 16 characters");
                return;
            }

            Networking.ConnectToServer(SendName, server, port);
        }


        /// <summary>
        /// This is used as the method that is called once the connection is established
        /// </summary>
        /// <param name="ss">Socket state representing the connection</param>
        private void SendName(SocketState ss)
        {
            //If any error occured, it triggers the event and closes the socket
            if (ss.ErrorOccured == true)
            {
                ErrorEvent("Unable to connect to server");
                if (ss.TheSocket.Connected)
                    ss.TheSocket.Close();
                return;
            }

            //Tries to send the tank name but if that fails it has an error
            if (!Networking.Send(ss.TheSocket, tankName + "\n"))
            {
                ErrorEvent("Couldn't send player name since socket was closed");
                if (ss.TheSocket.Connected)
                    ss.TheSocket.Close();
                return;
            }

            //Changes the OnNetworkAction to the next method
            ss.OnNetworkAction = ReceiveStartingData;
            Networking.GetData(ss);
        }


        /// <summary>
        /// This method receives the startup data sent by the server (world size and player ID)
        /// </summary>
        /// <param name="ss">Socket state representing the connection</param>
        private void ReceiveStartingData(SocketState ss)
        {
            if (ss.ErrorOccured == true)
            {
                ErrorEvent("Unable to receive tank ID and world size");
                if (ss.TheSocket.Connected)
                    ss.TheSocket.Close();
                return;
            }

            //Splits the data and stores it in a string array
            string[] startingInfo = Regex.Split(ss.GetData(), @"\n");

            //Parses and stores the ID and world size
            tankID = Int32.Parse(startingInfo[0]);
            TheWorld.worldSize = Int32.Parse(startingInfo[1]);

            //Removes the ID and world size from the socket string builder and processes the other data received
            ss.RemoveData(0, tankID.ToString().Length + TheWorld.worldSize.ToString().Length + 2);
            ProcessData(ss);

            //Changes the OnNetworkAction to the next method that will be called every frame
            ss.OnNetworkAction = ReceiveFrameData;
            Networking.GetData(ss);
        }


        /// <summary>
        /// This method receives frame data, processes it, calls an event that invalidates the form, and sends
        /// the commands to the server. At the end it gets more data and keeps the same OnNetworkAction to continue
        /// the event loop.
        /// </summary>
        /// <param name="ss">Socket state representing the connection</param>
        private void ReceiveFrameData(SocketState ss)
        {
            if (ss.ErrorOccured == true)
            {
                ErrorEvent("Error occured while receiving data from the server");
                if (ss.TheSocket.Connected)
                    ss.TheSocket.Close();
                return;
            }

            ProcessData(ss);

            OnFrameEvent();

            //Only sends data once the walls have all been sent
            if (wallsDone)
            {
                Networking.Send(ss.TheSocket, JsonConvert.SerializeObject(commands) + "\n");

                //This makes sure that it only sends the beam command once
                if(commands.fire == "alt")
                    commands.fire = "none";
            }

            Networking.GetData(ss);

        }


        /// <summary>
        /// This method processes the data received through the socket by splitting it and deserializing
        /// each string.
        /// </summary>
        /// <param name="ss">Socket state representing the connection</param>
        private void ProcessData(SocketState ss)
        {
            //Splits the string but keeps the '\n' characters
            string totalData = ss.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            lock (TheWorld)
            {
                foreach (string p in parts)
                {
                    //This is to ignore empty strings
                    if (p.Length == 0)
                        continue;
                    //This is so it ignores the last string if it doesn't end in '\n'
                    if (p[p.Length - 1] != '\n')
                        break;

                    //Calls a method to deserialize the data and then removes the data from the buffer
                    UpdateObject(p);
                    ss.RemoveData(0, p.Length);
                }
            }
        }


        /// <summary>
        /// Deserializes an object from an inputted string. This is handled differently depending on which
        /// kind of object it is.
        /// </summary>
        /// <param name="serializedObject">Serialized object string</param>
        private void UpdateObject(string serializedObject)
        {
            JObject obj = JObject.Parse(serializedObject);

            //Tank
            JToken token = obj["tank"];
            if (token != null)
            {
                Tank tank = JsonConvert.DeserializeObject<Tank>(serializedObject);
                TheWorld.Tanks[tank.ID] = tank;

                //Assigns a color to the tank and increments the seen players
                if (!TankColorRecord.ContainsKey(tank.ID))
                {
                    TankColorRecord.Add(tank.ID, SeenPlayers % 8);
                    SeenPlayers++;
                }

                //If it disconnected the tank is removed
                if (tank.Disconnected)
                    TheWorld.Tanks.Remove(tank.ID);

                //It also notes that the walls must be done if it's importing a tank
                wallsDone = true;
                return;
            }

            //Projectile
            token = obj["proj"];
            if (token != null)
            {
                Projectile proj = JsonConvert.DeserializeObject<Projectile>(serializedObject);
                TheWorld.Projectiles[proj.ID] = proj;
                //Removes projectiles when they die
                if (proj.Died)
                    TheWorld.Projectiles.Remove(proj.ID);
                return;
            }

            //Powerup
            token = obj["power"];
            if (token != null)
            {
                PowerUp power = JsonConvert.DeserializeObject<PowerUp>(serializedObject);
                TheWorld.PowerUps[power.ID] = power;
                //Removes powerups when they die
                if (power.Died)
                    TheWorld.PowerUps.Remove(power.ID);
                return;
            }

            //Beam
            token = obj["beam"];
            if (token != null)
            {
                Beam beam = JsonConvert.DeserializeObject<Beam>(serializedObject);
                TheWorld.Beams[beam.ID] = beam;
                return;
            }

            //Wall
            token = obj["wall"];
            if (token != null)
            {
                Wall wall = JsonConvert.DeserializeObject<Wall>(serializedObject);
                TheWorld.Walls[wall.ID] = wall;
                return;
            }
        }

        
        /// <summary>
        /// Method for getting the color of a given tank ID. It returns a number between 1 and 8
        /// that can be assigned to a color.
        /// </summary>
        /// <param name="ID">ID of the tank</param>
        /// <returns>Number between 1 and 8 that can be assigned to a color</returns>
        public int GetColor(int ID)
        {
            return TankColorRecord[ID];
        }
    }
}
