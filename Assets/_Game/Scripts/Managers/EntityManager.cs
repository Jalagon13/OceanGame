using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    [DefaultExecutionOrder(-1000)]
    public class EntityManager : MonoBehaviour
    {
        public static EntityManager Instance { get; private set; }
        
        private List<Entity> _entities = new();

        private void Awake() 
        {
            Instance = this;
        }
        
        private void FixedUpdate() 
        {
            if (!WorldManager.Instance.IsWorldReady) return;
            
            for (int i = _entities.Count - 1; i >= 0; i--)
            {
                _entities[i].FixedTick(Time.fixedDeltaTime);
            }

            GridPhysics.CheckOverlap(_entities);
        }

        public void Register(Entity entity)
        {
            if(!_entities.Contains(entity))
            {
                _entities.Add(entity);
            }
        }

        public void UnRegister(Entity entity)
        {
            if (_entities.Contains(entity))
            {
                _entities.Remove(entity);
            }
        }
    }
}