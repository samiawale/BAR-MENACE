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

        public WeaponManager()
        {
            Weapons = new List<WeaponItem>();
            // Exaktes Balancing!
            CreateWeapon(WeaponType.Bottle, new Vector2(280, 468), 2, Color.LimeGreen);
            CreateWeapon(WeaponType.BilliardCue, new Vector2(940, 468), 4, Color.Tan);
            CreateWeapon(WeaponType.Barstool, new Vector2(620, 318), 3, Color.OrangeRed);
            CreateWeapon(WeaponType.Jukebox, new Vector2(480, 318), 2, Color.Purple);
        }

        private void CreateWeapon(WeaponType type, Vector2 pos, int durability, Color debugColor)
        {
            WeaponItem weapon = new WeaponItem();
            weapon.Type = type;
            weapon.State = WeaponState.OnGround;
            weapon.Position = pos;
            weapon.SpawnPoint = pos;
            weapon.MaxDurability = durability;
            weapon.CurrentDurability = durability;
            weapon.DebugColor = debugColor;
            weapon.BoundingBox = new Rectangle((int)pos.X, (int)pos.Y, 32, 32);
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
                    if (w.Type == WeaponType.Barstool || w.Type == WeaponType.Jukebox)
                    {
                        float offsetX = player.IsFacingRight ? 16 : 16;
                        w.Position = new Vector2(player.Position.X + offsetX, player.Position.Y - 25);
                    }
                    else
                    {
                        float offsetX = player.IsFacingRight ? 40 : -10;
                        w.Position = new Vector2(player.Position.X + offsetX, player.Position.Y + 50);
                    }
                }
                else if (w.State == WeaponState.Thrown || w.State == WeaponState.OnGround)
                {
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
                        Rectangle upcomingBounds = new Rectangle((int)w.Position.X, (int)w.Position.Y, 32, 32);
                        foreach (Rectangle platform in platforms)
                        {
                            if (upcomingBounds.Intersects(platform))
                            {
                                if (w.Velocity.Y > 0 && (upcomingBounds.Bottom - w.Velocity.Y * dt) <= platform.Top + 15)
                                {
                                    w.Position.Y = platform.Top - 32;
                                    hitSurface = true;
                                }
                            }
                        }
                        if (w.Position.Y >= screenHeight - 32)
                        {
                            w.Position.Y = screenHeight - 32;
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

                    // NEU: Kugelsicherer Out-Of-Bounds-Check!
                    // Egal ob zu weit links, rechts oder sogar durch den Boden gefallen: Waffe wird resettet.
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
                        // Respawnt die Waffe unversehrt am Ursprungsort
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

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Vector2 cameraOffset)
        {
            foreach (WeaponItem weapon in Weapons)
            {
                if (weapon.State != WeaponState.Respawning)
                {
                    Rectangle renderRect = new Rectangle((int)weapon.Position.X + (int)cameraOffset.X,
                                                         (int)weapon.Position.Y + (int)cameraOffset.Y, 32, 32);
                    spriteBatch.Draw(pixelTexture, renderRect, weapon.DebugColor);
                }
            }
        }
    }
}