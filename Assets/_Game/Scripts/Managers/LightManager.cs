using System;
using System.Collections.Generic; // Required for reusable Queue
using UnityEngine;
using UnityEngine.UI;

namespace OceanGame
{
    // Next add attenuation for solid vs background tiles
    // Next add a pre attenuation buffer zone when going through solid or background tiles before attenuation

    public class LightManager : MonoBehaviour
    {
        public static LightManager Instance { get; private set; }

        [SerializeField] private RawImage _lightmapOverlay;
        [SerializeField] private Material _multiplyMaterial;

        [Header("Light Settings")]
        [SerializeField] private FilterMode _lightmapFilterMode;
        [SerializeField] private int _extraLightmapPadding;
        [SerializeField, Min(1f)] private float _fullBrightness;
        [SerializeField] private int _decayBuffer = 1;
        
        [Header("Decay")]
        [SerializeField] private float _solidFgDecay = 3f;
        [SerializeField] private float _baseDecay = 1f;

        [Header("Blur")]
        [Range(0, 4)]
        [SerializeField] private int _blurPasses = 2; // Tweakable in the Inspector!

        private RectInt _lmBounds;
        private Texture2D _lightmapTexture;
        private float[,] _lightGrid;
        private float[,] _blurGrid; // Caching a reusable scratchpad array for the blur math
        private int[,] _solidDepthGrid;
        private Color32[] _colorBuffer;

        private Queue<Vector2Int> _lightQueue;
        private static readonly Vector2Int[] _directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        private void Awake()
        {
            Instance = this;
            _lightQueue = new Queue<Vector2Int>(100 * 100); // Pre-allocate a reasonable default size 
        }

        private void Start()
        {
            PlayerCamera.Instance.OnVisibleTileBoundsChanged += OnBoundsChanged;
        }

        private void OnDestroy()
        {
            if (PlayerCamera.Instance != null) PlayerCamera.Instance.OnVisibleTileBoundsChanged -= OnBoundsChanged;

            // Prevent memory leaks when changing scenes
            if (_lightmapTexture != null) Destroy(_lightmapTexture);
        }

