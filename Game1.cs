using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Bar_Menace.Entities;
using Bar_Menace.Environment;

namespace Bar_Menace
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _pixelTexture;
        private Texture2D _playerSprite;

        private Player _player;
        private Dummy _dummy;
        private Level _level;
        private WeaponManager _weaponManager;

        private KeyboardState _oldKeyboardState;
        private Random _random;

        private bool _isSwinging = false;
        private bool _isPunching = false;
        private float _swingTimer = 0f;

        private float _currentSwingDuration = 0.15f;
        private int _currentSwingReach = 50;
        private int _currentStrikeDamage = 0;

        private Rectangle _attackHitbox;

        private float _shakeDuration = 0f;
        private float _shakeIntensity = 0f;
        private Vector2 _cameraOffset = Vector2.Zero;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
            _playerSprite = Content.Load<Texture2D>("player_sprite");

            _player = new Player(new Vector2(640, 200));
            _dummy = new Dummy(new Vector2(900, 300));
            _level = new Level();
            _weaponManager = new WeaponManager();

            _random = new Random();
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState kState = Keyboard.GetState();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || kState.IsKeyDown(Keys.Escape))
                Exit();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            int screenWidth = GraphicsDevice.Viewport.Width;
            int screenHeight = GraphicsDevice.Viewport.Height;

            float weaponWeight = 1.0f;
            _player.IsCarryingHeavy = false;

            if (_weaponManager.HeldWeaponIndex != -1)
            {
                WeaponType heldType = _weaponManager.Weapons[_weaponManager.HeldWeaponIndex].Type;
                if (heldType == WeaponType.BilliardCue) weaponWeight = 0.85f;
                else if (heldType == WeaponType.Barstool) { weaponWeight = 0.65f; _player.IsCarryingHeavy = true; }
                else if (heldType == WeaponType.Jukebox) { weaponWeight = 0.45f; _player.IsCarryingHeavy = true; }
            }
            if (_player.IsCarryingDummy) weaponWeight = 0.5f;

            _player.Update(dt, kState, _oldKeyboardState, _level.Platforms, screenWidth, screenHeight, weaponWeight);

            bool projectileTriggeredShake;
            _weaponManager.Update(dt, kState, _oldKeyboardState, _player, _dummy, _level.Platforms, screenWidth, screenHeight, out projectileTriggeredShake);
            if (projectileTriggeredShake) { _shakeDuration = 0.15f; _shakeIntensity = 5f; }

            _dummy.Update(dt, screenWidth, screenHeight, _level.Platforms);

            if (kState.IsKeyDown(Keys.E) && _oldKeyboardState.IsKeyUp(Keys.E) && !_player.IsSlamming)
            {
                if (_weaponManager.HeldWeaponIndex == -1 && !_player.IsCarryingDummy)
                {
                    Rectangle pickupRange = new Rectangle(_player.Bounds.X - 50, _player.Bounds.Y - 20, _player.Bounds.Width + 100, _player.Bounds.Height + 40);
                    if (pickupRange.Intersects(_dummy.Bounds))
                    {
                        _player.IsCarryingDummy = true;
                    }
                }
                else if (_player.IsCarryingDummy)
                {
                    _player.IsCarryingDummy = false;
                    _dummy.Position = new Vector2(_player.Position.X, _player.Position.Y);
                }
            }

            if (kState.IsKeyDown(Keys.Q) && _oldKeyboardState.IsKeyUp(Keys.Q) && _player.IsCarryingDummy && !_player.IsSlamming)
            {
                _player.IsCarryingDummy = false;
                float directionMultiplier = _player.IsFacingRight ? 1f : -1f;
                _dummy.Velocity = new Vector2(850f * directionMultiplier, -400f);
                _player.TriggerThrow();
            }

            if (_player.IsCarryingDummy)
            {
                float offsetOffsetX = _player.IsFacingRight ? 30 : -30;
                _dummy.Position.X = _player.Position.X + offsetOffsetX;
                _dummy.Position.Y = _player.Position.Y - 40;
                _dummy.Velocity = Vector2.Zero;
            }

            // HIER IST DER MAGISCHE ORT: Der Body Slam Treffer!
            if (_player.JustLandedSlam)
            {
                if (_player.IsCarryingDummy)
                {
                    _shakeDuration = 0.6f;
                    _shakeIntensity = 18f;
                    _player.IsCarryingDummy = false;
                    _dummy.Health -= 40;
                    _dummy.ApplyHit(new Vector2(0f, -800f), 0.5f);
                }
                else
                {
                    Rectangle slamHitbox = new Rectangle(_player.Bounds.X - 100, _player.Bounds.Y, _player.Bounds.Width + 200, _player.Bounds.Height + 50);

                    bool hasBarstool = false;
                    bool hasJukebox = false;
                    if (_weaponManager.HeldWeaponIndex != -1)
                    {
                        WeaponType t = _weaponManager.Weapons[_weaponManager.HeldWeaponIndex].Type;
                        if (t == WeaponType.Barstool) hasBarstool = true;
                        if (t == WeaponType.Jukebox) hasJukebox = true;
                    }

                    if (hasJukebox)
                    {
                        _shakeDuration = 0.6f;
                        _shakeIntensity = 20f;
                        if (slamHitbox.Intersects(_dummy.Bounds))
                        {
                            _dummy.Health -= 40;
                            _dummy.ApplyHit(new Vector2(0f, -900f), 0.4f);
                        }
                        // NEU: Egal wie neu die Jukebox war, sie zerbricht JETZT!
                        _weaponManager.InstaBreakHeldWeapon();
                    }
                    else if (hasBarstool)
                    {
                        _shakeDuration = 0.4f;
                        _shakeIntensity = 12f;
                        if (slamHitbox.Intersects(_dummy.Bounds))
                        {
                            _dummy.Health -= 30;
                            _dummy.ApplyHit(new Vector2(0f, -700f), 0.25f);
                        }
                        // NEU: Auch der Barhocker zerspringt SOFORT in tausend Teile!
                        _weaponManager.InstaBreakHeldWeapon();
                    }
                    else
                    {
                        _shakeDuration = 0.25f;
                        _shakeIntensity = 6f;
                        if (slamHitbox.Intersects(_dummy.Bounds))
                        {
                            _dummy.Health -= 15;
                            _dummy.ApplyHit(new Vector2(0f, -400f), 0.2f);
                        }
                    }
                }
            }

            _player.IsAttacking = false;
            _player.IsStabbing = false;

            if (kState.IsKeyDown(Keys.F) && _oldKeyboardState.IsKeyUp(Keys.F))
            {
                if (_player.IsCarryingDummy && !_player.IsGrounded)
                {
                    _player.IsSlamming = true;
                }
                else if (!_player.IsCarryingDummy)
                {
                    bool strikeTriggered = false;
                    float forceX = 0f, forceY = 0f;

                    if (_weaponManager.HeldWeaponIndex != -1)
                    {
                        WeaponItem currentWeapon = _weaponManager.Weapons[_weaponManager.HeldWeaponIndex];

                        if (!_player.IsGrounded && (currentWeapon.Type == WeaponType.Barstool || currentWeapon.Type == WeaponType.Jukebox))
                        {
                            _player.IsSlamming = true;
                        }
                        else if (!_isSwinging && (currentWeapon.Type == WeaponType.Bottle || currentWeapon.Type == WeaponType.BilliardCue || currentWeapon.Type == WeaponType.Barstool))
                        {
                            _isSwinging = true;
                            _isPunching = false;
                            _swingTimer = 0f;

                            if (currentWeapon.Type == WeaponType.Bottle)
                            {
                                _currentStrikeDamage = 10;
                                _currentSwingReach = 60;
                                _currentSwingDuration = 0.10f;
                                forceX = _player.IsFacingRight ? 450f : -450f;
                                forceY = -120f;
                                _player.IsStabbing = true;
                            }
                            else if (currentWeapon.Type == WeaponType.BilliardCue)
                            {
                                _currentStrikeDamage = 15;
                                _currentSwingReach = 90;
                                _currentSwingDuration = 0.25f;
                                forceX = _player.IsFacingRight ? 600f : -600f;
                                forceY = -150f;
                                _player.IsAttacking = true;
                            }
                            else if (currentWeapon.Type == WeaponType.Barstool)
                            {
                                _currentStrikeDamage = 20;
                                _currentSwingReach = 70;
                                _currentSwingDuration = 0.35f;
                                forceX = _player.IsFacingRight ? 750f : -750f;
                                forceY = -200f;
                                _player.IsAttacking = true;
                            }

                            int attackX = _player.IsFacingRight ? _player.Bounds.Right : _player.Bounds.Left - _currentSwingReach;
                            _attackHitbox = new Rectangle(attackX, _player.Bounds.Y + 30, _currentSwingReach, 50);
                            strikeTriggered = true;

                            if (_attackHitbox.Intersects(_dummy.Bounds))
                            {
                                _weaponManager.ForceDestroyHeldWeapon(); // Normale Schläge ziehen nur 1 Haltbarkeit ab
                            }
                        }
                    }
                    else if (!_isSwinging && !_isPunching)
                    {
                        if (!_player.IsGrounded)
                        {
                            _player.IsSlamming = true;
                        }
                        else
                        {
                            _isPunching = true;
                            _swingTimer = 0f;
                            _currentStrikeDamage = 5;
                            _currentSwingReach = 45;
                            _currentSwingDuration = 0.15f;

                            int attackX = _player.IsFacingRight ? _player.Bounds.Right : _player.Bounds.Left - _currentSwingReach;
                            _attackHitbox = new Rectangle(attackX, _player.Bounds.Y + 40, _currentSwingReach, 40);

                            strikeTriggered = true;
                            forceX = _player.IsFacingRight ? 350f : -350f;
                            forceY = -100f;

                            _player.IsAttacking = true;
                        }
                    }

                    if (strikeTriggered && _attackHitbox.Intersects(_dummy.Bounds))
                    {
                        _dummy.Health -= _currentStrikeDamage;
                        float stunTime = _currentSwingDuration + 0.05f;
                        _dummy.ApplyHit(new Vector2(forceX, forceY), stunTime);

                        _shakeDuration = 0.1f;
                        _shakeIntensity = (_currentStrikeDamage / 5f) + 1f;
                    }
                }
            }

            if (_isSwinging || _isPunching)
            {
                _swingTimer += dt;
                if (_currentStrikeDamage == 10) _player.IsStabbing = true;
                else _player.IsAttacking = true;

                if (_swingTimer >= _currentSwingDuration)
                {
                    _isSwinging = false;
                    _isPunching = false;
                }
            }

            if (_shakeDuration > 0)
            {
                _shakeDuration -= dt;
                _cameraOffset.X = (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity;
                _cameraOffset.Y = (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity;
                _shakeIntensity = MathHelper.Lerp(_shakeIntensity, 0f, 6f * dt);
            }
            else
            {
                _cameraOffset = Vector2.Zero;
            }

            _oldKeyboardState = kState;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(24, 20, 36));

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            _level.Draw(_spriteBatch, _pixelTexture, _cameraOffset);
            _weaponManager.Draw(_spriteBatch, _pixelTexture, _cameraOffset);
            _dummy.Draw(_spriteBatch, _pixelTexture, _cameraOffset);
            _player.Draw(_spriteBatch, _playerSprite, _cameraOffset);

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}