//Authors: Ben Huenemann and Jonathan Wigderson

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TankWars
{
    /// <summary>
    /// Class that contains commands to send to the server about moving the tank, aiming, and firing
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ControlCommands
    {
        //Tells whether the tank is moving or not
        [JsonProperty(PropertyName = "moving")]
        public string direction = "none";

        //Tells if the tank is firing
        [JsonProperty(PropertyName = "fire")]
        public string fire = "none";

        //Tells where the player is aiming
        [JsonProperty(PropertyName = "tdir")]
        public Vector2D aiming = new Vector2D(0, -1);
    }
}
