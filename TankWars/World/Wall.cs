//Authors: Ben Huenemann and Jonathan Wigderson

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TankWars
{
    /// <summary>
    /// Class that represents a wall object. It contains info about the wall that can be serialized
    /// and sent to the server
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Wall
    {
        //Static variable so the IDs of the beams will increment
        static int NextID = 0;

        //Wall ID
        [JsonProperty(PropertyName = "wall")]
        public int ID { get; private set; }

        //Endpoint 1 of the wall
        [JsonProperty(PropertyName = "p1")]
        public Vector2D EndPoint1 { get; private set; }

        //Endpoint 2 of the wall
        [JsonProperty(PropertyName = "p2")]
        public Vector2D EndPoint2 { get; private set; }


        //Default constructor for JSON
        public Wall()
        {
            ID = NextID;
            NextID++;
        }

        //Constructo that sets up a wall between two points
        public Wall(Vector2D p1, Vector2D p2)
        {
            ID = NextID;
            NextID++;

            EndPoint1 = p1;
            EndPoint2 = p2;
        }
    }
}
