using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using AngryBirds.AI.BehaviourTree;

/// <summary>
/// 行为树可视化编辑器。Window → 行为树编辑器
/// 支持：拖拽创建节点 / 连线 / 参数编辑 / 保存加载 / 运行时高亮
/// </summary>
public class BTEditorWindow : EditorWindow
{
    // ---- 当前编辑的配置 ----
    private BTConfig currentConfig;
    private string configAssetPath = "Assets/Data/BT/";

    // ---- 画布 ----
    private Vector2 canvasOffset = new(300, 50);
    private Vector2 canvasDrag;
    private float canvasZoom = 1f;

    // ---- 选中 & 拖拽 ----
    private BTNodeData selectedNode;
    private string dragSourceGuid;    // 正在拖连线的父节点
    private Vector2 dragEndMousePos;

    // ---- 节点样式 ----
    private const float NodeW = 180f;
    private const float NodeH = 60f;

    // ---- 运行时高亮轮询 ----
    private float highlightPollTimer;

    // ---- 新建节点计数 ----
    private int newNodeCount;

    [MenuItem("Window/行为树编辑器")]
    public static void Open() => GetWindow<BTEditorWindow>("BT Editor").Show();

    void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.BeginHorizontal();

        DrawNodePalette();
        DrawCanvas();
        DrawInspectorPanel();

        EditorGUILayout.EndHorizontal();

