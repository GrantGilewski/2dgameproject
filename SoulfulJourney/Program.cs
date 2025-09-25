using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization.Formatters;

namespace SoulfulJourney
{
    public class GameEngine : Game
    {
        private GraphicsDeviceManager _graphics;

        private SpriteBatch? _spriteBatch;

        private Texture2D? testTexture;
        private Texture2D? testTexture2;

        private Player player;
        private Texture2D? _playerTexture;        // Player variables
        private const int PlayerWidth = 32;     // Player dimensions
        private const int PlayerHeight = 32;

        public GameEngine()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;

            // Cap at 60 FPS
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);

            _graphics.PreferredBackBufferWidth = Global.resolution[0];
            _graphics.PreferredBackBufferHeight = Global.resolution[1];
        }

        protected override void Initialize()
        {
            Global.pkbs = Keyboard.GetState();
            base.Initialize();
        }
        
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            testTexture = Texture2D.FromFile(GraphicsDevice, "ProjectFiles/Assets/test.png");
            testTexture2 = Texture2D.FromFile(GraphicsDevice, "ProjectFiles/Assets/test2.png");

            // Baseplate
            Sprite baseplate = new Sprite(0, _graphics.PreferredBackBufferHeight - 32, _graphics.PreferredBackBufferWidth, 32, Global.BACK_TERRAIN_PRIORITY, testTexture2, [true, false, false, false]);

            // Platforms
            new Sprite(100, 530, 150, 150, Global.BACK_TERRAIN_PRIORITY, Texture2D.FromFile(GraphicsDevice, "ProjectFiles/Assets/All.png"), [true, true, true, true]);
            new Sprite(400, 600, 80, 80, Global.BACK_TERRAIN_PRIORITY, Texture2D.FromFile(GraphicsDevice, "ProjectFiles/Assets/topOnly.png"), [true, false, false, false]);
            new Sprite(800, 500, 80, 80, Global.BACK_TERRAIN_PRIORITY, Texture2D.FromFile(GraphicsDevice, "ProjectFiles/Assets/topAndBottom.png"), [true, false, true, false]);
            new Sprite(600, 400, 80, 80, Global.BACK_TERRAIN_PRIORITY, Texture2D.FromFile(GraphicsDevice, "ProjectFiles/Assets/All.png"), [true, true, true, true]);
            new Sprite(900, 300, 60, 60, Global.BACK_TERRAIN_PRIORITY, Texture2D.FromFile(GraphicsDevice, "ProjectFiles/Assets/All.png"), [true, true, true, true]);
            new Sprite(1100, 150, 60, 60, Global.BACK_TERRAIN_PRIORITY, Texture2D.FromFile(GraphicsDevice, "ProjectFiles/Assets/AllButTop.png"), [false, true, true, true]);
            new Sprite(980, 300, 60, 60, Global.FRONT_TERRAIN_PRIORITY, testTexture, [false, false, false, false]);
            new Sprite(820, 300, 60, 60, Global.FRONT_TERRAIN_PRIORITY, Texture2D.FromFile(GraphicsDevice, "ProjectFiles/Assets/topOnly2.png"), [true, false, false, false]);

            // Create a simple square texture for the player
            _playerTexture = new Texture2D(GraphicsDevice, PlayerWidth, PlayerHeight);
            Color[] playerData = new Color[PlayerWidth * PlayerHeight];
            int tileSize = 8; // every 8 pixels we swap color
            for (int y = 0; y < PlayerHeight; y++)
            {
                for (int x = 0; x < PlayerWidth; x++)
                {
                    int tileX = x / tileSize;
                    int tileY = y / tileSize;
                    bool evenTile = ((tileX + tileY) % 2) == 0;
                    Color c = evenTile ? Color.Black : Color.Gray;
                    int index = y * PlayerWidth + x;
                    playerData[index] = c;
                }
            }
            _playerTexture.SetData(playerData);

            // Set initial player position (centered horizontally, on top of baseplate)
            player = new Player((Global.resolution[0] - PlayerWidth) / 2, baseplate.getY() - PlayerHeight, 32, 32, _playerTexture);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Global.ckbs = Keyboard.GetState();
            //GamePadState currentGamePad = GamePad.GetState(PlayerIndex.One);
            //bool backPressed = currentGamePad.Buttons.Back == ButtonState.Pressed;
            bool escapeJustPressed = Global.ckbs.IsKeyDown(Keys.Escape) && !Global.pkbs.IsKeyDown(Keys.Escape);

            if (escapeJustPressed)
                Exit();

            // Update Player Movement
            player.controls();

            // Save keyboard state for next frame (used to detect key presses)
            Global.pkbs = Global.ckbs;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White); // White background
            // If the sprite batch or textures haven't been created yet, skip drawing this frame
            if (_spriteBatch == null)
                return;

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Draw each sprite in the Global.spriteList
            foreach(Sprite sprite in Global.spriteList){
                _spriteBatch.Draw(sprite.getTexture(), sprite.getRectangle(), Color.White);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }

    static class Program
    {
        static void Main(string[] args)
        {
            using (var game = new GameEngine())
                game.Run();
        }
    }
}