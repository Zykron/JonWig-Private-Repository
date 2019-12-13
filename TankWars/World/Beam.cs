//Authors: Ben Huenemann and Jonathan Wigderson

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TankWars
{
    /// <summary>
    /// Class that represents a beam object. It contains info about the beam that can be serialized
    /// and sent to the server
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Beam
    {
        //Static variable so the IDs of the beams will increment
        static int NextID = 0;

        //Variable for whether or not the beam data has already been sent to the client
        public bool Spawned { get; internal set; } = false;

        //ID of the beam
        [JsonProperty(PropertyName = "beam")]
        public int ID { get; internal set; }

        //Location for the beam to be drawn from
        [JsonProperty(PropertyName = "org")]
        public Vector2D Origin { get; internal set; }

        //Direction for the beam to be drawn
        [JsonProperty(PropertyName = "dir")]
        public Vector2D Orientation { get; internal set; }

        //Owner of the beam
        [JsonProperty(PropertyName = "owner")]
        public int OwnerID { get; internal set; }

        //Dictionary containing the particles around the beam and the frames those particles have been out
        public Dictionary<int, Vector2D> beamParticles = new Dictionary<int, Vector2D>();
        public int beamFrames = 0;


        //Default constructor for JSON
        Beam()
        {
            //Sets the beam ID
            ID = NextID;
            NextID++;
        }


        public Beam(Vector2D originPoint, Vector2D beamOrientation, int owner)
        {
            //Sets the beam ID
            ID = NextID;
            NextID++;

            //Sets the data to the data inputted
            Origin = originPoint;
            Orientation = beamOrientation;
            OwnerID = owner;
        }
    }


}
