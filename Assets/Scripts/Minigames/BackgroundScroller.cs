using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally tiles a background sprite downward as the camera follows the Lampa.
/// Tiles are recycled via an object pool — no allocations during gameplay.
/// Attach to any GameObject and assign the sprite in the Inspector.
/// The original stretched Background object should be disabled or deleted.
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("Camera to track. Defaults to Camera.main if left empty.")]
    public Camera targetCamera;

    [Header("Tile Appearance")]
    [Tooltip("Sprite used for every background tile.")]
    public Sprite tileSprite;
    [Tooltip("Color tint applied to all tiles.")]
    public Color tileColor = Color.white;
    [Tooltip("Optional material override. Leave empty to use the default sprite material.")]
    public Material tileMaterial;

    [Header("Tile Size (world units)")]
    [Tooltip("Height of a single tile in world units. " +
             "Default matches the native sprite size at 100 PPU (1024 px / 100 = 10.24).")]
    public float tileHeight = 10.24f;
    [Tooltip("Width of a single tile in world units.")]
    public float tileWidth = 10.24f;

    [Header("Spawn Buffer")]
    [Tooltip("How many extra tiles to keep ready below the camera's bottom edge. " +
             "Raise if gaps appear at high fall speeds.")]
    [Min(1)]
    public int tilesAhead = 3;
    [Tooltip("How many tiles to keep above the camera before recycling them.")]
    [Min(1)]
    public int tilesAbove = 1;

    [Header("Rendering")]
    public string sortingLayerName = "Default";
    public int sortingOrder = -1;

    // Active tiles ordered top (first) → bottom (last).
    private readonly LinkedList<GameObject> activeTiles = new LinkedList<GameObject>();
    private readonly Stack<GameObject> pool             = new Stack<GameObject>();

    private Camera cam;
    private float camHalfHeight;

    private void Awake()
    {
        cam = targetCamera != null ? targetCamera : Camera.main;
    }

    private void Start()
    {
        camHalfHeight = cam.orthographicSize;
        SpawnInitialTiles();
    }

    private void LateUpdate()
    {
        float camY = cam.transform.position.y;

        // Threshold below which new tiles must exist.
        float spawnThreshold  = camY - camHalfHeight - tilesAhead * tileHeight;
        // Threshold above which tiles can be recycled.
        float recycleThreshold = camY + camHalfHeight + tilesAbove * tileHeight;

        // Spawn downward until covered.
        while (BottomY() > spawnThreshold)
            SpawnAt(BottomY() - tileHeight, toBottom: true);

        // Recycle tiles that scrolled too far above.
        while (activeTiles.Count > 0 && TopY() > recycleThreshold)
            RecycleTop();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private float TopY()    => activeTiles.First?.Value.transform.position.y ?? float.MinValue;
    private float BottomY() => activeTiles.Last?.Value.transform.position.y  ?? float.MaxValue;

    private void SpawnInitialTiles()
    {
        float camY = cam.transform.position.y;

        // Align to grid so tiles never partially overlap the top of the view.
        float topY = Mathf.Ceil((camY + camHalfHeight + tilesAbove * tileHeight) / tileHeight)
                     * tileHeight;

        int tilesNeeded = tilesAbove
                        + Mathf.CeilToInt(camHalfHeight * 2f / tileHeight)
                        + tilesAhead
                        + 2; // safety margin

        for (int i = 0; i < tilesNeeded; i++)
            SpawnAt(topY - i * tileHeight, toBottom: true);
    }

    private void SpawnAt(float worldY, bool toBottom)
    {
        GameObject tile = pool.Count > 0 ? pool.Pop() : CreateTile();
        tile.transform.position = new Vector3(transform.position.x, worldY, transform.position.z);
        tile.SetActive(true);

        if (toBottom) activeTiles.AddLast(tile);
        else          activeTiles.AddFirst(tile);
    }

    private void RecycleTop()
    {
        GameObject tile = activeTiles.First.Value;
        activeTiles.RemoveFirst();
        tile.SetActive(false);
        pool.Push(tile);
    }

    private GameObject CreateTile()
    {
        var go = new GameObject("BgTile");
        go.transform.SetParent(transform, worldPositionStays: false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite           = tileSprite;
        sr.color            = tileColor;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder     = sortingOrder;

        if (tileMaterial != null)
            sr.sharedMaterial = tileMaterial;

        // Scale the tile so it fills exactly tileWidth × tileHeight in world space.
        if (tileSprite != null)
        {
            float ppu     = tileSprite.pixelsPerUnit;
            float nativeW = tileSprite.rect.width  / ppu;
            float nativeH = tileSprite.rect.height / ppu;
            go.transform.localScale = new Vector3(tileWidth / nativeW, tileHeight / nativeH, 1f);
        }

        return go;
    }
}
