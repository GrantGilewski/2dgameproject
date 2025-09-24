using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;

namespace SoulfulJourney
{
    public class GameEngine : Game
    {
    private GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;

    private Texture2D? _playerTexture;        // Player variables
    private Vector2 _playerPosition;
    private float _playerSpeed = 200f;
    private Rectangle _baseplateRect;      // Baseplate variables
    private Texture2D? _baseplateTexture;
    private Texture2D? _platformTexture;     // Additional platforms
    private List<Rectangle> _platforms = new List<Rectangle>();
        private bool _isJumping = false;            // Jump variables
        private float _jumpVelocity = 0f;
        private float _gravity = 600f;
        private float _jumpStrength = 400f;
        private const int PlayerWidth = 32;     // Player dimensions
        private const int PlayerHeight = 32;
        private KeyboardState _previousKeyboardState;   // To track previous keyboard state for input detection

        public GameEngine()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
        }

        protected override void Initialize()
        {
            _previousKeyboardState = Keyboard.GetState();
            base.Initialize();
        }
        
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

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
            // Compute ground Y based on baseplate and player height and place player on it
            _baseplateRect = new Rectangle(0, _graphics.PreferredBackBufferHeight - 32, _graphics.PreferredBackBufferWidth, 32);
            float groundY = _baseplateRect.Top - PlayerHeight;
            _playerPosition = new Vector2((_graphics.PreferredBackBufferWidth - PlayerWidth) / 2, groundY);

            // Create baseplate texture (1x1 pixel, colored DarkGray)
            _baseplateTexture = new Texture2D(GraphicsDevice, 1, 1);
            _baseplateTexture.SetData(new[] { Color.DarkGray });

            // Create platform texture (1x1 pixel)
            _platformTexture = new Texture2D(GraphicsDevice, 1, 1);
            _platformTexture.SetData(new[] { Color.SaddleBrown });

            // Add a couple of small platforms above the baseplate
            // Platform width, height and positions are chosen relative to screen size
            int platW = 120;
            int platH = 12;
            int centerX = _graphics.PreferredBackBufferWidth / 2;
            int baseY = _baseplateRect.Top;

            _platforms.Add(new Rectangle(centerX - 180, baseY - 120, platW, platH));
            _platforms.Add(new Rectangle(centerX + 60, baseY - 200, platW, platH));
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            KeyboardState currentKeyboard = Keyboard.GetState();
            //GamePadState currentGamePad = GamePad.GetState(PlayerIndex.One);
            //bool backPressed = currentGamePad.Buttons.Back == ButtonState.Pressed;
            bool escapeJustPressed = currentKeyboard.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape);

            if (escapeJustPressed)
                Exit();
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Move player left
            if (currentKeyboard.IsKeyDown(Keys.Left) || currentKeyboard.IsKeyDown(Keys.A))
            {
                _playerPosition.X -= _playerSpeed * delta;
            }
            // Move player right
            if (currentKeyboard.IsKeyDown(Keys.Right) || currentKeyboard.IsKeyDown(Keys.D))
            {
                _playerPosition.X += _playerSpeed * delta;
            }

            // Clamp player within screen bounds
            _playerPosition.X = MathHelper.Clamp(_playerPosition.X, 0, _graphics.PreferredBackBufferWidth - 32);

            // Determine whether player is currently standing on the baseplate or any platform
            float groundYBase = _baseplateRect.Top - PlayerHeight;
            bool onBaseplate = (Math.Abs(_playerPosition.Y - groundYBase) < 0.5f)
                                && (_playerPosition.X + PlayerWidth > _baseplateRect.Left && _playerPosition.X < _baseplateRect.Right);

            bool onPlatform = false;
            foreach (var plat in _platforms)
            {
                float platTop = plat.Top - PlayerHeight;
                if ((Math.Abs(_playerPosition.Y - platTop) < 0.5f) && (_playerPosition.X + PlayerWidth > plat.Left && _playerPosition.X < plat.Right))
                {
                    onPlatform = true;
                    break;
                }
            }

            bool isGrounded = onBaseplate || onPlatform;

            // If player walked off a surface (was grounded but now not), start falling
            if (!isGrounded && !_isJumping)
            {
                _isJumping = true;
                _jumpVelocity = 0f; // start falling from rest
            }

            // Jump logic: detect a key-press (not just held key); only allow jump when grounded
            bool jumpKeyPressed = (currentKeyboard.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space))
                                  || (currentKeyboard.IsKeyDown(Keys.W) && !_previousKeyboardState.IsKeyDown(Keys.W))
                                  || (currentKeyboard.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up));

            if (!_isJumping && isGrounded && jumpKeyPressed)
            {
                _isJumping = true;
                _jumpVelocity = -_jumpStrength;
            }

            if (_isJumping)
            {
                // Apply vertical movement using current velocity, then integrate gravity
                float movement = _jumpVelocity * delta;
                _playerPosition.Y += movement;
                _jumpVelocity += _gravity * delta;

                // Check for landing on any platform (only when moving downward)
                if (movement > 0f)
                {
                    float prevY = _playerPosition.Y - movement;
                    foreach (var plat in _platforms)
                    {
                        float platTop = plat.Top - PlayerHeight; // y at which player's Y should land
                        bool horizontallyOverlapping = _playerPosition.X + PlayerWidth > plat.Left && _playerPosition.X < plat.Right;
                        if (prevY < platTop && _playerPosition.Y >= platTop && horizontallyOverlapping)
                        {
                            _playerPosition.Y = platTop;
                            _isJumping = false;
                            _jumpVelocity = 0f;
                            break;
                        }
                    }
                }

                // Check for landing on the baseplate
                float groundY = _baseplateRect.Top - PlayerHeight; // baseplate Y - player height
                if (_playerPosition.Y >= groundY)
                {
                    _playerPosition.Y = groundY;
                    _isJumping = false;
                    _jumpVelocity = 0f;
                }
            }

            // Save keyboard state for next frame (used to detect key presses)
            _previousKeyboardState = currentKeyboard;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White); // White background
            // If the sprite batch or textures haven't been created yet, skip drawing this frame
            if (_spriteBatch == null)
                return;

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Draw baseplate (scale the 1x1 texture to the full rectangle)
            if (_baseplateTexture != null)
                _spriteBatch.Draw(_baseplateTexture, _baseplateRect, Color.White);

            // Draw platforms
            if (_platformTexture != null)
            {
                foreach (var plat in _platforms)
                {
                    _spriteBatch.Draw(_platformTexture, plat, Color.Black);
                }
            }

            // Draw player using a destination rectangle for precise positioning/size
            if (_playerTexture != null)
            {
                var playerDest = new Rectangle((int)_playerPosition.X, (int)_playerPosition.Y, PlayerWidth, PlayerHeight);
                _spriteBatch.Draw(_playerTexture, playerDest, Color.White);
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