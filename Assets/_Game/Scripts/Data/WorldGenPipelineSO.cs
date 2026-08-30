using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New Gen Pipeline", menuName = "OceanGame/WorldGenPipelineSO")]
    public class WorldGenPipelineSO : ScriptableObject
    {
        [Header("Dimensions")]
        public int Width = 100;
        public int Height = 100;
        public int SeaLevel = 50;
        public int Seed = 0;
        public bool UseRandomSeed = true;

        [Header("Steps (Sequential Execution)")]
        [SerializeReference]
        public List<WorldGenStep> Steps = new();

        public IEnumerator RunPipelineRoutine(System.Action<WorldGenContext> onComplete, System.Action<float, string> onProgress = null)
        {
            Debug.Log($"[WorldGen] Generation Started");
        
            int actualSeed = UseRandomSeed ? Random.Range(0, 100000) : Seed;
            var context = new WorldGenContext(Width, Height, SeaLevel, actualSeed);

            for (int i = 0; i < Steps.Count; i++)
            {
                var step = Steps[i];
                
                if (step != null && step.RunStep)
                {
                    float progress = (float)i / Steps.Count;
                    onProgress?.Invoke(progress, step.GetType().Name);

                    // Track duration of this step
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                    yield return step.Execute(context);

                    stopwatch.Stop();
                    Debug.Log($"[WorldGen] Step '{step.GetType().Name}' finished in {stopwatch.Elapsed}");
                }
            }

            onProgress?.Invoke(1f, "Complete!");
            onComplete?.Invoke(context);
        }
    }
}
