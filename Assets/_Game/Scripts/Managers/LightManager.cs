using System;
using System.Collections.Generic;
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
        [SerializeField] private int _lightCellsPerTile;
        [SerializeField, Min(1f)] private float _fullBrightness;
        [SerializeField] private bool _enableBlurPass = true;
        [Tooltip("Pushes light values away from the midpoint toward dark or full brightness. Set to 1 for no adjustment.")]
        [SerializeField, Range(1f, 3f)] private float _lightContrast = 1.2f;
        
        [Header("Light Transmission")]
        [Tooltip("Light diminished after entering a light cell occupied by a solid foreground tile.")]
        [SerializeField] private float _solidFgTransmission = 0.56f;
        
        [Tooltip("Light diminished after entering empty space, water, or a non-solid foreground tile.")]
        [SerializeField] private float _permeableTransmission = 0.88f;

        private RectInt _lmBounds;
        private Texture2D _lightmapTexture;
        private Color32[] _colorBuffer;
        private float[,] _lightGrid;
        private float[,] _blurGrid;

        private Queue<Vector2Int> _lightQueue;
        private static readonly Vector2Int[] _directions = { new(1, 1), new(-1, 1), new(1, -1), new(-1, -1), Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        public float FullBrightness => _fullBrightness;

        private void Awake()
        {
            Instance = this;

            _lightOverlay.SetActive(_showLight);

            _lightQueue = new Queue<Vector2Int>(100 * 100 * _lightCellsPerTile * _lightCellsPerTile);
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

            int tileWidth = _lmBounds.width;
            int tileHeight = _lmBounds.height;
            int lightmapWidth = tileWidth * _lightCellsPerTile;
            int lightmapHeight = tileHeight * _lightCellsPerTile;

            if (_lightmapTexture == null) // Initialize if new
            {
                _lightmapTexture = new(lightmapWidth, lightmapHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = _lightmapFilterMode,
                    wrapMode = TextureWrapMode.Clamp
                };
            }
            else if (_lightmapTexture.width != lightmapWidth || _lightmapTexture.height != lightmapHeight) // dynamically set it when camera changes dimensions
            {
                _lightmapTexture.Reinitialize(lightmapWidth, lightmapHeight);
                _lightmapTexture.filterMode = _lightmapFilterMode;
                _lightmapTexture.wrapMode = TextureWrapMode.Clamp;
            }

            // Manage light grid and color buffer sizes safely
            if (_lightGrid == null || _lightGrid.GetLength(0) != lightmapWidth || _lightGrid.GetLength(1) != lightmapHeight)
            {
                _lightGrid = new float[lightmapWidth, lightmapHeight];
                _blurGrid = new float[lightmapWidth, lightmapHeight];
                _colorBuffer = new Color32[lightmapWidth * lightmapHeight];
            }
            else
            {
                Array.Clear(_lightGrid, 0, _lightGrid.Length); // Clear existing frames rather than instantiating clean arrays
            }

            _lightQueue.Clear();

            // Seed light sources
            for (int localX = 0; localX < tileWidth; localX++)
            {
                for (int localY = 0; localY < tileHeight; localY++)
                {
                    int worldPosX = xMin + localX;
                    int worldPosY = yMin + localY;

                    var fgTd = world.FgGrid.GetTileData(worldPosX, worldPosY);
                    var bgTd = world.BgGrid.GetTileData(worldPosX, worldPosY);

                    if (((fgTd.HasTile && !fgTd.IsSolid) || !fgTd.HasTile) && !bgTd.HasTile && world.FluidGrid.GetFluidType(worldPosX, worldPosY) == FluidType.Air)
                    {
                        SeedTile(localX, localY, _fullBrightness);
                    }
                    else if (fgTd.LightLevel > 0 || bgTd.LightLevel > 0)
                    {
                        float sourceStrength = Mathf.Max(fgTd.LightLevel, bgTd.LightLevel) / 100f;
                        SeedTile(localX, localY, _fullBrightness * Mathf.Clamp01(sourceStrength));
                    }
                }
            }

            // Propagate LightGrid
            while (_lightQueue.Count > 0)
            {
                Vector2Int curr = _lightQueue.Dequeue();
                float currLight = _lightGrid[curr.x, curr.y];

                if (currLight <= 0) continue;
                
                foreach (Vector2Int dir in _directions)
                {
                    int nx = curr.x + dir.x;
                    int ny = curr.y + dir.y;
                    
                    if (nx >= 0 && nx < lightmapWidth && ny >= 0 && ny < lightmapHeight)
                    {
                        float highestAdjacentLight = GetHighestAdjacentLight(nx, ny, lightmapWidth, lightmapHeight);
                        float transmission = GetTransmission(nx, ny, world);
                        float potentialLight = highestAdjacentLight - transmission;

                        if (potentialLight <= 0) continue;

                        if (potentialLight > _lightGrid[nx, ny])
                        {
                            _lightGrid[nx, ny] = potentialLight;
                            _lightQueue.Enqueue(new Vector2Int(nx, ny));
                        }
                    }
                }
            }

            if(_enableBlurPass)
            {
                BlurLightGrid(lightmapWidth, lightmapHeight);
            }

            float fullBrightnessInv = 1f / _fullBrightness; // Multiplies are faster than division loops

            // Increase contrast around the midpoint before converting the grid to greyscale.
            if (_lightContrast > 1f)
            {
                for (int localY = 0; localY < lightmapHeight; localY++)
                {
                    for (int localX = 0; localX < lightmapWidth; localX++)
                    {
                        float lightFraction = Mathf.Clamp01(_lightGrid[localX, localY] * fullBrightnessInv);
                        float darkPower = Mathf.Pow(lightFraction, _lightContrast);
                        float brightPower = Mathf.Pow(1f - lightFraction, _lightContrast);
                        _lightGrid[localX, localY] = _fullBrightness * (darkPower / (darkPower + brightPower));
                    }
                }
            }

            // Populate greyscale color buffer
            for (int localY = 0; localY < lightmapHeight; localY++)
            {
                for (int localX = 0; localX < lightmapWidth; localX++)
                {
                    float lightValue = _lightGrid[localX, localY];
                    float lightFraction = lightValue * fullBrightnessInv;

                    if (lightFraction > 1f) lightFraction = 1f;
                    else if (lightFraction < 0f) lightFraction = 0f;

                    byte greyScale = (byte)(lightFraction * 255f);
                    _colorBuffer[localY * lightmapWidth + localX] = new Color32(greyScale, greyScale, greyScale, 255);
                }
            }

            // Apply lightmap overlay
            _lightmapTexture.SetPixels32(_colorBuffer);
            _lightmapTexture.Apply();

            _lightmapOverlay.texture = _lightmapTexture;
            _lightmapOverlay.material = _multiplyMaterial;

            // Update Overlay positioning
            Vector2 center = new Vector2(_lmBounds.xMin + _lmBounds.xMax, _lmBounds.yMin + _lmBounds.yMax) * 0.5f;
            Vector2 size = new(tileWidth, tileHeight);

            _lightmapOverlay.rectTransform.position = center;
            _lightmapOverlay.rectTransform.sizeDelta = size;
            _lightmapOverlay.rectTransform.localScale = Vector3.one;
        }

        private void SeedTile(int localTileX, int localTileY, float lightValue)
        {
            int lightX = localTileX * _lightCellsPerTile;
            int lightY = localTileY * _lightCellsPerTile;

            for (int x = 0; x < _lightCellsPerTile; x++)
            {
                for (int y = 0; y < _lightCellsPerTile; y++)
                {
                    Vector2Int lightPosition = new(lightX + x, lightY + y);
                    _lightGrid[lightPosition.x, lightPosition.y] = lightValue;
                    _lightQueue.Enqueue(lightPosition);
                }
            }
        }

        private float GetHighestAdjacentLight(int x, int y, int width, int height)
        {
            float highestLight = 0f;

            if (x > 0) highestLight = Mathf.Max(highestLight, _lightGrid[x - 1, y]); // Left
            if (x < width - 1) highestLight = Mathf.Max(highestLight, _lightGrid[x + 1, y]); // Right
            if (y > 0) highestLight = Mathf.Max(highestLight, _lightGrid[x, y - 1]); // Up
            if (y < height - 1) highestLight = Mathf.Max(highestLight, _lightGrid[x, y + 1]); // Down

            return highestLight;
        }

        private float GetTransmission(int lightX, int lightY, WorldManager world)
        {
            int worldPosX = _lmBounds.xMin + (lightX / _lightCellsPerTile);
            int worldPosY = _lmBounds.yMin + (lightY / _lightCellsPerTile);

            var fgTd = world.FgGrid.GetTileData(worldPosX, worldPosY);

            return fgTd.HasTile && fgTd.IsSolid ? _solidFgTransmission : _permeableTransmission;
        }

        private void BlurLightGrid(int width, int height)
        {
            // Horizontal pass
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = _lightGrid[x, y];
                    int count = 1;

                    if (x > 0)
                    {
                        sum += _lightGrid[x - 1, y];
                        count++;
                    }

                    if (x < width - 1)
                    {
                        sum += _lightGrid[x + 1, y];
                        count++;
                    }

                    _blurGrid[x, y] = sum / count;
                }
            }

            // Vertical pass
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float sum = _blurGrid[x, y];
                    int count = 1;

                    if (y > 0)
                    {
                        sum += _blurGrid[x, y - 1];
                        count++;
                    }

                    if (y < height - 1)
                    {
                        sum += _blurGrid[x, y + 1];
                        count++;
                    }

                    _lightGrid[x, y] = sum / count;
                }
            }
        }

    }
}
