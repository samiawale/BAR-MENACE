using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Bar_Menace.Entities
{
    public class Player
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Rectangle Bounds { get; private set; }

        public bool IsGrounded = false, IsFacingRight = true, IsSlamming = false;
        public bool IsCarryingDummy = false, IsAttacking = false, IsStabbing = false;
        public bool IsHurt = false, IsKnockedDown = false, IsPickingUp = false;
        public bool IsCarryingHeavy = false;

        public bool JustLandedSlam = false;
        public int SlamPhase = 0;
        public float SlamTimer = 0f;
        public float ThrowTimer = 0f;

        private int jumpCount = 0;
        private const int MaxJumps = 2;
        private const float JumpForce = -580f;
        public const float SlamSpeed = 900f;
        private const float Gravity = 1400f;
        private const float BaseMoveSpeed = 350f;

        public const int SpriteWidth = 32, SpriteHeight = 64;
        public const int DrawScale = 2;
        public const int Width = SpriteWidth * DrawScale;
        public const int Height = SpriteHeight * DrawScale;

        private int currentFrame = 0;
        private float frameTimer = 0f;
        private const float FrameInterval = 0.15f;
        public float KnockbackTimer = 0f;

        public Player(Vector2 startPosition)
        {
            Position = startPosition;
            UpdateBounds();
        }

        private void UpdateBounds() => Bounds = new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        public void TriggerThrow()
        {
            ThrowTimer = 0.35f;
            currentFrame = 0;
            frameTimer = 0f;
        }

        public void TriggerAttack()
        {
            currentFrame = 0;
            frameTimer = 0f;
        }

        // NEU: KeyboardStates wurden aus den Parametern entfernt!
        public void Update(float dt, List<Rectangle> platforms, int screenWidth, int screenHeight, float weaponWeight)
        {
            JustLandedSlam = false;

            if (ThrowTimer > 0) ThrowTimer -= dt;

            if (IsSlamming && SlamPhase == 0)
            {
                SlamPhase = 1;
                SlamTimer = 0.40f;
            }

            if (SlamPhase == 1)
            {
                Velocity.Y = 0;
                SlamTimer -= dt;
                if (SlamTimer <= 0) SlamPhase = 2;
            }
            else if (SlamPhase == 3)
            {
                Velocity = Vector2.Zero;
                SlamTimer -= dt;
                if (SlamTimer <= 0)
                {
                    IsSlamming = false;
                    SlamPhase = 0;
                }
            }

            if (KnockbackTimer > 0)
            {
                KnockbackTimer -= dt;
                if (IsGrounded) Velocity.X = MathHelper.Lerp(Velocity.X, 0f, 8f * dt);
                else Velocity.X = MathHelper.Lerp(Velocity.X, 0f, 2f * dt);
            }
            else if (!IsKnockedDown && !IsHurt && !IsPickingUp)
            {
                if (!IsSlamming || SlamPhase == 2)
                {
                    float speed = BaseMoveSpeed * weaponWeight;
                    Velocity.X = 0;

                    // NEU: InputManager benutzt!
                    if (InputManager.IsHeld(Keys.A)) { Velocity.X = -speed; IsFacingRight = false; }
                    if (InputManager.IsHeld(Keys.D)) { Velocity.X = speed; IsFacingRight = true; }
                }
                else if (SlamPhase == 3)
                {
                    Velocity.X = 0;
                }
            }

            if (!IsGrounded)
            {
                if (SlamPhase == 2) Velocity.Y = SlamSpeed;
                else if (SlamPhase != 1 && SlamPhase != 3) Velocity.Y += Gravity * dt;
            }
            else
            {
                if (SlamPhase != 3) Velocity.Y = 0;
                jumpCount = 0;
            }

            // NEU: InputManager benutzt!
            if (InputManager.JustPressed(Keys.Space) && !IsSlamming && KnockbackTimer <= 0 && !IsKnockedDown && !IsHurt && !IsPickingUp)
            {
                if (IsGrounded || jumpCount < MaxJumps)
                {
                    Velocity.Y = JumpForce;
                    jumpCount++;
                    IsGrounded = false;
                }
            }

            Position += Velocity * dt;
            IsGrounded = false;

            if (Position.X < -40)
            {
                Velocity.X = 900f; Velocity.Y = -350f; KnockbackTimer = 0.4f;
                IsGrounded = false; IsSlamming = false; SlamPhase = 0;
            }
            else if (Position.X > screenWidth - Width + 40)
            {
                Velocity.X = -900f; Velocity.Y = -350f; KnockbackTimer = 0.4f;
                IsGrounded = false; IsSlamming = false; SlamPhase = 0;
            }

            if (Position.Y < 0)
            {
                Position.Y = 0;
                if (Velocity.Y < 0) Velocity.Y = 0;
            }

            float floorY = screenHeight - Height;
            if (Position.Y >= floorY)
            {
                Position.Y = floorY;
                IsGrounded = true;

                if (SlamPhase == 2)
                {
                    SlamPhase = 3;
                    SlamTimer = 0.60f;
                    JustLandedSlam = true;
                }
                else if (SlamPhase != 3)
                {
                    Velocity.Y = 0;
                    IsSlamming = false;
                    SlamPhase = 0;
                }
            }

            // NEU: InputManager benutzt!
            bool wantsToDrop = InputManager.IsHeld(Keys.S);
            Rectangle footSensor = new Rectangle((int)Position.X, (int)Position.Y + 1, Width, Height);

            foreach (Rectangle platform in platforms)
            {
                if (footSensor.Intersects(platform))
                {
                    if (!IsSlamming && Velocity.Y >= 0 && (Position.Y + Height - Velocity.Y * dt) <= platform.Top + 15 && !wantsToDrop)
                    {
                        Position.Y = platform.Top - Height;
                        IsGrounded = true;
                        Velocity.Y = 0;
                    }
                }
            }

            UpdateBounds();

            frameTimer += dt;
            if (frameTimer >= FrameInterval)
            {
                if ((IsAttacking || IsStabbing || ThrowTimer > 0) && currentFrame == 2)
                {
                    // Pose halten
                }
                else
                {
                    currentFrame = (currentFrame + 1) % 3;
                }
                frameTimer = 0f;
            }
        }

        public void Draw(SpriteBatch sb, Texture2D sheet, Vector2 offset)
        {
            int row = 0;
            int drawFrame = currentFrame;

            if (IsKnockedDown) row = 6;
            else if (IsHurt) row = 9;
            else if (IsPickingUp) row = 8;
            else if (IsStabbing || ThrowTimer > 0) row = 11;
            else if (IsSlamming) { row = 4; drawFrame = 0; }
            else if (IsAttacking) row = 3;
            else if (IsCarryingDummy || IsCarryingHeavy)
            {
                row = 5;
                if (IsGrounded && Math.Abs(Velocity.X) < 1f) drawFrame = 0;
            }
            else if (!IsGrounded)
            {
                row = 2; drawFrame = Velocity.Y < 0 ? 0 : 1;
            }
            else if (Math.Abs(Velocity.X) > 0) row = 1;
            else row = 0;

            Rectangle src = new Rectangle(drawFrame * SpriteWidth, row * SpriteHeight, SpriteWidth, SpriteHeight);
            SpriteEffects effect = IsFacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle dest = new Rectangle(Bounds.X + (int)offset.X, Bounds.Y + (int)offset.Y, Width, Height);

            sb.Draw(sheet, dest, src, Color.White, 0f, Vector2.Zero, effect, 0f);
        }
    }
}