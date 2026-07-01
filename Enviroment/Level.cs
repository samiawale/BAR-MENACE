using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Bar_Menace.Environment
{
    public class Level
    {
        // Hier liegen alle Plattformen deiner Bar
        public List<Rectangle> Platforms { get; private set; }

        // Die Farbe deiner Bar-Möbel (braun/holzfarben)
        private Color platformColor = new Color(139, 69, 19);

        public Level()
        {
            Platforms = new List<Rectangle>();
            SetupMap();
        }

        // Diese Methode definiert das Layout deiner Bar
        private void SetupMap()
        {
            // Wir nutzen hier exakt deine aktuellen Koordinaten aus Game1.cs
            Platforms.Add(new Rectangle(150, 500, 300, 30));
            Platforms.Add(new Rectangle(800, 500, 300, 30));
            Platforms.Add(new Rectangle(440, 350, 400, 30));
            Platforms.Add(new Rectangle(540, 200, 200, 25));
        }

        // Das Level zeichnet sich selbst
        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, Vector2 cameraOffset)
        {
            foreach (Rectangle platform in Platforms)
            {
                Rectangle renderRect = platform;
                renderRect.X += (int)cameraOffset.X;
                renderRect.Y += (int)cameraOffset.Y;

                spriteBatch.Draw(pixelTexture, renderRect, platformColor);
            }
        }
    }
}