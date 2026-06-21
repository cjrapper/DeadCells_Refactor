using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 一键重构关卡：经典平台跳跃 + 独立单向平台 + 匹配背景尺寸
/// 菜单位置：Tools → 一键重构关卡
/// </summary>
public class LevelDesigner
{
    private const int MapLeft = -18;
    private const int MapRight = 18;
    private const int GroundY = -4;
    private const string GroundTilemapName = "Tilemap";
    private const string PlatformsLayerName = "OneWayPlatform";

    [MenuItem("Tools/一键重构关卡")]
    public static void RebuildLevel()
    {
        TileBase tile = GetTileAsset();
        if (tile == null)
        {
            Debug.LogError("[LevelDesigner] 找不到 Tile 资产，操作终止");
            return;
        }

        RebuildGround(tile);
        RebuildPlatforms();
        OrganizeScene();
        RepositionGameObjects();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[LevelDesigner] 关卡重构完成！Ctrl+S 保存");
    }

    // ==================== 瓦片获取 ====================

    private static TileBase GetTileAsset()
    {
        GameObject tilemapObj = GameObject.Find(GroundTilemapName);
        if (tilemapObj != null)
        {
            Tilemap tm = tilemapObj.GetComponent<Tilemap>();
            if (tm != null)
            {
                foreach (var pos in tm.cellBounds.allPositionsWithin)
                {
                    TileBase t = tm.GetTile(pos);
                    if (t != null) return t;
                }
            }
        }

        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Square.asset");
        if (tile != null)
        {
            Debug.Log("[LevelDesigner] 从 Assets/Tiles/Square.asset 加载瓦片");
            return tile;
        }

        string[] guids = AssetDatabase.FindAssets("t:Tile");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TileBase t = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (t != null)
            {
                Debug.Log($"[LevelDesigner] 兜底找到瓦片: {path}");
                return t;
            }
        }

