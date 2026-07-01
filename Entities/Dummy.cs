using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Bar_Menace.Entities
{
    public class Dummy
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Rectangle Bounds { get; private set; }
        public int Health = 100;

        public bool IsHit = false;
        public bool IsDown = false;
        private float animationTimer = 0f;
        public float KnockbackTimer = 0f;

        public const int SpriteWidth = 32, SpriteHeight = 64;
        public const int DrawScale = 2;
        public const int Width = SpriteWidth * DrawScale;
        public const int Height = SpriteHeight * DrawScale;

        public Dummy(Vector2 startPosition)
        {
            Position = startPosition;
            UpdateBounds();
        }

        private void UpdateBounds()
        {
            Bounds = new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
        }

        public void ApplyHit(Vector2 force, float stunDuration)
        {
            Velocity = force;
            IsHit = true;
            animationTimer = stunDuration;
            IsDown = false;
        }

        public void Update(float dt, int screenWidth, int screenHeight, List<Rectangle> platforms)
        {
            if (KnockbackTimer > 0)
            {
                KnockbackTimer -= dt;
                Velocity.X = MathHelper.Lerp(Velocity.X, 0f, 2f * dt);
            }
            else
            {
                Velocity.X = MathHelper.Lerp(Velocity.X, 0f, 5f * dt);
            }

            Velocity.Y += 1400f * dt;
            Position += Velocity * dt;

            if (Position.X < -40)
            {
                Velocity.X = 900f;
                Velocity.Y = -350f;
                KnockbackTimer = 0.4f;
            }
            else if (Position.X > screenWidth - Width + 40)
            {
                Velocity.X = -900f;
                Velocity.Y = -350f;
                KnockbackTimer = 0.4f;
            }

            if (Position.Y >= screenHeight - Height)
            {
                Position.Y = screenHeight - Height;
                Velocity.Y = 0;
                if (IsHit) IsDown = true;
            }

            Rectangle upcomingBounds = new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
            foreach (Rectangle platform in platforms)
            {
                if (upcomingBounds.Intersects(platform))
                {
                    if (Velocity.Y > 0 && (upcomingBounds.Bottom - Velocity.Y * dt) <= platform.Top + 12)
                    {
                        Position.Y = platform.Top - Height;
                        Velocity.Y = 0;
                        if (IsHit) IsDown = true;
                    }
                }
            }

            if (animationTimer > 0)
            {
                animationTimer -= dt;
            }
            else
            {
                IsHit = false;
            }

            UpdateBounds();
        }

        public void Draw(SpriteBatch sb, Texture2D spriteSheet, Vector2 offset)
        {
            Rectangle destRect = new Rectangle(Bounds.X + (int)offset.X, Bounds.Y + (int)offset.Y, Width, Height);

            if (spriteSheet != null && spriteSheet.Width > 1)
            {
                int row = IsDown ? 1 : (IsHit ? 2 : 0);
                Rectangle src = new Rectangle(0, row * SpriteHeight, SpriteWidth, SpriteHeight);
                sb.Draw(spriteSheet, destRect, src, Color.White);
            }
            else
            {
                Color debugColor = IsDown ? Color.DarkGray : (IsHit ? Color.Red : Color.Gray);
                sb.Draw(spriteSheet, destRect, debugColor);
            }
        }
    }
}