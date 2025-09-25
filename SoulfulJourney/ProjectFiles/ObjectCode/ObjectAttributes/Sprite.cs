using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SoulfulJourney
{
    public class Sprite
    {
        // Sprites are positioned at the TOP LEFT corner!
        protected double x = 0;
        protected double y = 0;

        protected double xspeed = 0;
        protected double yspeed = 0;

        protected double width = 32;
        protected double height = 32;

        protected Texture2D? texture;
        protected Rectangle rectangle;

        // Draw priority for the sprite: Lower Number = Higher Priority
        protected int priority;

        // Specifies collision from the top side, right side, bottom side, and the left side in that order
        protected List<bool> collisionMap = [false, false, false, false];

        public Sprite(double x, double y, double width, double height, int priority, Texture2D? texture, List<bool> collisionMap)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.priority = priority;
            this.texture = texture;
            this.collisionMap = collisionMap;

            // Create rectangle
            rectangle = new Rectangle((int)Math.Round(x), (int)Math.Round(y), (int)Math.Round(width), (int)Math.Round(height));

            // Insert item into spriteList according to priority
            bool found = false;
            for(int i = 0; i < Global.spriteList.Count; i++){
                if(Global.spriteList[i].priority >= priority){
                    Global.spriteList.Insert(i,this);
                    found = true;
                    break;
                }
            }
            if (!found) { Global.spriteList.Add(this); }
            Global.spriteList = Global.spriteList.OrderBy(o => o.priority).ToList();
        }

        public double getX() { return x; }
        public void setX(double x) { this.x = x; rectangle.X = (int)Math.Round(x); }

        public double getY() { return y; }
        public void setY(double y) { this.y = y; rectangle.Y = (int)Math.Round(y); }

        public double getWidth() { return width; }
        public void setWidth(double width) { this.width = width; rectangle.Width = (int)Math.Round(width); }

        public double getHeight() { return height; }
        public void setHeight(double height) { this.height = height; rectangle.Height = (int)Math.Round(height); }

        public Texture2D? getTexture() { return texture; }
        public void setTexture(Texture2D? texture) { this.texture = texture; }

        public Rectangle getRectangle() { return rectangle; }
        public void updateRectangle() {
            rectangle.X = (int)Math.Round(x);
            rectangle.Y = (int)Math.Round(y);
            rectangle.Width = (int)Math.Round(width);
            rectangle.Height = (int)Math.Round(height);
        }

        public List<bool> getCollisionMap() { return collisionMap; }

        public virtual string toString()
        {
            return "Sprite";
        }
    }
}