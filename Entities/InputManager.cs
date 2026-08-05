using Microsoft.Xna.Framework.Input;

namespace Bar_Menace
{
    public static class InputManager
    {
        private static KeyboardState _currentState;
        private static KeyboardState _previousState;

        // Wird jeden Frame in Game1.cs ganz oben aufgerufen
        public static void Update()
        {
            _previousState = _currentState;
            _currentState = Keyboard.GetState();
        }

        // Checkt, ob eine Taste GENAU in diesem Frame gedrückt wurde (für Schlagen, Springen etc.)
        public static bool JustPressed(Keys key)
        {
            return _currentState.IsKeyDown(key) && _previousState.IsKeyUp(key);
        }

        // Checkt, ob eine Taste gerade gehalten wird (fürs Laufen)
        public static bool IsHeld(Keys key)
        {
            return _currentState.IsKeyDown(key);
        }

        // Checkt, ob eine Taste gerade losgelassen wurde
        public static bool JustReleased(Keys key)
        {
            return _currentState.IsKeyUp(key) && _previousState.IsKeyDown(key);
        }
    }
}