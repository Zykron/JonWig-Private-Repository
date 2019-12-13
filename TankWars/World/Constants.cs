//Authors: Ben Huenemann and Jonathan Wigderson

using System;
using System.Collections.Generic;
using System.Text;

namespace TankWars
{
    /// <summary>
    /// Static class that contains constants for various objects in the game so they can easily be changed
    /// </summary>
    public static class Constants
    {
        //Constansts for the speeds
        public const double TankSpeed = 2.9;
        public const int ProjectileSpeed = 25;

        //Tanks HP
        public const int MaxHP = 3;
        public const int ViewSize = 800;

        //Health bar constants
        public const int HealthBarX = -20;
        public const int HealthBarY = -36;
        public const int HealthBarFull = 40;
        public const int HealthBarHigh = 25;
        public const int HealthBarLow = 10;
        public const int HealthBarHeight = 5;

        //Name bar constants
        public const int NameBarX = -20;
        public const int NameBarY = 26;
        public const int NameBarXMultiplier = 5;

        //Location to put the drawing panel
        public const int ViewLocationX = 10;
        public const int ViewLocationY = 45;

        //Information for the beam particles
        public const int BeamParticleCount = 30;
        public const int BeamParticleSpeed = 5;
        public const int BeamParticleRadius = 3;
        public const int BeamFrameLength = 30;

        //Information for drawing the tank particles
        public const int TankParticleCount = 50;
        public const int TankParticleSpeed = 2;
        public const int TankParticleSpawnRadius = 5;
        public const int TankParticleRadius = 7;
        public const int TankParticleFrameLength = 30;

        //Sizes to draw each object
        public const int TankSize = 60;
        public const int TurretSize = 50;
        public const int PowerUpSize = 16;
        public const int ProjectileSize = 30;
        public const int WallSize = 50;
        public const int BeamWidth = 6;

        //Information for powerups
        public const int MaxPowerUps = 2;
        public const int MaxPowerUpDelay = 1650;
    }
}
