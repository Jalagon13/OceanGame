using UnityEngine;

namespace OceanGame
{
    public class WorldGenContext
    {
        public int Width { get; }
        public int Height { get; }
        public int Seed { get; }
        public System.Random Random { get; }

        public TileData[,] FgTiles { get; set; }
        public TileData[,] BgTiles { get; set; }
        public bool[,] CaveGrid { get; } // True is air
        public int[] SurfaceHeightValues { get; }

        public WorldGenContext(int width, int height, int seed)
        {
            Width = width;
            Height = height;
            Seed = seed;
            FgTiles = new TileData[width, height];
            BgTiles = new TileData[width, height];
            CaveGrid = new bool[width, height];
            SurfaceHeightValues = new int[width];
            Random = new(Seed);
        }
    }
}