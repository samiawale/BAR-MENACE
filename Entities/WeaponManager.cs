using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Bar_Menace.Entities
{
    public class WeaponManager
    {
        public List<WeaponItem> Weapons { get; private set; }
        public int HeldWeaponIndex { get; private set; } = -1;

        private Texture2D _bottleTexture;
        private Texture2D _cueTexture;

        public WeaponManager(Texture2D bottleTexture, Texture2D cueTexture)
        {
            _bottleTexture = bottleTexture;
            _cueTexture = cueTexture;

            Weapons = new List<WeaponItem>();

            CreateWeapon(WeaponType.Bottle, new Vector2(280, 468), 2, Color.LimeGreen, _bottleTexture);
            CreateWeapon(WeaponType.BilliardCue, new Vector2(940, 468), 4, Color.Tan, _cueTexture);
            CreateWeapon(WeaponType.Barstool, new Vector2(620, 318), 3, Color.OrangeRed, null);
            CreateWeapon(WeaponType.Jukebox, new Vector2(480, 318), 2, Color.Purple, null);
        }

        private void CreateWeapon(WeaponType type, Vector2 pos, int durability, Color debugColor, Texture2D texture)
        {
            WeaponItem weapon = new WeaponItem();
            weapon.Type = type;
            weapon.State = WeaponState.OnGround;
            weapon.Position = pos;
            weapon.SpawnPoint = pos;
            weapon.MaxDurability = durability;
            weapon.CurrentDurability = durability;
            weapon.DebugColor = debugColor;
            weapon.CustomTexture = texture;
            weapon.Rotation = 0f;

            int texWidth = texture != null ? texture.Width : 32;
            int texHeight = texture != null ? texture.Height : 32;

            // Bounding box size matching the desired visual scale
            int width = (type == WeaponType.BilliardCue) ? 50 : 32;
            int height = 32;

            weapon.BoundingBox = new Rectangle((int)pos.X, (int)pos.Y, width, height);

            // Default origin at center
            weapon.Origin = new Vector2(texWidth / 2f, texHeight / 2f);

            Weapons.Add(weapon);
        }

        public void ForceDestroyHeldWeapon()
        {
            if (HeldWeaponIndex != -1)
            {
                WeaponItem w = Weapons[HeldWeaponIndex];
                if (w.CurrentDurability > 1)
                {
                    w.CurrentDurability--;
                    Weapons[HeldWeaponIndex] = w;
                }
                else
                {
                    DestroyWeapon(HeldWeaponIndex);
                    HeldWeaponIndex = -1;
                }
            }
        }

        public void InstaBreakHeldWeapon()
        {
            if (HeldWeaponIndex != -1)
            {
                DestroyWeapon(HeldWeaponIndex);
                HeldWeaponIndex = -1;
            }
        }

        private void DestroyWeapon(int index)
        {
            WeaponItem w = Weapons[index];
            w.State = WeaponState.Respawning;
            w.CooldownTimer = 5f;
            w.Position = new Vector2(-500, -500);
            w.BoundingBox.X = -500;
            Weapons[index] = w;
        }

        public void Update(float dt, KeyboardState kState, KeyboardState oldKState, Player player, Dummy dummy, List<Rectangle> platforms, int screenWidth, int screenHeight, out bool triggerShake)
        {
            triggerShake = false;

            if (kState.IsKeyDown(Keys.E) && oldKState.IsKeyUp(Keys.E) && !player.IsSlamming && !player.IsCarryingDummy)
            {
                if (HeldWeaponIndex == -1)
                {
                    Rectangle pickupRange = new Rectangle(player.Bounds.X - 40, player.Bounds.Y - 20, player.Bounds.Width + 80, player.Bounds.Height + 40);

                    for (int i = 0; i < Weapons.Count; i++)
                    {
                        if (Weapons[i].State == WeaponState.OnGround && pickupRange.Intersects(Weapons[i].BoundingBox))
                        {
                            HeldWeaponIndex = i;
                            WeaponItem w = Weapons[i];
                            w.State = WeaponState.Held;
                            Weapons[i] = w;
                            break;
                        }
                    }
                }
                else
                {
                    WeaponItem w = Weapons[HeldWeaponIndex];
                    w.State = WeaponState.OnGround;
                    w.Position = new Vector2(player.Position.X + 16, player.Position.Y + 64);
                    w.Velocity = new Vector2(0, 100f);
                    Weapons[HeldWeaponIndex] = w;
                    HeldWeaponIndex = -1;
                }
            }

            if (kState.IsKeyDown(Keys.Q) && oldKState.IsKeyUp(Keys.Q) && HeldWeaponIndex != -1 && !player.IsSlamming)
            {
                WeaponItem w = Weapons[HeldWeaponIndex];
                w.State = WeaponState.Thrown;
                float throwDirection = player.IsFacingRight ? 1f : -1f;

                if (w.Type == WeaponType.Bottle) w.Velocity = new Vector2(1000f * throwDirection, -200f);
                else if (w.Type == WeaponType.BilliardCue) w.Velocity = new Vector2(800f * throwDirection, -100f);
                else if (w.Type == WeaponType.Barstool) w.Velocity = new Vector2(600f * throwDirection, -300f);
                else w.Velocity = new Vector2(500f * throwDirection, -400f);

                Weapons[HeldWeaponIndex] = w;
                HeldWeaponIndex = -1;
                player.TriggerThrow();
            }

            for (int i = 0; i < Weapons.Count; i++)
            {
                WeaponItem w = Weapons[i];

                if (w.State == WeaponState.Held && HeldWeaponIndex == i)
                {
                    w.Rotation = 0f;
                    if (w.Type == WeaponType.Barstool || w.Type == WeaponType.Jukebox)
                    {
                        float offsetX = player.IsFacingRight ? 16 : 16;
                        w.Position = new Vector2(player.Position.X + offsetX, player.Position.Y - 25);
                    }
                    else if (w.Type == WeaponType.BilliardCue)
                    {
                        // Position close to hands, holding from the left edge
                        float offsetX = player.IsFacingRight ? 20 : -20;
                        w.Position = new Vector2(player.Position.X + offsetX, player.Position.Y + 45);
                    }
                    else
                    {
                        float offsetX = player.IsFacingRight ? 40 : -10;
                        w.Position = new Vector2(player.Position.X + offsetX, player.Position.Y + 50);
                    }
                }
                else if (w.State == WeaponState.Thrown || w.State == WeaponState.OnGround)
                {
                    if (w.State == WeaponState.Thrown)
                    {
                        w.Rotation += 10f * dt;
                    }
                    else
                    {
                        w.Rotation = 0f;
                    }

                    w.Velocity.Y += 1200f * dt;
                    w.Position += w.Velocity * dt;
                    bool hitSurface = false;

                    if (w.State == WeaponState.Thrown && w.BoundingBox.Intersects(dummy.Bounds))
                    {
                        int damage = 10;
                        if (w.Type == WeaponType.Bottle) damage = 12;
                        else if (w.Type == WeaponType.BilliardCue) damage = 18;
                        else if (w.Type == WeaponType.Barstool) damage = 25;
                        else if (w.Type == WeaponType.Jukebox) damage = 40;

                        dummy.Health -= damage;
                        dummy.ApplyHit(new Vector2(w.Velocity.X * 0.5f, -300f), 0.3f);
                        triggerShake = true;

                        if (w.CurrentDurability > 1)
                        {
                            w.CurrentDurability--;
                            w.State = WeaponState.OnGround;
                            w.Velocity = new Vector2(-w.Velocity.X * 0.2f, -150f);
                        }
                        else
                        {
                            w.State = WeaponState.Respawning;
                            w.CooldownTimer = 5f;
                            w.Position = new Vector2(-500, -500);
                            w.Velocity = Vector2.Zero;
                        }
                    }
                    else
                    {
                        Rectangle upcomingBounds = new Rectangle((int)w.Position.X, (int)w.Position.Y, w.BoundingBox.Width, w.BoundingBox.Height);
                        foreach (Rectangle platform in platforms)
                        {
                            if (upcomingBounds.Intersects(platform))
                            {
                                if (w.Velocity.Y > 0 && (upcomingBounds.Bottom - w.Velocity.Y * dt) <= platform.Top + 15)
                                {
                                    w.Position.Y = platform.Top - w.BoundingBox.Height;
                                    hitSurface = true;
                                }
                            }
                        }
                        if (w.Position.Y >= screenHeight - w.BoundingBox.Height)
                        {
                            w.Position.Y = screenHeight - w.BoundingBox.Height;
                            hitSurface = true;
                        }
                    }

                    if (hitSurface)
                    {
                        if (w.State == WeaponState.Thrown)
                        {
                            if (w.CurrentDurability > 1)
                            {
                                w.CurrentDurability--;
                                w.State = WeaponState.OnGround;
                                w.Velocity.X *= 0.5f;
                                w.Velocity.Y *= -0.4f;
                            }
                            else
                            {
                                w.State = WeaponState.Respawning;
                                w.CooldownTimer = 5f;
                                w.Position = new Vector2(-500, -500);
                                w.Velocity = Vector2.Zero;
                            }
                        }
                        else
                        {
                            w.Velocity.Y = 0;
                            w.Velocity.X = MathHelper.Lerp(w.Velocity.X, 0f, 5f * dt);
                        }
                    }

                    if (w.Position.X < -200 || w.Position.X > screenWidth + 200 || w.Position.Y > screenHeight + 200)
                    {
                        w.State = WeaponState.Respawning;
                        w.CooldownTimer = 5f;
                        w.Position = new Vector2(-500, -500);
                        w.Velocity = Vector2.Zero;
                    }
                }
                else if (w.State == WeaponState.Respawning)
                {
                    w.CooldownTimer -= dt;
                    if (w.CooldownTimer <= 0)
                    {
                        w.State = WeaponState.OnGround;
                        w.CurrentDurability = w.MaxDurability;
                        w.Position = w.SpawnPoint;
                        w.Velocity = Vector2.Zero;
                    }
                }

                w.BoundingBox.X = (int)w.Position.X;
                w.BoundingBox.Y = (int)w.Position.Y;
                Weapons[i] = w;
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Vector2 cameraOffset, Player player)
        {
            foreach (WeaponItem weapon in Weapons)
            {
                if (weapon.State != WeaponState.Respawning)
                {
                    Vector2 drawPos = new Vector2(weapon.Position.X + cameraOffset.X, weapon.Position.Y + cameraOffset.Y);

                    if (weapon.CustomTexture != null)
                    {
                        float scale = 1f;
                        Vector2 origin = weapon.Origin;
                        SpriteEffects effect = SpriteEffects.None;

                        if (weapon.Type == WeaponType.Bottle)
                        {
                            scale = 1.8f;
                        }
                        else if (weapon.Type == WeaponType.BilliardCue)
                        {
                            scale = 0.15f;

                            if (weapon.State == WeaponState.Held)
                            {
                                // If facing left, flip horizontally and anchor from the right edge (which becomes the left when flipped)
                                if (!player.IsFacingRight)
                                {
                                    effect = SpriteEffects.FlipHorizontally;
                                    origin = new Vector2(weapon.CustomTexture.Width, weapon.CustomTexture.Height / 2f);
                                }
                                else
                                {
                                    origin = new Vector2(0, weapon.CustomTexture.Height / 2f);
                                }
                            }
                        }

                        Vector2 centeredPos = new Vector2(drawPos.X + (weapon.BoundingBox.Width / 2f), drawPos.Y + (weapon.BoundingBox.Height / 2f));

                        spriteBatch.Draw(
                            weapon.CustomTexture,
                            centeredPos,
                            null,
                            Color.White,
                            weapon.Rotation,
                            origin,
                            scale,
                            effect,
                            0f
                        );
                    }
                    else
                    {
                        Rectangle renderRect = new Rectangle((int)drawPos.X, (int)drawPos.Y, weapon.BoundingBox.Width, weapon.BoundingBox.Height);
                        spriteBatch.Draw(pixelTexture, renderRect, weapon.DebugColor);
                    }
                }
            }
        }
    }
}