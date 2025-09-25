using Microsoft.Xna.Framework.Input;

namespace SoulfulJourney
{
    public static class Global
    {
        public static double gravity = 0.4d; // Gravity in current room
        public static List<int> resolution = [1280, 720]; // Resolution of the Game

        public static List<Sprite> spriteList = new List<Sprite>(); // List of Sprites in the Game

        public static KeyboardState pkbs;   // To track previous keyboard state for input detection
        public static KeyboardState ckbs;   // To track current keyboard state for input detection

        public static int MENU_PRIORITY = 0;
        public static int FRONT_TERRAIN_PRIORITY = 9;
        public static int PLAYER_PRIORITY = 10;
        public static int BACK_TERRAIN_PRIORITY = 11;

    }
}