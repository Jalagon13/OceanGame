using System;
using UnityEngine;

namespace OceanGame
{
    public class PlayerDepthTracker : MonoBehaviour
    {
        public event Action<int> OnDepthChanged;

        public int CurrentDepthMeters { get; private set; }
        public bool HasDepthSample { get; private set; }

        private void Update()
        {
            WorldManager world = WorldManager.Instance;

            if (world == null || !world.IsWorldReady)
                return;

            // Positive values are below sea level; negative values are above it.
            int depth = Mathf.Max(0, Mathf.RoundToInt(world.SeaLevel - transform.position.y));

            if (HasDepthSample && depth == CurrentDepthMeters)
                return;

            CurrentDepthMeters = depth;
            HasDepthSample = true;
            OnDepthChanged?.Invoke(CurrentDepthMeters);
        }
    }
}