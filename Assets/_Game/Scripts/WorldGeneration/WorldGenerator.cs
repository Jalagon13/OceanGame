using UnityEngine;

namespace OceanGame
{
    public class WorldGenerator : MonoBehaviour
    {
        [SerializeField] private WorldGenPipelineSO _currentWorldGenPreset;

        private void Start()
        {
            GenerateWorld(); // Auto-generate on start for testing
        }

        public void GenerateWorld()
        {
            if (_currentWorldGenPreset == null)
            {
                Debug.LogError("[WorldGenerator] No WorldGenPipelineSO assigned!");
                return;
            }

            // Start the pipeline coroutine
            StartCoroutine(_currentWorldGenPreset.RunPipelineRoutine(onComplete: OnWorldGenComplete, onProgress: OnWorldGenProgress));
        }

        private void OnWorldGenProgress(float progress, string stepName)
        {
            // Update UI loading bars or log progress
            Debug.Log($"[WorldGen Progress] {(progress * 100):F0}% - Current Step: {stepName}");
        }

        private void OnWorldGenComplete(WorldGenContext context)
        {
            Debug.Log($"[WorldGen Complete] Finished generating world ({context.Width}x{context.Height}) with seed: {context.Seed}");

            // TODO: Pass context.FgTiles & context.BgTiles to WorldManager layers here!

            // Refresh camera bounds after building tilemap
            if (PlayerCamera.Instance != null)
            {
                PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
            }
        }
    }
}