using System;
using UnityEngine;

namespace OceanGame
{
    public class CharacterCommons
    {
        public static State GetNpcHSMRootState(ServerCharacter serverCharacter, StateMachineType stateMachineType)
        {
            switch (stateMachineType)
            {
                case StateMachineType.Player:
                    return new PlayerRootState(null, serverCharacter);
                // case StateMachineType.Fish:
                //     return new FishStateMachine(serverCharacter);
                // case StateMachineType.Jellyfish:
                //     return new JellyfishStateMachine(serverCharacter);
                default:
                    throw new NotSupportedException($"No StateMachine Selected");
            }
        }
    }

    public enum StateMachineType
    {
        Player,
        Fish,
        Jellyfish
    }
}