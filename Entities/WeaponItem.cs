using Microsoft.Xna.Framework;

namespace Bar_Menace.Entities
{
    // Die verschiedenen Waffen im Spiel
    public enum WeaponType
    {
        Bottle,
        BilliardCue,
        Barstool,
        Jukebox
    }

    // Die Zustände, in denen sich eine Waffe befinden kann
    public enum WeaponState
    {
        OnGround,
        Held,
        Thrown,
        Respawning
    }

    // Die Eigenschaften einer einzelnen Waffe
    public struct WeaponItem
    {
        public WeaponType Type;
        public WeaponState State;
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 SpawnPoint;
        public int MaxDurability;
        public int CurrentDurability;
        public float CooldownTimer;
        public Rectangle BoundingBox;
        public Color DebugColor; // Wird später durch echte Sprites ersetzt
    }
}