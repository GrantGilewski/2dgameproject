using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SoulfulJourney
{
    public class Player : Sprite
    {
        protected double playerSpeed = 4;

        protected bool isGrounded = false;            // Jump variables
        protected bool inAir = false;
        protected double jumpStrength = 15;

        public Player(double x, double y, double width, double height, Texture2D? texture) : base(x, y, width, height, Global.PLAYER_PRIORITY, texture, [true, true, true, true])
        {

        }

        public override string toString()
        {
            return "Player";
        }

        // Check button inputs that affect the player
        public void controls()
        {

            // Check if player is moving
            bool movingLeft = false;
            bool movingRight = false;

            // Move player left
            if (Global.ckbs.IsKeyDown(Keys.Left) || Global.ckbs.IsKeyDown(Keys.A))
            {
                xspeed = -playerSpeed;
                movingLeft = true;
            }
            // Move player right
            if (Global.ckbs.IsKeyDown(Keys.Right) || Global.ckbs.IsKeyDown(Keys.D))
            {
                xspeed = playerSpeed;
                movingRight = true;
            }

            // If both left and right are held down or if neither are held down, dont move the player
            if ((movingLeft && movingRight) || (!movingLeft && !movingRight)){
                xspeed = 0;
            }

            x += xspeed;

            // Check for bumping on the sides of a platform
            double prevX = x - xspeed;
            foreach (Sprite sprite in Global.spriteList){
                if (sprite != this)
                {
                    if (xspeed < 0) // Right
                    {
                        if (sprite.getCollisionMap()[1])
                        {
                            double right = sprite.getX() + sprite.getWidth(); // x at which player's x should be at when colliding
                            bool verticallyOverlapping = y + height > sprite.getY() && y < sprite.getY() + sprite.getHeight();
                            if (prevX >= right && x <= right && verticallyOverlapping)
                            {
                                x = right;
                                xspeed = 0;
                                break;
                            }
                        }
                    }
                    if (xspeed > 0) // Left
                    {
                        if (sprite.getCollisionMap()[1])
                        {
                            double left = sprite.getX() - width; // x at which player's x should be at when colliding
                            bool verticallyOverlapping = y + height > sprite.getY() && y < sprite.getY() + sprite.getHeight();
                            if (prevX <= left && x >= left && verticallyOverlapping)
                            {
                                x = left;
                                xspeed = 0;
                                break;
                            }
                        }
                    }
                }
            }

            // Clamp player within screen bounds
            x = Math.Max(0, Math.Min(Global.resolution[0] - width, x));

            // Determine whether player is currently standing on something
            bool onPlatform = false;
            foreach (Sprite sprite in Global.spriteList)
            {
                if (sprite != this){
                    // If Sprite has top collision, run top collision logic
                    if (sprite.getCollisionMap()[0])
                    {
                        double top = sprite.getY();
                        if ((Math.Abs(y + height - sprite.getY()) < 0.5f) && (x + width > sprite.getX() && x < sprite.getX() + sprite.getWidth()))
                        {
                            onPlatform = true;
                            break;
                        }
                    }
                }
            }

            bool isGrounded = onPlatform;
            // If player walked off a surface (was grounded but now not), start falling
            if (!isGrounded && !inAir)
            {
                inAir = true;
                yspeed = 0f; // start falling from rest
            }

            // Jump logic: detect a key-press (not just held key); only allow jump when grounded
            bool jumpKeyPressed = (Global.ckbs.IsKeyDown(Keys.Space))
                                  || (Global.ckbs.IsKeyDown(Keys.W))
                                  || (Global.ckbs.IsKeyDown(Keys.Up));

            if (!inAir && isGrounded && jumpKeyPressed)
            {
                inAir = true;
                yspeed = -jumpStrength;
            }

            if (inAir)
            {
                // Apply vertical movement using current velocity, then integrate gravity
                yspeed += Global.gravity;
                y += yspeed;

                // Check for bumping on top of any platform (only when moving upward)
                if (yspeed < 0f)
                {
                    double prevY = y - yspeed;
                    foreach (Sprite sprite in Global.spriteList)
                    {
                        if (sprite.getCollisionMap()[2])
                        {
                            double bottom = sprite.getY() + sprite.getHeight(); // y at which player's y should be at when colliding
                            bool horizontallyOverlapping = x + width > sprite.getX() && x < sprite.getX() + sprite.getWidth();
                            if (prevY > bottom && y <= bottom && horizontallyOverlapping)
                            {
                                y = bottom;

                                // Some speed conversion
                                yspeed = -yspeed*0.25;
                                break;
                            }
                        }
                    }
                }

                // Check for landing on any platform (only when moving downward)
                if (yspeed > 0f)
                {
                    double prevY = y - yspeed;
                    foreach (Sprite sprite in Global.spriteList){
                        if (sprite.getCollisionMap()[0]){
                            double top = sprite.getY() - height; // y at which player's Y should land
                            bool horizontallyOverlapping = x + width > sprite.getX() && x < sprite.getX() + sprite.getWidth();
                            if (prevY < top && y >= top && horizontallyOverlapping){
                                y = top;
                                yspeed = 0;
                                inAir = false;
                                break;
                            }
                        }
                    }
                }
            }

            // Update Rectangle
            updateRectangle();

        }
    }
}