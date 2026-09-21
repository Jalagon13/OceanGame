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
    
    public enum LifeState
    {
        Alive,
        IFrame,
        Dead
    }

    public enum StatType
    {
        MoveSpeed,
        MaxHealth,
        Defense
    }

    public enum StatModifierType
    {
        Flat,
        Percent
    }

    public readonly struct StatModifier : IEquatable<StatModifier>
    {
        public StatModifierType Type { get; }
        public float Value { get; }

        public StatModifier(StatModifierType type, float value)
        {
            Type = type;
            Value = value;
        }

        public bool Equals(StatModifier other) => Type == other.Type && Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is StatModifier other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Type, Value);
    }
}