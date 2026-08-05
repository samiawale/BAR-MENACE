using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Bar_Menace.Entities
{
    // Diese Klasse speichert alle Eigenschaften, die eine Waffe haben kann
    public class WeaponData
    {
        public int StrikeDamage;
        public float StrikeForceX;
        public float StrikeForceY;
        public float SwingDuration;
        public int HitboxReach;

        public int ThrowDamage;
        public float ThrowForceX;
        public float ThrowForceY;

        public float WeightMultiplier;
        public bool IsHeavy;
    }

    // Dies ist unsere "Datenbank", in der jede Waffe genau EINMAL definiert wird
    public static class WeaponDatabase
    {
        // Stats für die bloßen Fäuste (Unbewaffnet)
        public static readonly WeaponData Unarmed = new WeaponData
        {
            StrikeDamage = 5,
            StrikeForceX = 350f,
            StrikeForceY = -100f,
            SwingDuration = 0.75f,
            HitboxReach = 45,
            WeightMultiplier = 1.0f,
            IsHeavy = false
        };

        // Stats für alle Waffen
        public static readonly Dictionary<WeaponType, WeaponData> Stats = new Dictionary<WeaponType, WeaponData>()
        {
            {
                WeaponType.Bottle, new WeaponData {
                    StrikeDamage = 10, StrikeForceX = 450f, StrikeForceY = -120f, SwingDuration = 0.15f, HitboxReach = 60,
                    ThrowDamage = 12, ThrowForceX = 1000f, ThrowForceY = -200f,
                    WeightMultiplier = 1.0f, IsHeavy = false
                }
            },
            {
                WeaponType.BilliardCue, new WeaponData {
                    StrikeDamage = 15, StrikeForceX = 600f, StrikeForceY = -150f, SwingDuration = 0.3f, HitboxReach = 120,
                    ThrowDamage = 18, ThrowForceX = 800f, ThrowForceY = -100f,
                    WeightMultiplier = 0.85f, IsHeavy = false
                }
            },
            {
                WeaponType.Barstool, new WeaponData {
                    StrikeDamage = 20, StrikeForceX = 750f, StrikeForceY = -200f, SwingDuration = 0.35f, HitboxReach = 70,
                    ThrowDamage = 25, ThrowForceX = 600f, ThrowForceY = -300f,
                    WeightMultiplier = 0.65f, IsHeavy = true
                }
            },
            {
                WeaponType.Jukebox, new WeaponData {
                    StrikeDamage = 35, StrikeForceX = 900f, StrikeForceY = -250f, SwingDuration = 0.4f, HitboxReach = 80,
                    ThrowDamage = 40, ThrowForceX = 500f, ThrowForceY = -400f,
                    WeightMultiplier = 0.45f, IsHeavy = true
                }
            }
        };
    }
}