        // 运行时刷新
        if (EditorApplication.isPlaying && currentConfig != null)
        {
            highlightPollTimer -= Time.unscaledDeltaTime;
            if (highlightPollTimer <= 0f)
            {
                highlightPollTimer = 0.1f;
                Repaint();
            }
        }
    }

    // ==================== 工具栏 ====================

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("新建", EditorStyles.toolbarButton, GUILayout.Width(50)))
            NewConfig();
        if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(50)))
            SaveConfig();
        if (GUILayout.Button("加载", EditorStyles.toolbarButton, GUILayout.Width(50)))
            LoadConfig();

        GUILayout.Space(10);
        if (currentConfig != null)
        {
            EditorGUILayout.LabelField($"当前: {currentConfig.name}", GUILayout.Width(200));
            EditorGUILayout.LabelField($"节点数: {currentConfig.nodes.Count}", GUILayout.Width(80));
        }
        else
        {
            EditorGUILayout.LabelField("（未加载配置 — 请点 新建 或 加载）");
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("帮助", EditorStyles.toolbarButton, GUILayout.Width(40)))
            Debug.Log("[BT Editor] 用法: 左侧拖拽创建节点 → 右键节点连线 → 右侧面板编辑参数 → 保存");

        EditorGUILayout.EndHorizontal();
    }

    // ==================== 左侧：节点面板 ====================

    void DrawNodePalette()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(160));
        EditorGUILayout.LabelField("节点面板", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        DrawCreateButton("→ Sequence", BTNodeType.Sequence, Color.green);
        DrawCreateButton("? Selector", BTNodeType.Selector, Color.yellow);
        DrawCreateButton("🔍 Condition", BTNodeType.Condition, Color.cyan);
        DrawCreateButton("⚡ Action", BTNodeType.Action, Color.magenta);

        EditorGUILayout.Space(20);
        EditorGUILayout.HelpBox("拖节点到画布空白处创建；右键节点引出连线到子节点", MessageType.Info);

        if (currentConfig != null && selectedNode != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("删除节点", EditorStyles.boldLabel);
            if (GUILayout.Button("删除选中节点"))
            {
                DeleteNode(selectedNode);
                selectedNode = null;
            }
        }

        EditorGUILayout.EndVertical();
    }

    void DrawCreateButton(string label, BTNodeType type, Color color)
    {
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = color;
        if (GUILayout.Button(label, GUILayout.Height(36)))
        {
            CreateNodeAtCenter(type);
        }
        GUI.backgroundColor = old;
    }

    void CreateNodeAtCenter(BTNodeType type)
    {
        if (currentConfig == null) NewConfig();
        var data = new BTNodeData { nodeType = type };
        // 放在画布可见区域中心
        data.editorPosition = new Vector2(
            -canvasOffset.x + position.width * 0.5f / canvasZoom,
            -canvasOffset.y + position.height * 0.3f / canvasZoom
        );
        data.editorPosition += Random.insideUnitCircle * 30f;

        // 默认参数
        if (type == BTNodeType.Condition)
        {
            data.conditionType = BTConditionType.IsPlayerInRange;
            data.conditionParam = 5f;
        }
        else if (type == BTNodeType.Action)
        {
            data.actionType = BTActionType.StandIdle;
            data.actionParam = 3f;
        }

        currentConfig.nodes.Add(data);
        newNodeCount++;
        selectedNode = data;
        EditorUtility.SetDirty(currentConfig);
    }

    void DeleteNode(BTNodeData node)
    {
        if (currentConfig == null) return;
        // 移除所有指向该节点的子引用
        foreach (var n in currentConfig.nodes)
            n.childGuids.RemoveAll(g => node.GuidMatches(g));
        currentConfig.nodes.Remove(node);
        EditorUtility.SetDirty(currentConfig);
    }

    // ==================== 中央：画布 ====================

    void DrawCanvas()
    {
        // 背景
        Rect canvasRect = GUILayoutUtility.GetRect(position.width - 460, position.height - 28);
        _canvasRect = canvasRect;
        EditorGUI.DrawRect(canvasRect, new Color(0.15f, 0.15f, 0.15f));

        if (currentConfig == null) return;

        // 缩放 + 平移
        canvasZoom = EditorGUI.Slider(new Rect(canvasRect.x + 5, canvasRect.y + 5, 120, 18), "Zoom", canvasZoom, 0.3f, 2f);

        // 中键/右键拖拽画布
        if (canvasRect.Contains(Event.current.mousePosition))
        {
            if (Event.current.type == EventType.MouseDrag && (Event.current.button == 2 || Event.current.button == 1))
            {
                canvasOffset += Event.current.delta;
                Repaint();
            }
        }

        // 裁剪
        GUI.BeginGroup(canvasRect);

        // 绘制连接线
        DrawConnections();

        // 绘制节点
        for (int i = 0; i < currentConfig.nodes.Count; i++)
        {
            BTNodeData node = currentConfig.nodes[i];
            DrawNode(canvasRect, node);
        }

        // 左键单击空白 = 取消选中
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            Vector2 mouseCanvas = (Event.current.mousePosition - canvasRect.position - canvasOffset) / canvasZoom;
            BTNodeData hit = HitTest(mouseCanvas);
            if (hit == null) selectedNode = null;
        }

        // 右键菜单：连线
        if (Event.current.type == EventType.ContextClick && currentConfig.nodes.Count > 0)
        {
            Vector2 mouseCanvas = (Event.current.mousePosition - canvasRect.position - canvasOffset) / canvasZoom;
            BTNodeData hit = HitTest(mouseCanvas);
            if (hit != null)
            {
                GenericMenu menu = new();
                BTNodeData captured = hit;
                foreach (var n in currentConfig.nodes)
                {
                    if (n != captured && n.nodeType != BTNodeType.Condition && n.nodeType != BTNodeType.Action)
                    {
                        // 只有组合节点可以有子节点
                    }
                    if (n != captured)
                        menu.AddItem(new GUIContent($"连线到: {n.DisplayName.Replace("\n", " ")}"), false,
                            () => { captured.childGuids.Add(n.guid); EditorUtility.SetDirty(currentConfig); });
                }
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("删除此连线(清空子节点)"), false,
                    () => { captured.childGuids.Clear(); EditorUtility.SetDirty(currentConfig); });
                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        GUI.EndGroup();
    }

    void DrawNode(Rect canvasRect, BTNodeData node)
    {
        float x = node.editorPosition.x * canvasZoom + canvasOffset.x;
        float y = node.editorPosition.y * canvasZoom + canvasOffset.y;
        float w = NodeW * canvasZoom;
        float h = NodeH * canvasZoom;
        Rect r = new(canvasRect.x + x, canvasRect.y + y, w, h);

        // 运行时高亮
        bool isActive = EditorApplication.isPlaying
                     && currentConfig != null
                     && currentConfig.activeNodeGuid == node.guid;

        Color bg = node.nodeType switch
        {
            BTNodeType.Sequence  => new Color(0.15f, 0.5f, 0.15f),
            BTNodeType.Selector  => new Color(0.5f, 0.5f, 0.1f),
            BTNodeType.Condition => new Color(0.1f, 0.4f, 0.5f),
            BTNodeType.Action    => new Color(0.5f, 0.15f, 0.5f),
            _                    => Color.gray,
        };

        if (isActive) bg = Color.Lerp(bg, Color.green, 0.6f);
        if (node == selectedNode) bg = Color.Lerp(bg, Color.white, 0.15f);

        EditorGUI.DrawRect(r, bg);

        // 标签
        var style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontSize = Mathf.RoundToInt(11 * canvasZoom),
        };
        GUI.Label(r, node.DisplayName, style);

        // 子节点数标记
        if (node.childGuids.Count > 0)
        {
            var badgeRect = new Rect(r.x + r.width - 22, r.y + 2, 20, 16);
            EditorGUI.DrawRect(badgeRect, new Color(0, 0, 0, 0.5f));
            GUI.Label(badgeRect, node.childGuids.Count.ToString(),
                new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.white } });
        }

        // 点击选中
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && r.Contains(Event.current.mousePosition))
        {
            selectedNode = node;
        }

        // 拖拽移动
        if (Event.current.type == EventType.MouseDrag && Event.current.button == 0 && selectedNode == node)
        {
            node.editorPosition += Event.current.delta / canvasZoom;
            EditorUtility.SetDirty(currentConfig);
            Repaint();
        }
    }

    BTNodeData HitTest(Vector2 mouseCanvasPos)
    {
        if (currentConfig == null) return null;
        for (int i = currentConfig.nodes.Count - 1; i >= 0; i--)
        {
            var n = currentConfig.nodes[i];
            Rect r = new(n.editorPosition.x, n.editorPosition.y, NodeW, NodeH);
            if (r.Contains(mouseCanvasPos)) return n;
        }
        return null;
    }

    Vector2 NodeScreenPos(BTNodeData node)
    {
        // canvasRect 每次 OnGUI 都不同，存为字段在 DrawCanvas 开头更新
        return _canvasRect.position + node.editorPosition * canvasZoom + canvasOffset;
    }
    private Rect _canvasRect; // 在 DrawCanvas 开头赋值

    void DrawConnections()
    {
        if (currentConfig == null) return;

        Handles.BeginGUI();
        foreach (var node in currentConfig.nodes)
        {
            foreach (string childGuid in node.childGuids)
            {
                BTNodeData child = currentConfig.FindByGuid(childGuid);
                if (child == null) continue;

                Vector2 parentCenter = NodeScreenPos(node) + new Vector2(NodeW * canvasZoom * 0.5f, NodeH * canvasZoom);
                Vector2 childCenter  = NodeScreenPos(child) + new Vector2(NodeW * canvasZoom * 0.5f, 0);

                bool isActive = EditorApplication.isPlaying
                    && (node.guid == currentConfig.activeNodeGuid
                     || child.guid == currentConfig.activeNodeGuid);
                Color lineColor = isActive
                    ? Color.green : new Color(0.4f, 0.4f, 0.4f, 0.8f);
                float thickness = isActive ? 3f : 2f;

                Handles.DrawBezier(parentCenter, childCenter,
                    parentCenter + Vector2.down * 40f * canvasZoom,
                    childCenter  + Vector2.up * 40f * canvasZoom,
                    lineColor, null, thickness);
            }
        }
        Handles.EndGUI();
    }

    Vector2 NodeBottomCenter(BTNodeData node) => node.editorPosition + new Vector2(0, NodeH);
    Vector2 NodeTopCenter(BTNodeData node)    => node.editorPosition;

    // ==================== 右侧：属性面板 ====================

    void DrawInspectorPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(280));
        EditorGUILayout.LabelField("属性面板", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (currentConfig == null || selectedNode == null)
        {
            EditorGUILayout.HelpBox("选中一个节点以编辑参数", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        BTNodeData n = selectedNode;
        EditorGUILayout.LabelField($"GUID: {n.guid}");
        n.comment = EditorGUILayout.TextField("备注", n.comment);
        EditorGUILayout.Space(5);

        if (n.nodeType == BTNodeType.Condition)
        {
            EditorGUILayout.LabelField("条件类型", EditorStyles.boldLabel);
            n.conditionType = (BTConditionType)EditorGUILayout.EnumPopup("类型", n.conditionType);
            n.conditionParam = EditorGUILayout.FloatField("参数(距离/百分比/冷却)", n.conditionParam);
        }
        else if (n.nodeType == BTNodeType.Action)
        {
            EditorGUILayout.LabelField("动作类型", EditorStyles.boldLabel);
            n.actionType = (BTActionType)EditorGUILayout.EnumPopup("类型", n.actionType);
            n.actionParam = EditorGUILayout.FloatField("参数(速度等)", n.actionParam);
            n.actionParamInt = EditorGUILayout.IntField("整数参数(伤害)", n.actionParamInt);
        }
        else
        {
            EditorGUILayout.LabelField("组合节点", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"子节点数: {n.childGuids.Count}");
            if (GUILayout.Button("清空子节点连线"))
            {
                n.childGuids.Clear();
                EditorUtility.SetDirty(currentConfig);
            }
        }

        EditorGUILayout.EndVertical();
    }

    // ==================== 文件操作 ====================

    void NewConfig()
    {
        string path = EditorUtility.SaveFilePanelInProject("新建行为树配置", "NewBT.asset", "asset",
            "选择保存位置", configAssetPath);
        if (string.IsNullOrEmpty(path)) return;

        var config = CreateInstance<BTConfig>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        currentConfig = config;
        selectedNode = null;
        configAssetPath = System.IO.Path.GetDirectoryName(path) + "/";
        Debug.Log($"[BT Editor] 已创建: {path}");
    }

    void SaveConfig()
    {
        if (currentConfig == null) return;
        EditorUtility.SetDirty(currentConfig);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BT Editor] 已保存: {AssetDatabase.GetAssetPath(currentConfig)}");
    }

    void LoadConfig()
    {
        string path = EditorUtility.OpenFilePanel("加载行为树配置", configAssetPath, "asset");
        if (string.IsNullOrEmpty(path)) return;

        // 转换为相对路径
        string dataPath = Application.dataPath;
        if (path.StartsWith(dataPath))
            path = "Assets" + path.Substring(dataPath.Length);

        var config = AssetDatabase.LoadAssetAtPath<BTConfig>(path);
        if (config == null)
        {
            EditorUtility.DisplayDialog("加载失败", $"无法从路径加载 BTConfig:\n{path}", "确定");
            return;
        }
        currentConfig = config;
        selectedNode = null;
        configAssetPath = System.IO.Path.GetDirectoryName(path) + "/";
        Debug.Log($"[BT Editor] 已加载: {path} (节点数: {config.nodes.Count})");
    }
}
