//Authors: Ben Huenemann and Jonathan Wigderson

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TankWars
{
    /// <summary>
    /// Component class for drawing the game that extends the pannel class
    /// </summary>
    public class DrawingPanel : Panel
    {
        //Stores the controller that was inputted when the panel was created
        private GameController TheController;

        //Loads the images for various visuals in the game, such as tanks, backgrounds, walls, etc...
        private Image background = Image.FromFile(@"..\\..\\..\\Resources\Images\Background.png");

        private Image sourceImageBlueTank = Image.FromFile(@"..\\..\\..\\Resources\Images\BlueTank.png");
        private Image sourceImageDarkTank = Image.FromFile(@"..\\..\\..\\Resources\Images\DarkTank.png");
        private Image sourceImageGreenTank = Image.FromFile(@"..\\..\\..\\Resources\Images\GreenTank.png");
        private Image sourceImageLightGreenTank = Image.FromFile(@"..\\..\\..\\Resources\Images\LightGreenTank.png");
        private Image sourceImageOrangeTank = Image.FromFile(@"..\\..\\..\\Resources\Images\OrangeTank.png");
        private Image sourceImagePurpleTank = Image.FromFile(@"..\\..\\..\\Resources\Images\PurpleTank.png");
        private Image sourceImageRedTank = Image.FromFile(@"..\\..\\..\\Resources\Images\RedTank.png");
        private Image sourceImageYellowTank = Image.FromFile(@"..\\..\\..\\Resources\Images\YellowTank.png");

        private Image sourceImageBlueTurret = Image.FromFile(@"..\\..\\..\\Resources\Images\BlueTurret.png");
        private Image sourceImageDarkTurret = Image.FromFile(@"..\\..\\..\\Resources\Images\DarkTurret.png");
        private Image sourceImageGreenTurret = Image.FromFile(@"..\\..\\..\\Resources\Images\GreenTurret.png");
        private Image sourceImageLightGreenTurret = Image.FromFile(@"..\\..\\..\\Resources\Images\LightGreenTurret.png");
        private Image sourceImageOrangeTurret = Image.FromFile(@"..\\..\\..\\Resources\Images\OrangeTurret.png");
        private Image sourceImagePurpleTurret = Image.FromFile(@"..\\..\\..\\Resources\Images\PurpleTurret.png");
        private Image sourceImageRedTurret = Image.FromFile(@"..\\..\\..\\Resources\Images\RedTurret.png");
        private Image sourceImageYellowTurret = Image.FromFile(@"..\\..\\..\\Resources\Images\YellowTurret.png");

        private Image sourceImageBlueShot = Image.FromFile(@"..\\..\\..\\Resources\Images\shot-blue.png");
        private Image sourceImageDarkShot = Image.FromFile(@"..\\..\\..\\Resources\Images\shot-grey.png");
        private Image sourceImageGreenShot = Image.FromFile(@"..\\..\\..\\Resources\Images\shot-green.png");
        private Image sourceImageLightGreenShot = Image.FromFile(@"..\\..\\..\\Resources\Images\shot-white.png");
        private Image sourceImageOrangeShot = Image.FromFile(@"..\\..\\..\\Resources\Images\shot-brown.png");
        private Image sourceImagePurpleShot = Image.FromFile(@"..\\..\\..\\Resources\Images\shot-violet.png");
        private Image sourceImageRedShot = Image.FromFile(@"..\\..\\..\\Resources\Images\shot-red.png");
        private Image sourceImageYellowShot = Image.FromFile(@"..\\..\\..\\Resources\Images\shot-yellow.png");

        private Image sourceImageWall = Image.FromFile(@"..\\..\\..\\Resources\Images\WallSprite.png");



        /// <summary>
        /// This constructor saves the inputted controller for the drawing panel to use
        /// </summary>
        /// <param name="controller"></param>
        public DrawingPanel(GameController controller)
        {
            DoubleBuffered = true;
            TheController = controller;
        }

        /// <summary>
        /// Helper method for DrawObjectWithTransform
        /// </summary>
        /// <param name="size">The world (and image) size</param>
        /// <param name="w">The worldspace coordinate</param>
        /// <returns></returns>
        private static int WorldSpaceToImageSpace(int size, double w)
        {
            return (int)w + size / 2;
        }

        // A delegate for DrawObjectWithTransform
        // Methods matching this delegate can draw whatever they want using e  
        public delegate void ObjectDrawer(object o, PaintEventArgs e);

        /// <summary>
        /// This method performs a translation and rotation to drawn an object in the world.
        /// </summary>
        /// <param name="e">PaintEventArgs to access the graphics (for drawing)</param>
        /// <param name="o">The object to draw</param>
        /// <param name="worldSize">The size of one edge of the world (assuming the world is square)</param>
        /// <param name="worldX">The X coordinate of the object in world space</param>
        /// <param name="worldY">The Y coordinate of the object in world space</param>
        /// <param name="angle">The orientation of the objec, measured in degrees clockwise from "up"</param>
        /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
        private void DrawObjectWithTransform(PaintEventArgs e, object o, int worldSize, double worldX, double worldY, double angle, ObjectDrawer drawer)
        {
            //"push" the current transform
            System.Drawing.Drawing2D.Matrix oldMatrix = e.Graphics.Transform.Clone();

            //Transform the image and draw it
            int x = WorldSpaceToImageSpace(worldSize, worldX);
            int y = WorldSpaceToImageSpace(worldSize, worldY);
            e.Graphics.TranslateTransform(x, y);
            e.Graphics.RotateTransform((float)angle);
            drawer(o, e);

            //"pop" the transform
            e.Graphics.Transform = oldMatrix;
        }

        /// <summary>
        /// This method is invoked when the DrawingPanel is invalidated and needs to be redrawn
        /// </summary>
        /// <param name="e">Graphics for drawing objects</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            //If the walls aren't imported, it shouldn't try to draw anything so it just returns
            if (!TheController.wallsDone)
                return;

            //Store the player tank location
            double playerX = TheController.GetPlayerTank().Location.GetX();
            double playerY = TheController.GetPlayerTank().Location.GetY();

            //Calculate view/world size ratio
            double ratio = (double)Constants.ViewSize / (double)TheController.TheWorld.worldSize;
            int halfSizeScaled = (int)(TheController.TheWorld.worldSize / 2.0 * ratio);

            //Amount to translate the x and y coordinates
            double inverseTranslateX = -WorldSpaceToImageSpace(TheController.TheWorld.worldSize, playerX) + halfSizeScaled;
            double inverseTranslateY = -WorldSpaceToImageSpace(TheController.TheWorld.worldSize, playerY) + halfSizeScaled;

            //Translates the image and draws it
            e.Graphics.TranslateTransform((float)inverseTranslateX, (float)inverseTranslateY);
            e.Graphics.DrawImage(background, 0, 0, TheController.TheWorld.worldSize, TheController.TheWorld.worldSize);


            lock (TheController.TheWorld)
            {

                // Draw the walls
                foreach (Wall wall in TheController.TheWorld.Walls.Values)
                {
                    int Width = (int)Math.Abs(wall.EndPoint1.GetX() - wall.EndPoint2.GetX());
                    int Height = (int)Math.Abs(wall.EndPoint1.GetY() - wall.EndPoint2.GetY());

                    int MinX = (int)Math.Min(wall.EndPoint1.GetX(), wall.EndPoint2.GetX()) - Constants.WallSize/2;
                    int MinY = (int)Math.Min(wall.EndPoint1.GetY(), wall.EndPoint2.GetY()) - Constants.WallSize/2;

                    //Call DrawObjectWithTransform on each individual portion of the wall
                    for (int i = 0; i <= Width; i += Constants.WallSize)
                    {
                        for (int j = 0; j <= Height; j += Constants.WallSize)
                        {
                            DrawObjectWithTransform(e, wall, TheController.TheWorld.worldSize, MinX + i, MinY + j, 0, WallDrawer);
                        }
                    }
                }

                // Draw the players
                foreach (Tank tank in TheController.TheWorld.Tanks.Values)
                {
                    tank.Orientation.Normalize();
                    tank.Aiming.Normalize();
                    DrawObjectWithTransform(e, tank, TheController.TheWorld.worldSize, tank.Location.GetX(), tank.Location.GetY(), tank.Orientation.ToAngle(),
                        TankDrawer);
                    DrawObjectWithTransform(e, tank, TheController.TheWorld.worldSize, tank.Location.GetX(), tank.Location.GetY(), tank.Aiming.ToAngle(),
                        TurretDrawer);
                    DrawObjectWithTransform(e, tank, TheController.TheWorld.worldSize, tank.Location.GetX(), tank.Location.GetY(), 0,
                        NameDrawer);
                    DrawObjectWithTransform(e, tank, TheController.TheWorld.worldSize, tank.Location.GetX(), tank.Location.GetY(), 0,
                        HealthDrawer);
                }

                // Draw the powerups
                foreach (PowerUp pow in TheController.TheWorld.PowerUps.Values)
                {
                    DrawObjectWithTransform(e, pow, TheController.TheWorld.worldSize, pow.Location.GetX(), pow.Location.GetY(), 0, PowerUpDrawer);
                }

                // Draw the beams
                foreach (Beam beam in TheController.TheWorld.Beams.Values)
                {
                    DrawObjectWithTransform(e, beam, TheController.TheWorld.worldSize, beam.Origin.GetX(), beam.Origin.GetY(), beam.Orientation.ToAngle(), BeamDrawer);
                }

                // Draw the projectiles
                foreach (Projectile proj in TheController.TheWorld.Projectiles.Values)
                {
                    proj.Orientation.Normalize();
                    DrawObjectWithTransform(e, proj, TheController.TheWorld.worldSize, proj.Location.GetX(), proj.Location.GetY(), proj.Orientation.ToAngle(), ProjectileDrawer);
                }
                
                // If the there are any beams that have been out for too long it removes them
                if(TheController.TheWorld.Beams.Count != 0)
                {
                    foreach(Beam b in TheController.TheWorld.Beams.Values.ToList())
                    {
                        if (b.beamFrames == Constants.BeamFrameLength)
                        {
                            TheController.TheWorld.Beams.Remove(b.ID);
                        }
                    }
                }
            }

            // Do anything that Panel (from which we inherit) needs to do
            base.OnPaint(e);
        }

        /// <summary>
        /// Method for drawing the tanks
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void TankDrawer(object o, PaintEventArgs e)
        {
            Tank t = o as Tank;

            //Stops tank from updating anymore information if health equals zero
            if (t.HitPoints == 0)
            {
                DrawExplosion(t, e);
                return;
            }
            //Resets the tank frames when the tank isn't dead
            else if(TheController.TheWorld.TankExplosions.ContainsKey(t.ID) && (TheController.TheWorld.TankExplosions[t.ID].tankFrames != 0 && !t.Died))
                TheController.TheWorld.ExplosionClearFrames(TheController.TheWorld.TankExplosions[t.ID]);

            int tankWidth = Constants.TankSize;
            int tankHeight = Constants.TankSize;

            // Gets color ID of the tank
            int colorID = TheController.GetColor(t.ID);

            //Determines tank color based on the color ID of the tank
            switch(colorID)
            {
                //Blue
                case 0:
                    e.Graphics.DrawImage(sourceImageBlueTank, -(tankWidth / 2), -(tankHeight / 2), tankWidth, tankHeight);
                    break;
                //Dark
                case 1:
                    e.Graphics.DrawImage(sourceImageDarkTank, -(tankWidth / 2), -(tankHeight / 2), tankWidth, tankHeight);
                    break;
                //Green
                case 2:
                    e.Graphics.DrawImage(sourceImageGreenTank, -(tankWidth / 2), -(tankHeight / 2), tankWidth, tankHeight);
                    break;
                //Light Green
                case 3:
                    e.Graphics.DrawImage(sourceImageLightGreenTank, -(tankWidth / 2), -(tankHeight / 2), tankWidth, tankHeight);
                    break;
                //Orange
                case 4:
                    e.Graphics.DrawImage(sourceImageOrangeTank, -(tankWidth / 2), -(tankHeight / 2), tankWidth, tankHeight);
                    break;
                //Purple
                case 5:
                    e.Graphics.DrawImage(sourceImagePurpleTank, -(tankWidth / 2), -(tankHeight / 2), tankWidth, tankHeight);
                    break;
                //Red
                case 6:
                    e.Graphics.DrawImage(sourceImageRedTank, -(tankWidth / 2), -(tankHeight / 2), tankWidth, tankHeight);
                    break;
                //Yellow
                case 7:
                    e.Graphics.DrawImage(sourceImageYellowTank, -(tankWidth / 2), -(tankHeight / 2), tankWidth, tankHeight);
                    break;
            }

        }

        /// <summary>
        /// Method for drawing the Tank Turrets
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void TurretDrawer(object o, PaintEventArgs e)
        {
            Tank t = o as Tank;

            //Stops turret from updating anymore information if health equals zero
            if (t.HitPoints == 0)
                return;

            int turretWidth = Constants.TurretSize;
            int turretHeight = Constants.TurretSize;

            //Gets color ID of the turret
            int colorID = TheController.GetColor(t.ID);

            //Determines color of the tank turret based on the color ID
            switch (colorID)
            {
                //Blue
                case 0:
                    e.Graphics.DrawImage(sourceImageBlueTurret, -(turretWidth / 2), -(turretHeight / 2), turretWidth, turretHeight);
                    break;
                //Dark
                case 1:
                    e.Graphics.DrawImage(sourceImageDarkTurret, -(turretWidth / 2), -(turretHeight / 2), turretWidth, turretHeight);
                    break;
                //Green
                case 2:
                    e.Graphics.DrawImage(sourceImageGreenTurret, -(turretWidth / 2), -(turretHeight / 2), turretWidth, turretHeight);
                    break;
                //Light Green
                case 3:
                    e.Graphics.DrawImage(sourceImageLightGreenTurret, -(turretWidth / 2), -(turretHeight / 2), turretWidth, turretHeight);
                    break;
                //Orange
                case 4:
                    e.Graphics.DrawImage(sourceImageOrangeTurret, -(turretWidth / 2), -(turretHeight / 2), turretWidth, turretHeight);
                    break;
                //Purple
                case 5:
                    e.Graphics.DrawImage(sourceImagePurpleTurret, -(turretWidth / 2), -(turretHeight / 2), turretWidth, turretHeight);
                    break;
                //Red
                case 6:
                    e.Graphics.DrawImage(sourceImageRedTurret, -(turretWidth / 2), -(turretHeight / 2), turretWidth, turretHeight);
                    break;
                //Orange
                case 7:
                    e.Graphics.DrawImage(sourceImageYellowTurret, -(turretWidth / 2), -(turretHeight / 2), turretWidth, turretHeight);
                    break;
            }
        }

        /// <summary>
        /// Method for drawing the players names below the tanks
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void NameDrawer(object o, PaintEventArgs e)
        {
            Tank t = o as Tank;

            //Stops name from updating anymore information if health equals zero
            if (t.HitPoints == 0)
                return;

            using (Font font1 = new Font("Times New Roman", 24, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                PointF pointF1 = new PointF(Constants.NameBarX - t.Name.Length * Constants.NameBarXMultiplier, Constants.NameBarY);
                e.Graphics.DrawString(t.Name + ": " + t.Score, font1, Brushes.White, pointF1);
            }
        }

        /// <summary>
        /// Method for drawing the health bars above the tanks
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void HealthDrawer(object o, PaintEventArgs e)
        {
            Tank t = o as Tank;

            using (SolidBrush greenBrush = new SolidBrush(Color.Green))
            using (SolidBrush yellowBrush = new SolidBrush(Color.Yellow))
            using (SolidBrush redBrush = new SolidBrush(Color.Red))
            {
                if(t.HitPoints == 3)
                    e.Graphics.FillRectangle(greenBrush, Constants.HealthBarX, Constants.HealthBarY, Constants.HealthBarFull, Constants.HealthBarHeight);

                if (t.HitPoints == 2)
                    e.Graphics.FillRectangle(yellowBrush, Constants.HealthBarX, Constants.HealthBarY, Constants.HealthBarHigh, Constants.HealthBarHeight);

                if (t.HitPoints == 1)
                    e.Graphics.FillRectangle(redBrush, Constants.HealthBarX, Constants.HealthBarY, Constants.HealthBarLow, Constants.HealthBarHeight);
            }
        }

        /// <summary>
        /// Method for drawing the power-ups
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void PowerUpDrawer(object o, PaintEventArgs e)
        {
            PowerUp p = o as PowerUp;

            int width = Constants.PowerUpSize;
            int height = Constants.PowerUpSize;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (SolidBrush blackBrush = new SolidBrush(Color.Black))
            using (SolidBrush redBrush = new SolidBrush(Color.Red))
            {
                e.Graphics.FillEllipse(blackBrush, -(width / 2), -(height / 2), width, height);
                e.Graphics.FillEllipse(redBrush, -(width / 4), -(height / 4), width / 2, height / 2);
            }
        }

        /// <summary>
        /// Method for drawing the beams
        /// </summary>
        /// <param name="o">Beam to be drawn</param>
        /// <param name="e">Graphics to draw the beam</param>
        private void BeamDrawer(object o, PaintEventArgs e)
        {
            Beam b = o as Beam;

            int width = Constants.BeamWidth;

            //After a certain amount of frames it divides the width by three
            if (b.beamFrames > Constants.BeamFrameLength / 3)
                width = Constants.BeamWidth / 3;

            // Draw portion of source image
            using (SolidBrush goldBrush = new SolidBrush(Color.Gold))
            using (Pen pen = new Pen(goldBrush, width))
                e.Graphics.DrawLine(pen, 0, 0, 0, -Constants.ViewSize);

            DrawBeamParticles(b, e);
            
            b.beamFrames++;
        }

        /// <summary>
        /// Method for drawing the projectiles of the tanks with their respective colors
        /// </summary>
        /// <param name="o">Projectile to be drawn</param>
        /// <param name="e">Graphics for drawing the projectile</param>
        private void ProjectileDrawer(object o, PaintEventArgs e)
        {
            Projectile p = o as Projectile;

            int width = Constants.ProjectileSize;
            int height = Constants.ProjectileSize;

            //Gets color ID of the projectile
            int colorID = TheController.GetColor(p.OwnerID);

            //Determines color of the projectile based on the color ID
            switch (colorID)
            {
                //Blue
                case 0:
                    e.Graphics.DrawImage(sourceImageBlueShot, -(width / 2), -(height / 2), width, height);
                    break;
                //Dark
                case 1:
                    e.Graphics.DrawImage(sourceImageDarkShot, -(width / 2), -(height / 2), width, height);
                    break;
                //Green
                case 2:
                    e.Graphics.DrawImage(sourceImageGreenShot, -(width / 2), -(height / 2), width, height);
                    break;
                //Light Green
                case 3:
                    e.Graphics.DrawImage(sourceImageLightGreenShot, -(width / 2), -(height / 2), width, height);
                    break;
                //Orange
                case 4:
                    e.Graphics.DrawImage(sourceImageOrangeShot, -(width / 2), -(height / 2), width, height);
                    break;
                //Purple
                case 5:
                    e.Graphics.DrawImage(sourceImagePurpleShot, -(width / 2), -(height / 2), width, height);
                    break;
                //Red
                case 6:
                    e.Graphics.DrawImage(sourceImageRedShot, -(width / 2), -(height / 2), width, height);
                    break;
                //Yellow
                case 7:
                    e.Graphics.DrawImage(sourceImageYellowShot, -(width / 2), -(height / 2), width, height);
                    break;
            }
        }

        /// <summary>
        /// Method for drawing walls. This only draws a single wall since it is called multiple times in OnPaint for each
        /// wall segment
        /// </summary>
        /// <param name="o">Wall to be drawn</param>
        /// <param name="e">Graphics for drawing wall</param>
        private void WallDrawer(object o, PaintEventArgs e)
        {
            e.Graphics.DrawImage(sourceImageWall, 0, 0, Constants.WallSize, Constants.WallSize);
        }


        /// <summary>
        /// Method for drawing the particles around the beam. If the particles don't exist yet, it spawns them at constant
        /// spots along the length of the beam. If they do exist it generates a random angle and moves them in that direction.
        /// </summary>
        /// <param name="b">Beam that was spawned</param>
        /// <param name="e">Graphics for drawing beam particles</param>
        private void DrawBeamParticles(Beam b, PaintEventArgs e)
        {
            Random rnd = new Random();

            for (int i = 0; i < Constants.BeamParticleCount; i++)
            {
                if (b.beamParticles.ContainsKey(i))
                {
                    double Angle = rnd.NextDouble() * 2 * Math.PI;

                    b.beamParticles[i] += new Vector2D(Constants.BeamParticleSpeed * Math.Cos(Angle), Constants.BeamParticleSpeed * Math.Sin(Angle));
                }
                else
                {
                    int LocationY = -i * (Constants.ViewSize / Constants.BeamParticleCount);
                    b.beamParticles[i] = new Vector2D(0, LocationY);
                }

                using(SolidBrush whiteBrush = new SolidBrush(Color.White))
                    e.Graphics.FillEllipse(whiteBrush, (int)b.beamParticles[i].GetX(), (int)b.beamParticles[i].GetY(), Constants.BeamParticleRadius, Constants.BeamParticleRadius);
            }
        }


        /// <summary>
        /// Method for drawing a tank explosion for some tank t. If the particles haven't been created yet, it spawns them at
        /// a random angle some distance away from the tank. Then when this method is called next it increases the distance so
        /// those particles appear to move outward. It also keeps track of the amount of frames the particles have been out and
        /// if that exceeds the tank frames constant it gets rid of the particles.
        /// </summary>
        /// <param name="t">Tank that died</param>
        /// <param name="e">Graphics for drawing the particles</param>
        private void DrawExplosion(Tank t, PaintEventArgs e)
        {
            Dictionary<int, TankExplosion> explosionDictionary = TheController.TheWorld.TankExplosions;

            Random rnd = new Random();

            //Creates a new explosion if it doesn't exist yet
            if (!explosionDictionary.ContainsKey(t.ID))
                explosionDictionary[t.ID] = new TankExplosion();

            //Gets rid of the particles after a certain amount of frames
            if (explosionDictionary[t.ID].tankFrames > Constants.TankParticleFrameLength)
            {
                if (explosionDictionary[t.ID].tankParticles.Count > 0)
                    explosionDictionary[t.ID].tankParticles.Clear();
                return;
            }

            Dictionary<int, Vector2D> TankParticlesDictionary = explosionDictionary[t.ID].tankParticles;

            for (int i = 0; i < Constants.TankParticleCount; i++)
            {
                if (TankParticlesDictionary.ContainsKey(i))
                {
                    //Creates a direction vector and moves the particle in that direction
                    Vector2D direction = new Vector2D(TankParticlesDictionary[i]);
                    direction.Normalize();

                    TankParticlesDictionary[i] += direction * Constants.TankParticleSpeed;
                }
                else
                {
                    //Creates a random angle and spawns the particle at that location
                    double Angle = rnd.NextDouble() * 2 * Math.PI;

                    TankParticlesDictionary[i] = new Vector2D(Constants.TankParticleSpawnRadius * Math.Cos(Angle), Constants.TankParticleSpawnRadius * Math.Sin(Angle));
                }

                using (SolidBrush redBrush = new SolidBrush(Color.Red))
                    e.Graphics.FillEllipse(redBrush, (int)TankParticlesDictionary[i].GetX(), (int)TankParticlesDictionary[i].GetY(), Constants.TankParticleRadius, Constants.TankParticleRadius);
            }
            TheController.TheWorld.ExplosionIncrementFrames(explosionDictionary[t.ID]);
        }
    }
}

