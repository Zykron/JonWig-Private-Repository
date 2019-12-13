//Authors: Ben Huenemann and Jonathan Wigderson

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TankWars
{
    public class World
    {
        // A stopwatch that is called when starting THE WORLD to keep track of how long THE WORLD has been alive.
        public Stopwatch Duration = new Stopwatch();

        // Dictionary containing the commands of the player
        public Dictionary<int, ControlCommands> PlayerCommands = new Dictionary<int, ControlCommands>();

        // Dictionary containing all players and their respective tanks for saving to the database
        public Dictionary<int, Tank> Players = new Dictionary<int, Tank>();

        //Dictionaries that contain the objects that are in the world. Their IDs are used as the keys
        public Dictionary<int, Tank> Tanks { get; private set; } = new Dictionary<int, Tank>();
        public Dictionary<int, PowerUp> PowerUps { get; private set; } = new Dictionary<int, PowerUp>();
        public Dictionary<int, Beam> Beams { get; private set; } = new Dictionary<int, Beam>();
        public Dictionary<int, Projectile> Projectiles { get; private set; } = new Dictionary<int, Projectile>();
        public Dictionary<int, Wall> Walls { get; private set; } = new Dictionary<int, Wall>();

        public Dictionary<int, Tank> DeadTanks { get; private set; } = new Dictionary<int, Tank>();

        public Dictionary<int, TankExplosion> TankExplosions = new Dictionary<int, TankExplosion>();

        //Keeps track of the world size sent by the server
        public int worldSize;

        // Keeps tracks of how many frames have past for when to spawn a powerup
        public int powerUpFrames = 0;


        /// <summary>
        /// Constructor method for THE WORLD ! ! !
        /// Which starts a stopwatch.
        /// </summary>
        public World()
        {
            Duration.Start();
        }

        
        /// <summary>
        /// Updates the command for a specific player to be send to the client
        /// </summary>
        /// <param name="ID"> The ID of the player command </param>
        /// <param name="c"> Given Control Command to be given to PlayerCommands </param>
        public void UpdateCommand(int ID, ControlCommands c)
        {
            PlayerCommands[ID] = c;
        }

        /// <summary>
        /// Updates all tanks currently present in The World
        /// </summary>
        /// <param name="t"> the tank thats being updated </param>
        public void UpdateTank(Tank t)
        {
            Players[t.ID] = t;
            Tanks[t.ID] = t;
        }

        /// <summary>
        /// Updates all powerups currently present in The World
        /// </summary>
        /// <param name="t"> the powerup thats being updated </param>
        public void UpdatePowerUp(PowerUp p)
        {
            PowerUps[p.ID] = p;
        }

        /// <summary>
        /// Updates all beams currently present in The World
        /// </summary>
        /// <param name="t"> the beam thats being updated </param>
        public void UpdateBeam(Beam b)
        {
            Beams[b.ID] = b;
        }

        /// <summary>
        /// Updates all projectiles currently present in The World
        /// </summary>
        /// <param name="t"> the projectile thats being updated </param>
        public void UpdateProjectile(Projectile p)
        {
            Projectiles[p.ID] = p;
        }

        /// <summary>
        /// Updates all walls currently present in The World
        /// </summary>
        /// <param name="t"> the wall thats being updated </param>
        public void UpdateWall(Wall w)
        {
            Walls[w.ID] = w;
        }


        /// <summary>
        /// Updates the orientation of a tank
        /// </summary>
        /// <param name="ID"> ID of the tank to be updated </param>
        /// <param name="orientation"> orientation for the tank to be set to </param>
        public void TankSetOrientation(int ID, Vector2D orientation)
        {
            Tanks[ID].Orientation = orientation;
            Tanks[ID].Orientation.Normalize();
        }

        /// <summary>
        /// Updates the velocity of a tank
        /// </summary>
        /// <param name="ID"> ID of the tank to be updated </param>
        /// <param name="orientation"> velocity for the tank to be set to </param>
        public void TankSetVelocity(int ID, Vector2D velocity)
        {
            Tanks[ID].Velocity = velocity;
        }

        /// <summary>
        /// Updates the location of a tank
        /// </summary>
        /// <param name="ID"> ID of the tank to be updated </param>
        /// <param name="orientation"> location for the tank to be set to </param>
        public void TankSetLocation(int ID, Vector2D location)
        {
            Tanks[ID].Location = location;
        }

        /// <summary>
        /// Updates the direction a tank is aiming
        /// </summary>
        /// <param name="ID"> ID of the tank to be updated </param>
        /// <param name="orientation"> direction of where the tank is aiming for the tank s turret direction to be set to </param>
        public void TankSetAiming(int ID, Vector2D aiming)
        {
            Tanks[ID].Aiming = aiming;
            Tanks[ID].Aiming.Normalize();
        }

        /// <summary>
        /// Increments the cooldown frames of a specified tank by 1
        /// </summary>
        /// <param name="ID"> ID of the tank whose cooldown frames is to be updated </param>
        public void TankIncrementCooldownFrames(int ID)
        {
            Tanks[ID].CooldownFrames++;
        }

        /// <summary>
        /// Sets the cooldown frames of a specified tank to a given value
        /// </summary>
        /// <param name="ID"> ID of the tank whose cooldown frames are to be set</param>
        /// <param name="value"> Value for the cooldown frames to be set to.</param>
        public void TankSetCooldownFrames(int ID, int value)
        {
            Tanks[ID].CooldownFrames = value;
        }

        /// <summary>
        /// Increments the respawn frames of a specified tank by 1
        /// </summary>
        /// <param name="ID"> ID of the tank whose respawn frames is to be updated </param>
        public void TankIncrementRespawnFrames(int ID)
        {
            DeadTanks[ID].RespawnFrames++;
        }

        /// <summary>
        /// Sets the respawn frames of a specified tank to a given value
        /// </summary>
        /// <param name="ID"> ID of the tank whose cooldown frames are to be set</param>
        /// <param name="value"> Value for the respawn frames to be set to.</param>
        public void TankSetRespawnFrames(int ID, int value)
        {
            Tanks[ID].RespawnFrames = value;
        }

        /// <summary>
        /// Computes the health of a tank when hit by a projectile from another tank
        /// </summary>
        /// <param name="tankID"> ID of tank that has been hit by enemy projectile </param>
        /// <param name="ProjID"> ID of the projectile which is damaging the tank </param>
        public void TankProjectileDamage(int tankID, int ProjID)
        {
            // If health of the given tank is not zero, decreases health by 1.
            if(Tanks[tankID].HitPoints > 1)
                Tanks[tankID].HitPoints--;
            // The given tanks health is zero, so the tank dies and the owner
            // of the projectile given has its score increased by 1.
            else
            {
                if(Tanks.ContainsKey(Projectiles[ProjID].OwnerID))
                {
                    Tanks[Projectiles[ProjID].OwnerID].Score++;

                }
                else if(DeadTanks.ContainsKey(ProjID))
                {
                    DeadTanks[Projectiles[ProjID].OwnerID].Score++;
                }
                Tanks[tankID].HitPoints = 0;
                Tanks[tankID].Died = true;
                DeadTanks[tankID] = Tanks[tankID];
            }
        }

        /// <summary>
        /// Kills the tank when it has been hit by a beam.
        /// Also increased the score of the owner of the beam by one.
        /// </summary>
        /// <param name="tankID"> ID of tank that has been hit by enemy projectile </param>
        /// <param name="ProjID"> ID of the beam which is damaging the tank </param>
        public void TankBeamDamage(int tankID, int BeamID)
        {
            Tanks[Beams[BeamID].OwnerID].Score++;
            Tanks[tankID].HitPoints = 0;
            Tanks[tankID].Died = true;
            DeadTanks[tankID] = Tanks[tankID];
        }

        /// <summary>
        /// Restores a tanks health to the constant MaxHP, 
        /// removing it from the list of dead tanks and making it able to respawn.
        /// </summary>
        /// <param name="ID"> ID of the tank to have its health restored </param>
        public void TankRestoreHealth(int ID)
        {
            Tanks[ID] = DeadTanks[ID];
            Players[ID] = DeadTanks[ID];
            DeadTanks.Remove(ID);

            Tanks[ID].HitPoints = Constants.MaxHP;
            Tanks[ID].Died = false;
        }

        /// <summary>
        /// Removes a tank from the list of dead tanks
        /// </summary>
        /// <param name="ID"> ID of the tank to be removed from the list </param>
        public void TankDeadRemove(int ID)
        {
            DeadTanks.Remove(ID);
        }

        /// <summary>
        /// Removes a tank from the list of alive tanks
        /// </summary>
        /// <param name="ID"> ID of the tank to be removed from the list </param>
        public void TankRemove(int ID)
        {
            Tanks.Remove(ID);
        }

        /// <summary>
        /// Increases the amount of poperups a specified tank has
        /// </summary>
        /// <param name="ID"> ID of the tank to have its powerup count incremented </param>
        public void TankIncrementPowerUps(int ID)
        {
            Tanks[ID].PowerUps++;
        }

        /// <summary>
        /// Decreases the amount of poperups a specified tank has
        /// </summary>
        /// <param name="ID"> ID of the tank to have its powerup count decremented </param>
        public void TankDecrementPowerUps(int ID)
        {
            if(Tanks[ID].PowerUps > 0)
                Tanks[ID].PowerUps--;
        }

        /// <summary>
        /// Sets a tanks disconnected value to true
        /// </summary>
        /// <param name="ID"> ID of the tank to have its disconnected value set to true </param>
        public void TankDisconnect(int ID)
        {
            Tanks[ID].Disconnected = true;
        }

        /// <summary>
        /// Kills a tank, setting its health to zero and its died value to true
        /// </summary>
        /// <param name="ID"> ID of the tank to have its information updated </param>
        public void TankKill(int ID)
        {
            Tanks[ID].Died = true;
            Tanks[ID].HitPoints = 0;
        }

        /// <summary>
        /// Increments the amount of shots a tank has fired when called
        /// </summary>
        /// <param name="ID"> ID of the tank to have its specified information updated </param>
        public void TankIncrementShotsFired(int ID)
        {
            Tanks[ID].ShotsFired++;
        }

        /// <summary>
        /// Increments the amount of shots a tank has fired and hit another tank with when called
        /// </summary>
        /// <param name="ID"> ID of the tank to have its specified information updated </param>
        public void TankIncrementShotsHit(int ID)
        {
            Tanks[ID].ShotsHit++;
        }

        /// <summary>
        /// If a tank is dead and has hit another tank with its projectile...
        /// Increments the amount of shots a tank has fired and hit another tank with when called
        /// </summary>
        /// <param name="ID"> ID of the tank to have its specified information updated </param>
        public void TankDeadIncrementShotsHit(int ID)
        {
            DeadTanks[ID].ShotsHit++;
        }


        /// <summary>
        /// Updates the location of a specified projectile to a specified location
        /// </summary>
        /// <param name="ID"> ID of the projectile to have its location updated </param>
        /// <param name="location"> 2D vector location for the projectile to be updated to </param>
        public void ProjectileSetLocation(int ID, Vector2D location)
        {
            Projectiles[ID].Location = location;
        }

        /// <summary>
        /// Sets the died value of a specified projectile to true
        /// </summary>
        /// <param name="ID"> ID of the projectile to have its specified information updated </param>
        public void ProjectileSetDied(int ID)
        {
            Projectiles[ID].Died = true;
        }

        /// <summary>
        /// Removes a specified projectile from the list of projectiles
        /// </summary>
        /// <param name="ID"> ID of the specified projectile to be removed </param>
        public void ProjectileRemove(int ID)
        {
            Projectiles.Remove(ID);
        }


        /// <summary>
        /// Sets the value of a given beams spawned value to true
        /// </summary>
        /// <param name="ID"> ID of the specified beam to have its information updated </param>
        public void BeamSetSpawnedTrue(int ID)
        {
            Beams[ID].Spawned = true;
        }

        /// <summary>
        /// Removes a specified beam from the dictionary of beams
        /// </summary>
        /// <param name="ID"> ID of the beam to be removed from the dictionary of beams </param>
        public void BeamRemove(int ID)
        {
            Beams.Remove(ID);
        }


        /// <summary>
        /// Sets the location of a specified powerup
        /// </summary>
        /// <param name="ID"> ID of the specified powerup to have its infirmation updated </param>
        /// <param name="location"> 2D Vector lcoation for the specified powerup to be set to </param>
        public void PowerUpSetLocation(int ID, Vector2D location)
        {
            PowerUps[ID].Location = location;
        }

        /// <summary>
        /// Sets the died value of a specified powerup to true
        /// </summary>
        /// <param name="ID"> ID of the specified powerup to have its infromation updated </param>
        public void PowerUpSetDied(int ID)
        {
            PowerUps[ID].Died = true;
        }

        /// <summary>
        /// Removes a specified powerup from the dictionary of pwoerups
        /// </summary>
        /// <param name="ID"> ID of the specified powerup to be removed </param>
        public void PowerUpRemove(int ID)
        {
            PowerUps.Remove(ID);
        }


        /// <summary>
        /// Increments the amount of frames a specific explosion
        /// </summary>
        /// <param name="e"> The specific explosion to have its information updated </param>
        public void ExplosionIncrementFrames(TankExplosion e)
        {
            e.tankFrames++;
        }

        /// <summary>
        /// Clears the frames of a specific tank explosion
        /// </summary>
        /// <param name="e"> The specific explosion to have its infromation updated </param>
        public void ExplosionClearFrames(TankExplosion e)
        {
            e.tankFrames = 0;
        }
    }
}
