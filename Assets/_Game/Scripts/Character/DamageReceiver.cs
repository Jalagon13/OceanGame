using System;
using Unity.Netcode;
using UnityEngine;

namespace OceanGame
{
    [RequireComponent(typeof(ServerCharacter))]
    public class DamageReceiver : NetworkBehaviour
    {
        private ServerCharacter _character;
        private const float MinKnockbackForce = 0f;
        private const float MaxKnockbackForce = 100f;

        private void Awake()
        {
            _character = GetComponent<ServerCharacter>();
        }

        // Any damage enters through here
        public void ReceiveHit(SyncHitData hit)
        {
            if(_character.Health.CurrentLifeState.Value != LifeState.Alive) return;
        
            ReceiveHitServerRpc(hit);
        }
        
        [Rpc(SendTo.Server)]
        private void ReceiveHitServerRpc(SyncHitData hit)
        {
            if(_character.Health.CurrentLifeState.Value == LifeState.IFrame) return;
        
            float difficultyMult = 0.5f; // Placeholder for difficulty multiplier, 0.5 for normal, 0.75 for hard, 1 for insane TENT mults
            int defense = Mathf.RoundToInt(_character.Data.BaseDefense * difficultyMult);
            int netDamage = Mathf.Max(1, hit.Damage - defense); // Clamp it to 1

            _character.Health.TakeDamage(netDamage);

            if (!_character.Data.CanBeKnockedBacked || _character.Health.CurrentLifeState.Value == LifeState.Dead) return;

            float resistance = Mathf.Clamp01(_character.Data.BaseKbResist);
            float finalForce = hit.KnockbackForce * (1f - resistance);
            finalForce = Mathf.Clamp(finalForce, MinKnockbackForce, MaxKnockbackForce);

            Vector2 direction = ((Vector2)_character.transform.position - hit.SourcePosition).normalized;

            _character.ApplyKnockback(direction * finalForce);
        }
    }
    
    public struct SyncHitData : IEquatable<SyncHitData>, INetworkSerializable
    {
        public int Damage;
        public int KnockbackForce;
        public Vector2 SourcePosition;

        public bool Equals(SyncHitData other)
        {
            if(Damage != other.Damage || 
                KnockbackForce != other.KnockbackForce || 
                SourcePosition.x != other.SourcePosition.x || 
                SourcePosition.y != other.SourcePosition.y) return false;
                
            return true;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Damage);
            serializer.SerializeValue(ref KnockbackForce);
            serializer.SerializeValue(ref SourcePosition);
        }
    }
}