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
                case StateMachineType.Crab:
                    return new CrabRootState(null, serverCharacter);
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
        Crab,
        Jellyfish
    }
}