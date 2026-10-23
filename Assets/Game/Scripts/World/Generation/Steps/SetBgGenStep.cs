using System.Collections;
using UnityEngine;

namespace SketchLand
{
    public class SetBgGenStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _sandSO;
        [SerializeField] private TileConfigSO _limestoneSO;
        [SerializeField] private int _depthOfSandLayer = 2;
        [SerializeField, Min(0.0001f)] private float _noiseFrequency = 0.01f;
        [SerializeField, Min(0f)] private float _noiseAmplitude = 5f;
        [SerializeField] private int _yValFromSeaLevel = 10;
        [SerializeField] private int _undergroundBottomLevel = 600;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            float noiseOffsetX = ctx.Random.Next(-100000, 100000);
            float noiseOffsetY = ctx.Random.Next(-100000, 100000);

            ushort sandTileId = _sandSO != null ? _sandSO.GetId() : (ushort)0;
            ushort limestoneTileId = _limestoneSO.GetId();
            int sandLayerDepth = Mathf.Max(0, _depthOfSandLayer);
            int columnsPerFrame = Mathf.Max(1, ctx.GenColumnsPerFrame);

            for (int x = 0; x < ctx.Width; x++)
            {
                float noise = Mathf.PerlinNoise((x + noiseOffsetX) * _noiseFrequency, noiseOffsetY * _noiseFrequency);
                float height = noise * _noiseAmplitude;

                int surfaceY = Mathf.RoundToInt(ctx.SeaLevel + _yValFromSeaLevel + height);
                surfaceY = Mathf.Clamp(surfaceY, 0, ctx.Height - 1);

                for (int y = surfaceY; y >= _undergroundBottomLevel; y--)
                {
                    bool isSandLayer = surfaceY - y < sandLayerDepth;
                    ushort tileId = isSandLayer ? sandTileId : limestoneTileId;
                    ctx.BgGrid[x, y] = new TileData(tileId);
                }

                if ((x + 1) % columnsPerFrame == 0)
                    yield return null;
            }
        }
    }
}