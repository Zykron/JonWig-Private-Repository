//Authors: Ben Huenemann and Jonathan Wigderson

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TankWars
{
    /// <summary>
    /// Class that represents a projectile object. It contains info about the projectile that can be serialized
    /// and sent to the server
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Projectile
    {
        //Static variable so the IDs of the beams will increment
        static int NextID = 0;

        //Vector to keep track of the velocity of the projectile
        public Vector2D Velocity { get; private set; }

        //Projectile ID
        [JsonProperty(PropertyName = "proj")]
        public int ID { get; internal set; }

        //Location of Projectile
        [JsonProperty(PropertyName = "loc")]
        public Vector2D Location { get; internal set; }

        //Direction of the Projectile
        [JsonProperty(PropertyName = "dir")]
        public Vector2D Orientation { get; internal set; }

        //Tells whether the Projectile has collied with something or not
        [JsonProperty(PropertyName = "died")]
        public bool Died { get; internal set; } = false;

        //Owner tank ID of the Projectile
        [JsonProperty(PropertyName = "owner")]
        public int OwnerID { get; internal set; }



        //Default constructor for JSON
        public Projectile()
        {
            //Sets the ID
            ID = NextID;
            NextID++;
        }


        public Projectile(Vector2D currentLocation, Vector2D direction, int owner)
        {
            //Sets the ID
            ID = NextID;
            NextID++;

            //Sets the location, orientation, and owner. Also normalizes the orientation and calculates the velocity
            Location = currentLocation;

            Orientation = direction;
            Orientation.Normalize();

            Velocity = Orientation * Constants.ProjectileSpeed;

            OwnerID = owner;

        }
    }
}
