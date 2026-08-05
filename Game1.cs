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
        private Texture2D _dummySprite;

        private Texture2D _bottleTexture;
        private Texture2D _cueTexture;

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
        private int _currentStrikeDamage = 0;
        private float forceX = 0f;
        private float forceY = 0f;
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
            _dummySprite = Content.Load<Texture2D>("dummy_sprite");

            _bottleTexture = Content.Load<Texture2D>("beer");
            _cueTexture = Content.Load<Texture2D>("billiards_cue");

            _player = new Player(new Vector2(640, 200));
            _dummy = new Dummy(new Vector2(900, 300));
            _level = new Level();
            _weaponManager = new WeaponManager(_bottleTexture, _cueTexture);
            _random = new Random();
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState kState = Keyboard.GetState();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || kState.IsKeyDown(Keys.Escape)) Exit();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            int screenWidth = GraphicsDevice.Viewport.Width;
            int screenHeight = GraphicsDevice.Viewport.Height;

            // --- BEWEGUNGSGESCHWINDIGKEIT & GEWICHT ---
            float weaponWeight = WeaponDatabase.Unarmed.WeightMultiplier;
            _player.IsCarryingHeavy = WeaponDatabase.Unarmed.IsHeavy;

            if (_weaponManager.HeldWeaponIndex != -1)
            {
                WeaponType heldType = _weaponManager.Weapons[_weaponManager.HeldWeaponIndex].Type;
                WeaponData data = WeaponDatabase.Stats[heldType];
                weaponWeight = data.WeightMultiplier;
                _player.IsCarryingHeavy = data.IsHeavy;
            }
            if (_player.IsCarryingDummy) weaponWeight = 0.5f;

            _player.Update(dt, kState, _oldKeyboardState, _level.Platforms, screenWidth, screenHeight, weaponWeight);

            bool projectileTriggeredShake;
            _weaponManager.Update(dt, kState, _oldKeyboardState, _player, _dummy, _level.Platforms, screenWidth, screenHeight, out projectileTriggeredShake);
            if (projectileTriggeredShake) { _shakeDuration = 0.15f; _shakeIntensity = 5f; }

            // Dummy tragen Update
            if (_player.IsCarryingDummy)
            {
                _dummy.IsCarried = true;
                float offsetOffsetX = _player.IsFacingRight ? 10 : -10;
                _dummy.Position.X = _player.Position.X + offsetOffsetX;
                _dummy.Position.Y = _player.Position.Y - 100;
                _dummy.Velocity = Vector2.Zero;
            }
            else _dummy.IsCarried = false;

            _dummy.Update(dt, screenWidth, screenHeight, _level.Platforms);

            // E: Aufheben / Fallen lassen
            if (kState.IsKeyDown(Keys.E) && _oldKeyboardState.IsKeyUp(Keys.E) && !_player.IsSlamming)
            {
                if (_weaponManager.HeldWeaponIndex == -1 && !_player.IsCarryingDummy)
                {
                    Rectangle pickupRange = new Rectangle(_player.Bounds.X - 50, _player.Bounds.Y - 20, _player.Bounds.Width + 100, _player.Bounds.Height + 40);
                    if (pickupRange.Intersects(_dummy.Bounds)) _player.IsCarryingDummy = true;
                }
                else if (_player.IsCarryingDummy)
                {
                    _player.IsCarryingDummy = false;
                    _dummy.Position = new Vector2(_player.Position.X, _player.Position.Y);
                }
            }

            // Q: Werfen
            if (kState.IsKeyDown(Keys.Q) && _oldKeyboardState.IsKeyUp(Keys.Q) && _player.IsCarryingDummy && !_player.IsSlamming)
            {
                _player.IsCarryingDummy = false;
                _dummy.Velocity = new Vector2(850f * (_player.IsFacingRight ? 1f : -1f), -400f);
                _player.TriggerThrow();
            }

            // Landung nach dem Slam
            if (_player.JustLandedSlam)
            {
                if (_player.IsCarryingDummy)
                {
                    _shakeDuration = 0.6f; _shakeIntensity = 18f;
                    _player.IsCarryingDummy = false;
                    _dummy.Health -= 40;
                    _dummy.ApplyHit(new Vector2(0f, -800f), 0.5f);
                }
                else
                {
                    Rectangle slamHitbox = new Rectangle(_player.Bounds.X - 100, _player.Bounds.Y, _player.Bounds.Width + 200, _player.Bounds.Height + 50);
                    bool hitDummy = slamHitbox.Intersects(_dummy.Bounds);

                    _shakeDuration = 0.3f;
                    _shakeIntensity = 8f;

                    if (_weaponManager.HeldWeaponIndex != -1)
                    {
                        WeaponType t = _weaponManager.Weapons[_weaponManager.HeldWeaponIndex].Type;
                        WeaponData data = WeaponDatabase.Stats[t];

                        if (data.IsHeavy)
                        {
                            _shakeDuration = (t == WeaponType.Jukebox) ? 0.6f : 0.4f;
                            _shakeIntensity = (t == WeaponType.Jukebox) ? 20f : 12f;

                            if (hitDummy)
                            {
                                _dummy.Health -= (t == WeaponType.Jukebox) ? 40 : 30;
                                _dummy.ApplyHit(new Vector2(0f, -900f), 0.4f);
                            }
                            _weaponManager.InstaBreakHeldWeapon();
                        }
                    }
                    else if (hitDummy)
                    {
                        _dummy.Health -= 20;
                        _dummy.ApplyHit(new Vector2(0f, -500f), 0.3f);
                    }
                }
            }

            _player.IsAttacking = false; _player.IsStabbing = false;

            // --- ANGRIFFS LOGIK (F-TASTE) ---
            if (kState.IsKeyDown(Keys.F) && _oldKeyboardState.IsKeyUp(Keys.F))
            {
                if (_player.IsCarryingDummy && !_player.IsGrounded) _player.IsSlamming = true;
                else if (!_player.IsCarryingDummy)
                {
                    bool strikeTriggered = false;

                    if (_weaponManager.HeldWeaponIndex != -1) // Mit Waffe
                    {
                        WeaponItem currentWeapon = _weaponManager.Weapons[_weaponManager.HeldWeaponIndex];
                        WeaponData data = WeaponDatabase.Stats[currentWeapon.Type];

                        if (!_player.IsGrounded && data.IsHeavy) _player.IsSlamming = true;
                        else if (!_isSwinging)
                        {
                            _isSwinging = true;
                            _swingTimer = 0f;

                            // Holt alle Werte direkt aus der Datenbank!
                            _currentStrikeDamage = data.StrikeDamage;
                            forceX = _player.IsFacingRight ? data.StrikeForceX : -data.StrikeForceX;
                            forceY = data.StrikeForceY;
                            _currentSwingDuration = data.SwingDuration;

                            if (currentWeapon.Type == WeaponType.Bottle) _player.IsStabbing = true;
                            else _player.IsAttacking = true;

                            strikeTriggered = true;
                        }
                    }
                    else if (!_isSwinging && !_isPunching) // Unbewaffnet (Faustschlag)
                    {
                        if (!_player.IsGrounded) _player.IsSlamming = true;
                        else
                        {
                            WeaponData unarmedData = WeaponDatabase.Unarmed;

                            _isPunching = true;
                            _swingTimer = 0f;

                            // Holt Faust-Werte aus der Datenbank
                            _currentStrikeDamage = unarmedData.StrikeDamage;
                            _currentSwingDuration = unarmedData.SwingDuration;
                            forceX = _player.IsFacingRight ? unarmedData.StrikeForceX : -unarmedData.StrikeForceX;
                            forceY = unarmedData.StrikeForceY;

                            _player.IsAttacking = true;
                            strikeTriggered = true;
                        }
                    }

                    if (strikeTriggered) _player.TriggerAttack();
                }
            }

            // --- KONTINUIERLICHE HITBOX WÄHREND DEM SCHLAG ---
            if (_isSwinging || _isPunching)
            {
                _swingTimer += dt;

                // Reichweite aus der Datenbank holen
                int reach = WeaponDatabase.Unarmed.HitboxReach;
                bool isBottle = false;

                if (_weaponManager.HeldWeaponIndex != -1)
                {
                    WeaponType heldType = _weaponManager.Weapons[_weaponManager.HeldWeaponIndex].Type;
                    reach = WeaponDatabase.Stats[heldType].HitboxReach;
                    if (heldType == WeaponType.Bottle) isBottle = true;
                }

                int hitboxX = _player.IsFacingRight ? _player.Bounds.Right : _player.Bounds.Left - reach;
                _attackHitbox = new Rectangle(hitboxX, _player.Bounds.Y + 20, reach, 50);

                if (isBottle) _player.IsStabbing = true;
                else _player.IsAttacking = true;

                // Treffer abfragen
                if (_attackHitbox.Intersects(_dummy.Bounds))
                {
                    _dummy.Health -= _currentStrikeDamage;
                    _dummy.ApplyHit(new Vector2(forceX, forceY), 0.3f);

                    _shakeDuration = 0.1f;
                    _shakeIntensity = 2f;

                    if (_weaponManager.HeldWeaponIndex != -1) _weaponManager.ForceDestroyHeldWeapon();

                    _isSwinging = false;
                    _isPunching = false;
                }

                if (_swingTimer >= _currentSwingDuration)
                {
                    _isSwinging = false;
                    _isPunching = false;
                }
            }

            // Kamera-Wackeln
            if (_shakeDuration > 0)
            {
                _shakeDuration -= dt;
                _cameraOffset.X = (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity;
                _cameraOffset.Y = (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity;
                _shakeIntensity = MathHelper.Lerp(_shakeIntensity, 0f, 6f * dt);
            }
            else _cameraOffset = Vector2.Zero;

            _oldKeyboardState = kState;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(24, 20, 36));
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _level.Draw(_spriteBatch, _pixelTexture, _cameraOffset);
            _weaponManager.Draw(_spriteBatch, _pixelTexture, _cameraOffset, _player);
            _dummy.Draw(_spriteBatch, _dummySprite, _cameraOffset);
            _player.Draw(_spriteBatch, _playerSprite, _cameraOffset);
            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}