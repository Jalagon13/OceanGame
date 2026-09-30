using UnityEngine;

namespace OceanGame
{
    public class CrabRootState : State
    {
        private readonly ServerCharacter _ctx;

        public CrabRootState(StateMachine m, ServerCharacter ctx) : base(m, null)
        {
            _ctx = ctx;
        }
    }
}