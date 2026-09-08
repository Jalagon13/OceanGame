using System;
using UnityEngine;

namespace OceanGame
{
    public abstract class Entity : MonoBehaviour
    {
        [SerializeField] private bool _ignoreCollision;

        [HideInInspector] public Vector2 Velocity;
        [HideInInspector] public Vector2 ColliderSize;
        
        public GridPhysics.CollisionResult CollisionResult { get; private set; }

        // Updating and moving the entity
        public virtual void FixedTick(float fixedDeltaTime) 
        {
            CollisionResult = GridPhysics.MoveAndResolve(transform.position, Velocity, ColliderSize, fixedDeltaTime, _ignoreCollision);
            transform.position = CollisionResult.NewPosition;
        }

        // If the phyics systems finds an overlap, do something with it
        public virtual void OnEntityOverlap(Entity other)
        {
            
        } 
    }
}