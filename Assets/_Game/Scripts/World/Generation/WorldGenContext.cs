using UnityEngine;

namespace OceanGame
{
    public class WorldGenContext
    {
        public int Width { get; }
        public int Height { get; }
        public int Seed { get; }
        public int SeaLevel { get; }
        public int UndergroundBottomLevel { get; }
        public int GenColumnsPerFrame { get; }
        public System.Random Random { get; }

        public TileData[,] FgGrid { get; set; }
        public TileData[,] BgGrid { get; set; }

        public WorldGenContext(int width, int height, int seed, int undergroundBottomLevel, int genColumnsPerFrame, int seaLevel)
        {
            Width = width;
            Height = height;
            Seed = seed;
            SeaLevel = seaLevel;
            UndergroundBottomLevel = undergroundBottomLevel;
            GenColumnsPerFrame = genColumnsPerFrame;
            FgGrid = new TileData[width, height];
            BgGrid = new TileData[width, height];
            Random = new(Seed);
        }
    }
}