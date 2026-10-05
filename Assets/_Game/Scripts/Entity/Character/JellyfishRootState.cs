using UnityEngine;

namespace OceanGame
{
    public class JellyfishRootState : State
    {
        private readonly ServerCharacter _ctx;

        public JellyfishRootState(StateMachine m, ServerCharacter ctx) : base(m, null)
        {
            _ctx = ctx;
        }
    }
}