using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;

namespace Bar_Menace.Entities
{
    public class Dummy
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Rectangle Bounds { get; private set; }
        public int Health = 100;

        public bool IsGrounded = false, IsFacingRight = true;
        public bool IsHit = false, IsDown = false, IsCarried = false, IsGettingUp = false;

        private float hitTimer = 0f;
        private float getUpTimer = 0f;
        private const float Gravity = 1400f;
        public const int SpriteWidth = 32, SpriteHeight = 64;
        public const int DrawScale = 2;
        public const int Width = SpriteWidth * DrawScale;
        public const int Height = SpriteHeight * DrawScale;

        private int currentFrame = 0;
        private float frameTimer = 0f;
        private const float FrameInterval = 0.15f;

        public Dummy(Vector2 startPosition) { Position = startPosition; UpdateBounds(); }

        private void UpdateBounds() => Bounds = new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        public void ApplyHit(Vector2 force, float stunDuration)
        {
            Velocity = force;
            IsHit = true;
            IsDown = true;
            IsGettingUp = false;
            hitTimer = stunDuration;
            IsFacingRight = force.X > 0;
        }

        public void Update(float dt, int screenWidth, int screenHeight, List<Rectangle> platforms)
        {
            if (!IsCarried)
            {
                if (!IsGrounded) Velocity.Y += Gravity * dt;
                else { Velocity.Y = 0; Velocity.X = MathHelper.Lerp(Velocity.X, 0f, 15f * dt); }

                Position += Velocity * dt;
                IsGrounded = false;

                if (Position.X < 0) { Position.X = 0; Velocity.X = 0; }
                else if (Position.X > screenWidth - Width) { Position.X = screenWidth - Width; Velocity.X = 0; }

                float floorY = screenHeight - Height;
                if (Position.Y >= floorY) { Position.Y = floorY; IsGrounded = true; }

                foreach (Rectangle platform in platforms)
                {
                    if (new Rectangle((int)Position.X, (int)Position.Y + 1, Width, Height).Intersects(platform))
                    {
                        if (Velocity.Y >= 0 && (Position.Y + Height - Velocity.Y * dt) <= platform.Top + 15)
                        {
                            Position.Y = platform.Top - Height;
                            IsGrounded = true;
                            Velocity.Y = 0;
                        }
                    }
                }
            }

            // Logic: Wait for hit timer, then start Getting Up
            if (IsDown && !IsGettingUp && hitTimer <= 0)
            {
                getUpTimer = 0.8f;
                IsGettingUp = true;
            }

            if (IsGettingUp)
            {
                getUpTimer -= dt;
                if (getUpTimer <= 0) { IsDown = false; IsGettingUp = false; }
            }

            if (hitTimer > 0) hitTimer -= dt;
            else IsHit = false;

            UpdateBounds();
            frameTimer += dt;
            if (frameTimer >= FrameInterval) { currentFrame = (currentFrame + 1) % 3; frameTimer = 0f; }
        }

        public void Draw(SpriteBatch sb, Texture2D sheet, Vector2 offset)
        {
            SpriteEffects effect = IsFacingRight ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            int row = 0;
            int frame = currentFrame;
            int rowWidth = SpriteWidth; // Default: 32px

            // Define Logic based on Dummy_sprite.png
            if (IsCarried)
            {
                row = 6;            // Your flat pose row
                rowWidth = 64;      // It spans 2 boxes
                frame = 0;          // Keep static
            }
            else if (IsGettingUp)
            {
                row = 7;            // Your get-up animation row
            }
            else if (IsDown)
            {
                row = 6;            // Your flat pose row
                rowWidth = 64;      // It spans 2 boxes
                frame = 0;          // Keep static
            }
            else if (IsHit)
            {
                row = 1;
            }
            else
            {
                row = 0;            // Idle/Default
            }

            // Source rectangle uses the dynamic rowWidth (32 or 64)
            Rectangle src = new Rectangle(frame * SpriteWidth, row * SpriteHeight, rowWidth, SpriteHeight);

            // Calculate width to render
            int drawWidth = rowWidth * DrawScale;

            // Calculate position: If carried/down, center the wider sprite
            int drawX = (int)(Bounds.X + offset.X);
            if (rowWidth > SpriteWidth)
            {
                drawX -= (drawWidth - Width) / 2; // Center horizontally
            }

            Rectangle dest = new Rectangle(drawX, (int)(Bounds.Y + offset.Y), drawWidth, Height);

            sb.Draw(sheet, dest, src, Color.White, 0f, Vector2.Zero, effect, 0f);
        }
    }
}