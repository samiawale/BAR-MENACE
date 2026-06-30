using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Bar_Menace
{
    // --- WEAPON ENUMS & STRUCTURE -----
    public enum WeaponType { Bottle, BilliardCue, Barstool }
    public enum WeaponState { OnGround, Held, Thrown, Respawning }

    public struct WeaponItem
    {
        public WeaponType Type;
        public WeaponState State;
        public Vector2 Position;
        public Vector2 Velocity;
        public Rectangle BoundingBox;
        public Vector2 SpawnPoint;
        public int MaxDurability;
        public int CurrentDurability;
        public float CooldownTimer;
        public Color DebugColor;
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // --- PLAYER MOVEMENT VARIABLES ---
        Vector2 playerPosition;
        Vector2 playerVelocity;
        const float MoveSpeed = 350f;
        const float Gravity = 1400f;
        const float JumpForce = -580f;
        bool isGrounded = false;
        bool isFacingRight = true;

        // --- DOUBLE JUMP & INPUT TRACKING VARIABLES ---
        int jumpCount = 0;
        const int MaxJumps = 2;
        KeyboardState oldKeyboardState;

        // --- PLAYER RETRO BOX PLACEHOLDER ---
        Texture2D pixelTexture;
        Rectangle playerBounds;
        const int PlayerWidth = 32;
        const int PlayerHeight = 64;

        // --- PLATFORM LAYOUT VARIABLES ---
        List<Rectangle> barPlatforms;

        // --- WEAPON MANAGER VARIABLES ----
        List<WeaponItem> weaponsList;
        const float RespawnDelay = 10f;
        int heldWeaponIndex = -1;
        const float ThrowSpeedX = 800f;
        const float ThrowSpeedY = -200f;

        // --- COMBAT & GROUND SLAM VARIABLES --
        bool isSlamming = false;
        const float SlamSpeed = 1200f;

        bool isSwinging = false;
        float swingTimer = 0f;
        const float MaxSwingTime = 0.15f;
        Rectangle attackHitbox;

        // --- UNARMED PUNCH ARCHETYPE -------
        bool isPunching = false;
        Color attackBoxColor = Color.Orange;

        // --- PRACTICE AI DUMMY VARIABLES -----
        Vector2 dummyPosition;
        Vector2 dummyVelocity;
        Rectangle dummyBounds;
        bool dummyGrounded = false;
        const int DummyWidth = 32;
        const int DummyHeight = 64;
        float dummyHitStunTimer = 0f;
        bool isDummyHit = false;

        // --- VISUAL SCREEN SHAKE ---
        float shakeDuration = 0f;
        float shakeIntensity = 0f;
        Vector2 cameraOffset = Vector2.Zero;

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

            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            playerPosition = new Vector2(640, 200);

            dummyPosition = new Vector2(900, 300);
            dummyVelocity = Vector2.Zero;

            barPlatforms = new List<Rectangle>();
            barPlatforms.Add(new Rectangle(150, 500, 300, 30));
            barPlatforms.Add(new Rectangle(800, 500, 300, 30));
            barPlatforms.Add(new Rectangle(440, 350, 400, 30));
            barPlatforms.Add(new Rectangle(540, 200, 200, 25));

            weaponsList = new List<WeaponItem>();
            CreateWeapon(WeaponType.Bottle, new Vector2(280, 476), 1, Color.LimeGreen);
            CreateWeapon(WeaponType.BilliardCue, new Vector2(940, 484), 3, Color.Tan);
            CreateWeapon(WeaponType.Barstool, new Vector2(620, 318), 5, Color.OrangeRed);
        }

        private void CreateWeapon(WeaponType type, Vector2 pos, int durability, Color debugColor)
        {
            WeaponItem weapon = new WeaponItem();
            weapon.Type = type;
            weapon.State = WeaponState.OnGround;
            weapon.Position = pos;
            weapon.Velocity = Vector2.Zero;
            weapon.SpawnPoint = pos;
            weapon.MaxDurability = durability;
            weapon.CurrentDurability = durability;
            weapon.CooldownTimer = 0f;
            weapon.DebugColor = debugColor;

            int w = 24, h = 24;
            if (type == WeaponType.BilliardCue) { w = 40; h = 16; }
            if (type == WeaponType.Barstool) { w = 32; h = 32; }

            weapon.BoundingBox = new Rectangle((int)pos.X, (int)pos.Y, w, h);
            weaponsList.Add(weapon);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kState = Keyboard.GetState();

            // Movement Controls
            playerVelocity.X = 0;

            if (!isSlamming)
            {
                if (kState.IsKeyDown(Keys.A))
                {
                    playerVelocity.X = -MoveSpeed;
                    isFacingRight = false;
                }
                if (kState.IsKeyDown(Keys.D))
                {
                    playerVelocity.X = MoveSpeed;
                    isFacingRight = true;
                }
            }

            // Gravity Engine
            if (!isGrounded)
            {
                if (isSlamming) { playerVelocity.Y = SlamSpeed; }
                else { playerVelocity.Y += Gravity * dt; }
            }
            else
            {
                // LANDING IMPACT CHECK (Slam Finisher)
                if (isSlamming)
                {
                    isSlamming = false;
                    shakeDuration = 0.4f;
                    shakeIntensity = 12f;

                    if (Vector2.Distance(playerPosition, dummyPosition) < 200f)
                    {
                        isDummyHit = true;
                        dummyHitStunTimer = 0.25f;
                        dummyVelocity = new Vector2(0f, -700f);
                        dummyGrounded = false;
                    }

                    if (heldWeaponIndex != -1)
                    {
                        WeaponItem sw = weaponsList[heldWeaponIndex];
                        sw.CurrentDurability = 0;
                        sw.State = WeaponState.Respawning;
                        sw.CooldownTimer = 0f;
                        sw.Velocity = Vector2.Zero; // CRITICAL RESET

                        sw.Position = new Vector2(-500, -500);
                        sw.BoundingBox.X = -500;
                        sw.BoundingBox.Y = -500;

                        weaponsList[heldWeaponIndex] = sw;
                        heldWeaponIndex = -1;
                    }
                }

                playerVelocity.Y = 0;
                jumpCount = 0;
            }

            // Jump Engine
            if (kState.IsKeyDown(Keys.Space) && oldKeyboardState.IsKeyUp(Keys.Space) && !isSlamming)
            {
                if (isGrounded || jumpCount < MaxJumps)
                {
                    playerVelocity.Y = JumpForce;
                    jumpCount++;
                    isGrounded = false;
                }
            }

            playerPosition += playerVelocity * dt;
            isGrounded = false;

            // Screen Floor Bounding (Player)
            float floorY = GraphicsDevice.Viewport.Height - PlayerHeight;
            if (playerPosition.Y >= floorY)
            {
                playerPosition.Y = floorY;
                isGrounded = true;
            }

            // Platform Collisions Check (Player)
            Rectangle upcomingBounds = new Rectangle((int)playerPosition.X, (int)playerPosition.Y, PlayerWidth, PlayerHeight);
            foreach (Rectangle platform in barPlatforms)
            {
                if (upcomingBounds.Intersects(platform))
                {
                    if (!isSlamming && playerVelocity.Y > 0 && (upcomingBounds.Bottom - playerVelocity.Y * dt) <= platform.Top + 12)
                    {
                        playerPosition.Y = platform.Top - PlayerHeight;
                        isGrounded = true;
                        playerVelocity.Y = 0;
                    }
                }
            }
            playerBounds = new Rectangle((int)playerPosition.X, (int)playerPosition.Y, PlayerWidth, PlayerHeight);

            // PRACTICE DUMMY PHYSICS ENGINE
            if (dummyGrounded) { dummyVelocity.X = MathHelper.Lerp(dummyVelocity.X, 0f, 8f * dt); }
            else { dummyVelocity.X = MathHelper.Lerp(dummyVelocity.X, 0f, 2f * dt); }

            if (!dummyGrounded) { dummyVelocity.Y += Gravity * dt; }
            else { dummyVelocity.Y = 0; }

            dummyPosition += dummyVelocity * dt;
            dummyGrounded = false;

            float dummyFloorY = GraphicsDevice.Viewport.Height - DummyHeight;
            if (dummyPosition.Y >= dummyFloorY)
            {
                dummyPosition.Y = dummyFloorY;
                dummyGrounded = true;
            }

            Rectangle upcomingDummyBounds = new Rectangle((int)dummyPosition.X, (int)dummyPosition.Y, DummyWidth, DummyHeight);
            foreach (Rectangle platform in barPlatforms)
            {
                if (upcomingDummyBounds.Intersects(platform))
                {
                    if (dummyVelocity.Y > 0 && (upcomingDummyBounds.Bottom - dummyVelocity.Y * dt) <= platform.Top + 12)
                    {
                        dummyPosition.Y = platform.Top - DummyHeight;
                        dummyGrounded = true;
                        dummyVelocity.Y = 0;
                    }
                }
            }
            dummyBounds = new Rectangle((int)dummyPosition.X, (int)dummyPosition.Y, DummyWidth, DummyHeight);

            if (isDummyHit)
            {
                dummyHitStunTimer -= dt;
                if (dummyHitStunTimer <= 0) { isDummyHit = false; }
            }

            // COMBAT ATTACK LOGIC (F) - WITH EXPLICIT ACCELERATION RESETS
            if (kState.IsKeyDown(Keys.F) && oldKeyboardState.IsKeyUp(Keys.F))
            {
                bool strikeTriggered = false;
                float forceX = 0f, forceY = 0f;

                if (heldWeaponIndex != -1)
                {
                    WeaponItem currentWeapon = weaponsList[heldWeaponIndex];

                    if (!isGrounded && currentWeapon.Type == WeaponType.Barstool)
                    {
                        isSlamming = true;
                    }
                    else if (!isSwinging)
                    {
                        isSwinging = true;
                        isPunching = false;
                        swingTimer = 0f;
                        attackBoxColor = Color.Orange * 0.6f;

                        int swingReach = 50;
                        int attackX = isFacingRight ? playerBounds.Right : playerBounds.Left - swingReach;
                        attackHitbox = new Rectangle(attackX, playerBounds.Y + 15, swingReach, 30);

                        strikeTriggered = true;
                        forceX = isFacingRight ? 550f : -550f;
                        forceY = -150f;

                        currentWeapon.CurrentDurability--;

                        if (currentWeapon.CurrentDurability <= 0)
                        {
                            currentWeapon.State = WeaponState.Respawning;
                            currentWeapon.CooldownTimer = 0f;
                            currentWeapon.Velocity = Vector2.Zero; // CRITICAL FIX: Stops invisible descent bug

                            currentWeapon.Position = new Vector2(-500, -500);
                            currentWeapon.BoundingBox.X = -500;
                            currentWeapon.BoundingBox.Y = -500;

                            weaponsList[heldWeaponIndex] = currentWeapon;
                            heldWeaponIndex = -1;
                        }
                        else
                        {
                            weaponsList[heldWeaponIndex] = currentWeapon;
                        }
                    }
                }
                else if (!isSwinging && !isPunching)
                {
                    isPunching = true;
                    swingTimer = 0f;
                    attackBoxColor = Color.SkyBlue * 0.7f;

                    int punchReach = 30;
                    int attackX = isFacingRight ? playerBounds.Right : playerBounds.Left - punchReach;
                    attackHitbox = new Rectangle(attackX, playerBounds.Y + 20, punchReach, 20);

                    strikeTriggered = true;
                    forceX = isFacingRight ? 350f : -350f;
                    forceY = -100f;
                }

                if (strikeTriggered && attackHitbox.Intersects(dummyBounds))
                {
                    isDummyHit = true;
                    dummyHitStunTimer = 0.15f;
                    dummyVelocity = new Vector2(forceX, forceY);
                    dummyGrounded = false;
                }
            }

            // Handle swing animations timers
            if (isSwinging || isPunching)
            {
                swingTimer += dt;
                int currentReach = isPunching ? 30 : 50;
                int attackX = isFacingRight ? playerBounds.Right : playerBounds.Left - currentReach;
                attackHitbox.X = attackX;
                attackHitbox.Y = playerBounds.Y + 20;

                float frameDuration = isPunching ? 0.10f : MaxSwingTime;
                if (swingTimer >= frameDuration)
                {
                    isSwinging = false;
                    isPunching = false;
                }
            }

            // INTERACTION LOGIC (E KEY)
            if (kState.IsKeyDown(Keys.E) && oldKeyboardState.IsKeyUp(Keys.E) && !isSlamming)
            {
                if (heldWeaponIndex == -1)
                {
                    for (int i = 0; i < weaponsList.Count; i++)
                    {
                        if (weaponsList[i].State == WeaponState.OnGround && playerBounds.Intersects(weaponsList[i].BoundingBox))
                        {
                            WeaponItem pickedWeapon = weaponsList[i];
                            pickedWeapon.State = WeaponState.Held;
                            weaponsList[i] = pickedWeapon;
                            heldWeaponIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    WeaponItem droppedWeapon = weaponsList[heldWeaponIndex];
                    droppedWeapon.State = WeaponState.OnGround;
                    droppedWeapon.Velocity = Vector2.Zero;
                    droppedWeapon.Position = new Vector2(playerPosition.X, playerPosition.Y + (PlayerHeight - droppedWeapon.BoundingBox.Height));
                    droppedWeapon.BoundingBox.X = (int)droppedWeapon.Position.X;
                    droppedWeapon.BoundingBox.Y = (int)droppedWeapon.Position.Y;
                    weaponsList[heldWeaponIndex] = droppedWeapon;
                    heldWeaponIndex = -1;
                }
            }

            // THROW ENGINE (Q KEY)
            if (kState.IsKeyDown(Keys.Q) && oldKeyboardState.IsKeyUp(Keys.Q) && heldWeaponIndex != -1 && !isSlamming)
            {
                WeaponItem thrownWeapon = weaponsList[heldWeaponIndex];
                thrownWeapon.State = WeaponState.Thrown;
                float directionMultiplier = isFacingRight ? 1f : -1f;
                thrownWeapon.Velocity = new Vector2(ThrowSpeedX * directionMultiplier, ThrowSpeedY);
                weaponsList[heldWeaponIndex] = thrownWeapon;
                heldWeaponIndex = -1;
            }

            // LOCK WEAPON TO HANDS POSITION
            if (heldWeaponIndex != -1 && !isSlamming)
            {
                WeaponItem heldW = weaponsList[heldWeaponIndex];
                float offsetOffsetX = isFacingRight ? 12 : -12;
                heldW.Position.X = playerPosition.X + (PlayerWidth / 2) - (heldW.BoundingBox.Width / 2) + offsetOffsetX;
                heldW.Position.Y = playerPosition.Y + (PlayerHeight / 2) - (heldW.BoundingBox.Height / 2);
                heldW.BoundingBox.X = (int)heldW.Position.X;
                heldW.BoundingBox.Y = (int)heldW.Position.Y;
                weaponsList[heldWeaponIndex] = heldW;
            }

            // PROJECTILE & COOLDOWN RESPAWNS MANAGEMENT LOOP
            for (int i = 0; i < weaponsList.Count; i++)
            {
                WeaponItem w = weaponsList[i];

                if (w.State == WeaponState.Thrown)
                {
                    w.Velocity.Y += Gravity * dt;
                    w.Position += w.Velocity * dt;
                    w.BoundingBox.X = (int)w.Position.X;
                    w.BoundingBox.Y = (int)w.Position.Y;

                    bool hitFloor = w.Position.Y >= (GraphicsDevice.Viewport.Height - w.BoundingBox.Height);
                    bool wentOffScreen = w.Position.X < -100 || w.Position.X > GraphicsDevice.Viewport.Width + 100;
                    bool hitDummy = w.BoundingBox.Intersects(dummyBounds);

                    if (hitFloor || wentOffScreen || hitDummy)
                    {
                        if (hitDummy)
                        {
                            isDummyHit = true;
                            dummyHitStunTimer = 0.2f;
                            float launchDirX = w.Velocity.X > 0 ? 650f : -650f;
                            dummyVelocity = new Vector2(launchDirX, -250f);
                            dummyGrounded = false;
                        }

                        w.CurrentDurability = 0;
                        w.State = WeaponState.Respawning;
                        w.CooldownTimer = 0f;
                        w.Velocity = Vector2.Zero; // CLEAR PROJECTILE VELOCITY

                        w.Position = new Vector2(-500, -500);
                        w.BoundingBox.X = -500;
                        w.BoundingBox.Y = -500;

                        if (hitFloor || hitDummy) { shakeDuration = 0.15f; shakeIntensity = 5f; }
                    }
                    weaponsList[i] = w;
                }
                else if (w.State == WeaponState.Respawning)
                {
                    w.CooldownTimer += dt;
                    if (w.CooldownTimer >= RespawnDelay)
                    {
                        w.State = WeaponState.OnGround;
                        w.CurrentDurability = w.MaxDurability;
                        w.Position = w.SpawnPoint;
                        w.Velocity = Vector2.Zero;
                        w.CooldownTimer = 0f;
                        w.BoundingBox.X = (int)w.SpawnPoint.X;
                        w.BoundingBox.Y = (int)w.SpawnPoint.Y;
                    }
                    weaponsList[i] = w;
                }
            }

            // SCREEN SHAKE ENGINE
            if (shakeDuration > 0)
            {
                shakeDuration -= dt;
                float shakeX = (float)(Math.Sin(gameTime.TotalGameTime.TotalMilliseconds * 1.2) * shakeIntensity);
                float shakeY = (float)(Math.Cos(gameTime.TotalGameTime.TotalMilliseconds * 1.5) * shakeIntensity);
                cameraOffset = new Vector2(shakeX, shakeY);
                shakeIntensity = MathHelper.Lerp(shakeIntensity, 0f, 6f * dt);
            }
            else
            {
                cameraOffset = Vector2.Zero;
            }

            oldKeyboardState = kState;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(24, 20, 36));

            _spriteBatch.Begin();

            // Draw Platforms
            foreach (Rectangle platform in barPlatforms)
            {
                Rectangle renderRect = platform;
                renderRect.X += (int)cameraOffset.X;
                renderRect.Y += (int)cameraOffset.Y;
                _spriteBatch.Draw(pixelTexture, renderRect, new Color(139, 69, 19));
            }

            // Draw Weapons
            foreach (WeaponItem weapon in weaponsList)
            {
                if (weapon.State == WeaponState.OnGround || weapon.State == WeaponState.Held || weapon.State == WeaponState.Thrown)
                {
                    Rectangle renderRect = weapon.BoundingBox;
                    renderRect.X += (int)cameraOffset.X;
                    renderRect.Y += (int)cameraOffset.Y;
                    _spriteBatch.Draw(pixelTexture, renderRect, weapon.DebugColor);
                }
            }

            // Draw Strike Flash Hitbox
            if (isSwinging || isPunching)
            {
                Rectangle renderAttack = attackHitbox;
                renderAttack.X += (int)cameraOffset.X;
                renderAttack.Y += (int)cameraOffset.Y;
                _spriteBatch.Draw(pixelTexture, renderAttack, attackBoxColor);
            }

            // Draw Practice Dummy
            Rectangle dummyRenderBounds = dummyBounds;
            dummyRenderBounds.X += (int)cameraOffset.X;
            dummyRenderBounds.Y += (int)cameraOffset.Y;
            Color dummyColor = isDummyHit ? Color.White : new Color(112, 128, 144);
            _spriteBatch.Draw(pixelTexture, dummyRenderBounds, dummyColor);

            // Draw Player
            Rectangle playerRenderBounds = playerBounds;
            playerRenderBounds.X += (int)cameraOffset.X;
            playerRenderBounds.Y += (int)cameraOffset.Y;
            Color playerColor = isSlamming ? Color.Gold : Color.Crimson;
            _spriteBatch.Draw(pixelTexture, playerRenderBounds, playerColor);

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}

//zusammen Coded von Sami und Joud 
//Joud behandelte die Bewegungsmechanik und Sami und Joud den Rest Allgemmein

//Hilfe von KI war definitiv gebraucht :D 