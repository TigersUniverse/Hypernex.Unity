using UnityEngine;

namespace Hypernex.Game.Bindings
{
    public struct LeftMove
    {
        public bool valid;
        public Vector3 move;
        public bool jump;
        public bool moving;
        public Vector2 input;
    }

    public struct RightMove
    {
        public bool valid;
        public Vector3 move;
        public bool jump;
        public bool moving;
    }
}