        return null;
    }

    // ==================== 地面 ====================

    private static void RebuildGround(TileBase tile)
    {
        GameObject go = GameObject.Find(GroundTilemapName);
        if (go == null) return;

        Tilemap tilemap = go.GetComponent<Tilemap>();
        if (tilemap == null) return;

        tilemap.ClearAllTiles();
        FillRow(tilemap, tile, MapLeft, MapRight, GroundY);
        FillRow(tilemap, tile, MapLeft, MapLeft + 1, GroundY + 1);
        FillRow(tilemap, tile, MapRight - 1, MapRight, GroundY + 1);
        tilemap.CompressBounds();
        Debug.Log($"[LevelDesigner] 地面: {MapLeft}~{MapRight}, y={GroundY}");
    }

    // ==================== 浮空单向平台（独立 GameObject） ====================

    private static void RebuildPlatforms()
    {
        // 清理旧的
        foreach (string name in new[] { "Platforms", "-Platforms-",
                                        "OneWayPlatform (1)", "OnwayPlatform (1)" })
        {
            GameObject old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        Sprite sprite = GetGroundSprite();
        Color color = GetGroundColor();
        if (sprite == null)
        {
            Debug.LogError("[LevelDesigner] 无法获取平台 Sprite，跳过");
            return;
        }

        // 父节点
        GameObject parent = new GameObject("-Platforms-");
        Undo.RegisterCreatedObjectUndo(parent, "Create -Platforms-");
        GameObject env = GameObject.Find("-Environment-");
        if (env != null) parent.transform.SetParent(env.transform);

        int layer = LayerMask.NameToLayer(PlatformsLayerName);

        // (y, xLeft, xRight) — 3 层，间距 3 格
        var defs = new (float y, int xL, int xR)[] {
            (-1f, -16, -4),
            (-1f,   4, 16),
            ( 2f, -14,  0),
            ( 2f,   2, 14),
            ( 5f,  -8,  8),
        };

        foreach (var (y, xL, xR) in defs)
        {
            float w = xR - xL + 1;
            float cx = (xL + xR) / 2f;

            GameObject p = new GameObject($"Platform_y{y}_x{xL}");
            Undo.RegisterCreatedObjectUndo(p, "Create platform");
            p.transform.SetParent(parent.transform);
            p.transform.position = new Vector3(cx, y, 0);
            p.layer = layer;
            p.tag = "OneWayPlatform";

            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(w, 1);
            sr.color = color;
            sr.sortingLayerName = "Environment";

            var bc = p.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(w, 1);
            bc.usedByEffector = true;

            var eff = p.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 180;

            var rb = p.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
        }

        Debug.Log($"[LevelDesigner] 已创建 {defs.Length} 个单向平台");
    }

    private static Sprite GetGroundSprite()
    {
        GameObject go = GameObject.Find(GroundTilemapName);
        if (go != null)
        {
            var tm = go.GetComponent<Tilemap>();
            if (tm != null)
            {
                foreach (var pos in tm.cellBounds.allPositionsWithin)
                {
                    TileBase t = tm.GetTile(pos);
                    if (t is Tile tile && tile.sprite != null) return tile.sprite;
                }
            }
        }
        TileBase asset = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Square.asset");
        if (asset is Tile t2 && t2.sprite != null) return t2.sprite;
        return null;
    }

    private static Color GetGroundColor()
    {
        GameObject go = GameObject.Find(GroundTilemapName);
        if (go != null)
        {
            var tm = go.GetComponent<Tilemap>();
            if (tm != null) return tm.color;
        }
        return Color.white;
    }

    // ==================== 场景整理 ====================

    private static void OrganizeScene()
    {
        // 对象池归组
        string[] poolNames = { " ProjectilePool", "ProjectilePool", "JumpDustPool",
                               "landDustPool", "LandDustPool", "HitEffectPool",
                               "GohstPool", "GhostPool" };
        GameObject poolsParent = GameObject.Find("-Pools-");
        if (poolsParent == null)
        {
            poolsParent = new GameObject("-Pools-");
            poolsParent.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(poolsParent, "Create -Pools-");
        }
        foreach (string name in poolNames)
        {
            GameObject pool = GameObject.Find(name);
            if (pool != null && pool.transform.parent == null)
                Undo.SetTransformParent(pool.transform, poolsParent.transform, "Reparent");
        }

        // 命名修正
        var renames = new Dictionary<string, string> {
            { "GohstPool", "GhostPool" },
            { "landDustPool", "LandDustPool" },
            { " ProjectilePool", "ProjectilePool" },
        };
        foreach (var kv in renames)
        {
            GameObject go = GameObject.Find(kv.Key);
            if (go != null) { Undo.RecordObject(go, "Rename"); go.name = kv.Value; }
        }

        // AddressablesLoader (替代已删除的 ABManager)
        if (Object.FindObjectOfType<AngryBirds.Loading.AddressablesLoader>() == null)
        {
            GameObject gm = GameObject.Find("GameManager");
            if (gm == null) { gm = new GameObject("GameManager"); Undo.RegisterCreatedObjectUndo(gm, "Create GameManager"); }
            Undo.AddComponent<AngryBirds.Loading.AddressablesLoader>(gm);
        }
    }

    // ==================== 位置调整 ====================

    private static void RepositionGameObjects()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Undo.RecordObject(player.transform, "Move Player");
            player.transform.position = new Vector3(-4f, GroundY + 1.5f, player.transform.position.z);
        }

        GameObject dummy = GameObject.Find("DummyEnemy_BT");
        if (dummy != null)
        {
            Undo.RecordObject(dummy.transform, "Move DummyEnemy");
            dummy.transform.position = new Vector3(5f, GroundY + 1.5f, 0f);
        }

        GameObject bounds = GameObject.Find("Bounds");
        if (bounds != null)
        {
            PolygonCollider2D poly = bounds.GetComponent<PolygonCollider2D>();
            if (poly != null)
            {
                Undo.RecordObject(poly, "Update Bounds");
                poly.pathCount = 1;
                poly.SetPath(0, new Vector2[] {
                    new(MapLeft - 2, GroundY - 2),
                    new(MapRight + 2, GroundY - 2),
                    new(MapRight + 2, 8),
                    new(MapLeft - 2, 8),
                });
            }
        }
    }

    // ==================== 工具 ====================

    private static void FillRow(Tilemap tilemap, TileBase tile, int xFrom, int xTo, int y)
    {
        for (int x = xFrom; x <= xTo; x++)
            tilemap.SetTile(new Vector3Int(x, y, 0), tile);
    }
}