        private void OnBoundsChanged(RectInt oldBounds, RectInt newBounds)
        {
            var world = WorldManager.Instance;

            if (world == null || !world.IsWorldReady) return;

            // Initializations
            int xMin = newBounds.xMin - _extraLightmapPadding;
            int yMin = newBounds.yMin - _extraLightmapPadding;
            int xMax = newBounds.xMax + _extraLightmapPadding;
            int yMax = newBounds.yMax + _extraLightmapPadding;

            _lmBounds = new(xMin, yMin, xMax - xMin, yMax - yMin);

            int localWidth = _lmBounds.width;
            int localHeight = _lmBounds.height;

            if (_lightmapTexture == null) // Initialize if new
            {
                _lightmapTexture = new(localWidth, localHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = _lightmapFilterMode,
                    wrapMode = TextureWrapMode.Clamp
                };
            }
            else if (_lightmapTexture.width != localWidth || _lightmapTexture.height != localHeight) // dynamically set it when camera changes dimensions
            {
                _lightmapTexture.Reinitialize(localWidth, localHeight);
            }

            // Manage sizes for both the light and scratchpad blur grids safely
            if (_lightGrid == null || _lightGrid.GetLength(0) != localWidth || _lightGrid.GetLength(1) != localHeight)
            {
                _lightGrid = new float[localWidth, localHeight];
                _blurGrid = new float[localWidth, localHeight];
                _colorBuffer = new Color32[localWidth * localHeight];
                _solidDepthGrid = new int[localWidth, localHeight];
            }
            else
            {
                Array.Clear(_lightGrid, 0, _lightGrid.Length); // Clear existing frames rather than instantiating clean arrays
                // Note: _blurGrid doesn't need explicit clearing because the pass algorithm explicitly overwrites its cells
                Array.Clear(_solidDepthGrid, 0, _solidDepthGrid.Length);
            }

            _lightQueue.Clear();

            // Seed light sources
            for (int localX = 0; localX < localWidth; localX++)
            {
                for (int localY = 0; localY < localHeight; localY++)
                {
                    int worldPosX = xMin + localX;
                    int worldPosY = yMin + localY;

                    var fgTd = world.FgLayer.GetTileData(worldPosX, worldPosY);
                    var bgTd = world.BgLayer.GetTileData(worldPosX, worldPosY);

                    if (fgTd.IsAir && bgTd.IsAir)
                    {
                        _lightGrid[localX, localY] = _fullBrightness;
                        _solidDepthGrid[localX, localY] = 0;
                        _lightQueue.Enqueue(new Vector2Int(localX, localY));
                    }
                }
            }

            // Propagate LightGrid
            while (_lightQueue.Count > 0)
            {
                Vector2Int curr = _lightQueue.Dequeue();
                float currLight = _lightGrid[curr.x, curr.y];

                foreach (Vector2Int dir in _directions)
                {
                    int nx = curr.x + dir.x;
                    int ny = curr.y + dir.y;

                    if (nx >= 0 && nx < localWidth && ny >= 0 && ny < localHeight)
                    {
                        int currDepth = _solidDepthGrid[curr.x, curr.y];

                        // Calculate target tile world coordinates
                        int worldNx = _lmBounds.xMin + nx;
                        int worldNy = _lmBounds.yMin + ny;

                        var fgTd = world.FgLayer.GetTileData(worldNx, worldNy);
                        var bgTd = world.BgLayer.GetTileData(worldNx, worldNy);

                        bool isSolidOrBg = fgTd.HasTile || bgTd.HasTile;

                        // Increment depth if traveling into solid/BG tile, otherwise reset
                        int nextDepth = isSolidOrBg ? currDepth + 1 : 0;

                        // Apply decay only after exceeding the buffer
                        float decay = (isSolidOrBg && nextDepth <= _decayBuffer) ? 0f : GetDecay(nx, ny, world);

                        float potentialLight = currLight - decay;
                        if (potentialLight < 0f) potentialLight = 0f;

                        // Update if potential light is brighter, or equal light with less depth used
                        if (potentialLight > _lightGrid[nx, ny] || (potentialLight == _lightGrid[nx, ny] && nextDepth < _solidDepthGrid[nx, ny]))
                        {
                            _lightGrid[nx, ny] = potentialLight;
                            _solidDepthGrid[nx, ny] = nextDepth;
                            _lightQueue.Enqueue(new Vector2Int(nx, ny));
                        }
                    }
                }
            }

            // Apply Blur Pass (Happens after propagation completes, but before texturing color bytes)
            if (_blurPasses > 0)
            {
                BlurLightGrid(localWidth, localHeight);

                // FIX: Force open-air tiles back to full brightness so the blur doesn't dim the sky!
                for (int localX = 0; localX < localWidth; localX++)
                {
                    for (int localY = 0; localY < localHeight; localY++)
                    {
                        int worldPosX = xMin + localX;
                        int worldPosY = yMin + localY;

                        var fgTd = world.FgLayer.GetTileData(worldPosX, worldPosY);
                        var bgTd = world.BgLayer.GetTileData(worldPosX, worldPosY);

                        if (fgTd.IsAir && bgTd.IsAir)
                        {
                            _lightGrid[localX, localY] = _fullBrightness;
                        }
                    }
                }
            }

            // Populate greyscale color buffer
            float fullBrightnessInv = 1f / _fullBrightness; // Multiplies are faster than division loops

            for (int localY = 0; localY < localHeight; localY++)
            {
                for (int localX = 0; localX < localWidth; localX++)
                {
                    float lightValue = _lightGrid[localX, localY];
                    float lightFraction = lightValue * fullBrightnessInv;

                    if (lightFraction > 1f) lightFraction = 1f;
                    else if (lightFraction < 0f) lightFraction = 0f;

                    byte greyScale = (byte)(lightFraction * 255f);
                    _colorBuffer[localY * localWidth + localX] = new Color32(greyScale, greyScale, greyScale, 255);
                }
            }

            // Apply lightmap overlay
            _lightmapTexture.SetPixels32(_colorBuffer);
            _lightmapTexture.Apply();

            _lightmapOverlay.texture = _lightmapTexture;
            _lightmapOverlay.material = _multiplyMaterial;

            // Update Overlay positioning
            Vector2 center = new Vector2(_lmBounds.xMin + _lmBounds.xMax, _lmBounds.yMin + _lmBounds.yMax) * 0.5f;
            Vector2 size = new(localWidth, localHeight);

            _lightmapOverlay.rectTransform.position = center;
            _lightmapOverlay.rectTransform.sizeDelta = size;
            _lightmapOverlay.rectTransform.localScale = Vector3.one;
        }

        private float GetDecay(int localX, int localY, WorldManager world)
        {
            int worldPosX = _lmBounds.xMin + localX;
            int worldPosY = _lmBounds.yMin + localY;

            var fgTd = world.FgLayer.GetTileData(worldPosX, worldPosY);
            
            if (fgTd.HasTile) return _solidFgDecay;
            
            return _baseDecay;
        }

        private void BlurLightGrid(int width, int height)
        {
            for (int pass = 0; pass < _blurPasses; pass++)
            {
                // Horizontal Pass
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float sum = _lightGrid[x, y]; int count = 1;
                        if (x > 0) { sum += _lightGrid[x - 1, y]; count++; }
                        if (x < width - 1) { sum += _lightGrid[x + 1, y]; count++; }
                        _blurGrid[x, y] = sum / count;
                    }
                }

                // Vertical Pass
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float sum = _blurGrid[x, y]; int count = 1;
                        if (y > 0) { sum += _blurGrid[x, y - 1]; count++; }
                        if (y < height - 1) { sum += _blurGrid[x, y + 1]; count++; }
                        _lightGrid[x, y] = sum / count;
                    }
                }

                // Diagonal Passes
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float sum = _lightGrid[x, y]; int count = 1;
                        if (x > 0 && y > 0) { sum += _lightGrid[x - 1, y - 1]; count++; } // Top-Left
                        if (x < width - 1 && y < height - 1) { sum += _lightGrid[x + 1, y + 1]; count++; } // Bottom-Right
                        _blurGrid[x, y] = sum / count;
                    }
                }

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float sum = _blurGrid[x, y]; int count = 1;
                        if (x < width - 1 && y > 0) { sum += _blurGrid[x + 1, y - 1]; count++; } // Top-Right
                        if (x > 0 && y < height - 1) { sum += _blurGrid[x - 1, y + 1]; count++; } // Bottom-Left
                        _lightGrid[x, y] = sum / count;
                    }
                }
            }
        }

    }
}
