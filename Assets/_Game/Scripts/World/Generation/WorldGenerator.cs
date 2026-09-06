using UnityEngine;

namespace OceanGame
{
    public class WorldGenerator : MonoBehaviour
    {
        [field: SerializeField] 
        public WorldGenPipelineSO CurrentWorldGenPreset { get; private set; }

        private void Start()
        {
            GenerateWorld(); // Auto-generate on start for testing
        }

        public void GenerateWorld()
        {
            if (CurrentWorldGenPreset == null)
            {
                Debug.LogError("[WorldGenerator] No WorldGenPipelineSO assigned!");
                return;
            }

            // Start the pipeline coroutine
            StartCoroutine(CurrentWorldGenPreset.RunPipelineRoutine(onComplete: OnWorldGenComplete, onProgress: OnWorldGenProgress));
        }

        private void OnWorldGenProgress(float progress, string stepName)
        {
            // Update UI loading bars or log progress
            Debug.Log($"[WorldGen Progress] {(progress * 100):F0}% - Current Step: {stepName}");
        }

        private void OnWorldGenComplete(WorldGenContext context)
        {
            Debug.Log($"[WorldGen Complete] Finished generating world ({context.Width}x{context.Height}) with seed: {context.Seed}");

            WorldManager.Instance.LoadGeneratedWorld(context);
        }
    }
}