using System;
using System.Collections.Generic; // Required for reusable Queue
using UnityEngine;
using UnityEngine.UI;

namespace OceanGame
{
    public class LightManager : MonoBehaviour
    {
        public static LightManager Instance { get; private set; }

        [SerializeField] private RawImage _lightmapOverlay;
        [SerializeField] private Material _multiplyMaterial;
        [SerializeField] private GameObject _lightOverlay;

        [Header("Light Settings")]
        [SerializeField] private bool _showLight = true;
        [SerializeField] private FilterMode _lightmapFilterMode;
        [SerializeField] private int _extraLightmapPadding;
        [SerializeField, Min(1f)] private float _fullBrightness;
        
        [Header("Decay")]
        [SerializeField] private float _solidFgDecay = 3f;
        [SerializeField] private float _baseDecay = 1f;
        [SerializeField] private float _waterDecay = 0.75f;

        private RectInt _lmBounds;
        private Texture2D _lightmapTexture;
        private float[,] _lightGrid;
        private Color32[] _colorBuffer;

        private Queue<Vector2Int> _lightQueue;
        private static readonly Vector2Int[] _directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        public float FullBrightness => _fullBrightness;

        private void Awake()
        {
            Instance = this;

            _lightOverlay.SetActive(_showLight);

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

            // Manage light grid and color buffer sizes safely
            if (_lightGrid == null || _lightGrid.GetLength(0) != localWidth || _lightGrid.GetLength(1) != localHeight)
            {
                _lightGrid = new float[localWidth, localHeight];
                _colorBuffer = new Color32[localWidth * localHeight];
            }
            else
            {
                Array.Clear(_lightGrid, 0, _lightGrid.Length); // Clear existing frames rather than instantiating clean arrays
            }

            _lightQueue.Clear();

            // Seed light sources
            for (int localX = 0; localX < localWidth; localX++)
            {
                for (int localY = 0; localY < localHeight; localY++)
                {
                    int worldPosX = xMin + localX;
                    int worldPosY = yMin + localY;

                    var fgTd = world.FgGrid.GetTileData(worldPosX, worldPosY);
                    var bgTd = world.BgGrid.GetTileData(worldPosX, worldPosY);

                    if (!fgTd.HasTile && !bgTd.HasTile && world.FluidGrid.GetFluidType(worldPosX, worldPosY) == FluidType.Air)
                    {
                        _lightGrid[localX, localY] = _fullBrightness;
                        _lightQueue.Enqueue(new Vector2Int(localX, localY));
                    }
                    else if (fgTd.LightLevel > 0 || bgTd.LightLevel > 0)
                    {
                        var largestLightVal = Mathf.Max(fgTd.LightLevel, bgTd.LightLevel);
                        float t = largestLightVal / 100f;
                        _lightGrid[localX, localY] = Mathf.Lerp(0, _fullBrightness, t);
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
                        // Calculate target tile world coordinates
                        float decay = GetDecay(nx, ny, world);

                        float potentialLight = currLight - decay;
                        if (potentialLight < 0f) potentialLight = 0f;

                        if (potentialLight > _lightGrid[nx, ny])
                        {
                            _lightGrid[nx, ny] = potentialLight;
                            _lightQueue.Enqueue(new Vector2Int(nx, ny));
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

            var fgTd = world.FgGrid.GetTileData(worldPosX, worldPosY);
            var fluidType = world.FluidGrid.GetFluidType(worldPosX, worldPosY);

            if (fgTd.HasTile && fgTd.TileConfig.IsSolid) return _solidFgDecay;
            if (fluidType == FluidType.Water) return _waterDecay;

            return _baseDecay;
        }

    }
}
