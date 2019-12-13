using System;
using System.Collections.Generic;
using System.Text;

namespace TankWars
{
    /// <summary>
    /// Class that represents an explosion that's spawned when a tank dies. This is only used for the client
    /// </summary>
    public class TankExplosion
    {
        //Dictionary that contains the locations of each tank explosion particle
        public Dictionary<int, Vector2D> tankParticles = new Dictionary<int, Vector2D>();

        //Frames that the explosion has existed for
        public int tankFrames { get; internal set; } = 0;
    }
}
