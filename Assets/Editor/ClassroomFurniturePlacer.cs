// Assets/Editor/ClassroomFurniturePlacer.cs
// 사용법: Unity 상단 메뉴 → Tools → Classroom Furniture Placer
// 스프라이트를 Inspector에서 연결하면 버튼 하나로 강의실 배치

using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class ClassroomFurniturePlacer : EditorWindow
{
    // ── 스프라이트 슬롯 ──────────────────────────────────────────
    public Sprite sprComputerDesk;
    public Sprite sprDesk;
    public Sprite sprPodium;
    public Sprite sprWheelChair;
    public Sprite sprMeshChair;
    public Sprite sprWhiteboard;

    // ── 강의실 종류 선택 ─────────────────────────────────────────
    public enum RoomType { ComputerLab, RegularClassroom, ModernClassroom }
    public RoomType roomType = RoomType.ComputerLab;

    // ── 배치 기준점 ──────────────────────────────────────────────
    public Vector2Int roomOrigin = new Vector2Int(0, 0);  // 방 좌하단 타일 좌표
    public int roomWidth  = 20;
    public int roomHeight = 16;

    private Vector2 scrollPos;

    [MenuItem("Tools/Classroom Furniture Placer")]
    public static void ShowWindow()
    {
        GetWindow<ClassroomFurniturePlacer>("가구 배치 도구");
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("── 스프라이트 연결 ──", EditorStyles.boldLabel);
        sprComputerDesk = (Sprite)EditorGUILayout.ObjectField("컴퓨터 책상", sprComputerDesk, typeof(Sprite), false);
        sprDesk         = (Sprite)EditorGUILayout.ObjectField("그냥 책상",   sprDesk,         typeof(Sprite), false);
        sprPodium       = (Sprite)EditorGUILayout.ObjectField("단상",        sprPodium,       typeof(Sprite), false);
        sprWheelChair   = (Sprite)EditorGUILayout.ObjectField("바퀴 의자",   sprWheelChair,   typeof(Sprite), false);
        sprMeshChair    = (Sprite)EditorGUILayout.ObjectField("메쉬 의자",   sprMeshChair,    typeof(Sprite), false);
        sprWhiteboard   = (Sprite)EditorGUILayout.ObjectField("화이트보드",  sprWhiteboard,   typeof(Sprite), false);

        EditorGUILayout.Space(8);
        GUILayout.Label("── 배치 설정 ──", EditorStyles.boldLabel);
        roomType   = (RoomType)EditorGUILayout.EnumPopup("강의실 종류", roomType);
        roomOrigin = EditorGUILayout.Vector2IntField("방 원점 (타일)", roomOrigin);
        roomWidth  = EditorGUILayout.IntField("방 너비 (타일)", roomWidth);
        roomHeight = EditorGUILayout.IntField("방 높이 (타일)", roomHeight);

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶ 가구 자동 배치", GUILayout.Height(36)))
            PlaceFurniture();

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("✕ 배치된 가구 전체 삭제", GUILayout.Height(28)))
            ClearFurniture();

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndScrollView();
    }

    // ── 타일 좌표 → 월드 좌표 변환 ──────────────────────────────
    Vector3 TileToWorld(int tx, int ty)
    {
        // PPU=16, 1타일=1유닛
        return new Vector3(roomOrigin.x + tx + 0.5f, roomOrigin.y + ty, 0f);
    }

    // ── 스프라이트 → GameObject 생성 ────────────────────────────
    GameObject CreateObj(string objName, Sprite spr, int tx, int ty,
                         float colW = 1f, float colH = 1f)
    {
        if (spr == null) { Debug.LogWarning($"{objName}: 스프라이트 없음"); return null; }

        var go = new GameObject(objName);
        go.transform.position = TileToWorld(tx, ty);
        go.tag = "Furniture";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingLayerName = "Furniture";
        sr.sortingOrder = Mathf.RoundToInt(-go.transform.position.y * 10);

        var col = go.AddComponent<BoxCollider2D>();
        col.size   = new Vector2(colW, colH);
        col.offset = new Vector2(0f, colH * 0.5f);

        // 부모 폴더링
        var parent = GameObject.Find("=== Furniture ===");
        if (parent == null) parent = new GameObject("=== Furniture ===");
        go.transform.SetParent(parent.transform);

        Undo.RegisterCreatedObjectUndo(go, "Place Furniture");
        return go;
    }

    void PlaceFurniture()
    {
        switch (roomType)
        {
            case RoomType.ComputerLab:       PlaceComputerLab();       break;
            case RoomType.RegularClassroom:  PlaceRegularClassroom();  break;
            case RoomType.ModernClassroom:   PlaceModernClassroom();   break;
        }
        Debug.Log($"[가구 배치] {roomType} 완료!");
    }

    // ══════════════════════════════════════════════════════════════
    // 컴퓨터 강의실 (이미지 2,4 참고)
    // 상단 PC 카운터 + 의자 열
    // ══════════════════════════════════════════════════════════════
    void PlaceComputerLab()
    {
        int deskCols = (roomWidth - 4) / 2;

        // 상단 PC 책상 열 (2개 행)
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < deskCols; col++)
            {
                int tx = 2 + col * 2;
                int ty = roomHeight - 4 - row * 4;
                CreateObj($"PCDesk_R{row}_C{col}", sprComputerDesk, tx, ty, 2f, 1f);
                // 의자 (책상 앞)
                CreateObj($"PCChair_R{row}_C{col}", sprWheelChair, tx, ty - 2, 1f, 1f);
            }
        }

        // 단상 (앞쪽 중앙)
        int podX = roomWidth / 2 - 1;
        CreateObj("Podium", sprPodium, podX, 2, 2f, 2f);

        // 화이트보드 (앞쪽 벽)
        CreateObj("Whiteboard", sprWhiteboard, podX - 3, 1, 3f, 0.5f);
    }

    // ══════════════════════════════════════════════════════════════
    // 일반 강의실 (이미지 5 참고)
    // 메쉬 의자 격자 배열
    // ══════════════════════════════════════════════════════════════
    void PlaceRegularClassroom()
    {
        int cols = (roomWidth - 4) / 2;
        int rows = (roomHeight - 6) / 3;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int tx = 2 + col * 2;
                int ty = roomHeight - 5 - row * 3;
                CreateObj($"Desk_R{row}_C{col}",  sprDesk,      tx, ty,   2f, 1f);
                CreateObj($"Chair_R{row}_C{col}", sprMeshChair, tx, ty-2, 1f, 1f);
            }
        }

        int podX = roomWidth / 2 - 1;
        CreateObj("Podium", sprPodium, podX, 2, 2f, 2f);
        CreateObj("Whiteboard_L", sprWhiteboard, 2, 1, 3f, 0.5f);
        CreateObj("Whiteboard_R", sprWhiteboard, podX + 2, 1, 3f, 0.5f);
    }

    // ══════════════════════════════════════════════════════════════
    // 최신 강의실 (이미지 6 참고)
    // 바퀴 의자 + 대형 TV
    // ══════════════════════════════════════════════════════════════
    void PlaceModernClassroom()
    {
        int cols = (roomWidth - 4) / 2;
        int rows = (roomHeight - 6) / 3;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int tx = 2 + col * 2;
                int ty = roomHeight - 5 - row * 3;
                CreateObj($"Desk_R{row}_C{col}",      sprDesk,       tx, ty,   2f, 1f);
                CreateObj($"WheelChair_R{row}_C{col}", sprWheelChair, tx, ty-2, 1f, 1f);
            }
        }

        int podX = roomWidth / 2 - 1;
        CreateObj("Podium",     sprPodium,     podX, 2, 2f, 2f);
        CreateObj("Whiteboard", sprWhiteboard, 2,    1, 3f, 0.5f);
    }

    void ClearFurniture()
    {
        var parent = GameObject.Find("=== Furniture ===");
        if (parent != null)
        {
            Undo.DestroyObjectImmediate(parent);
            Debug.Log("가구 전체 삭제 완료");
        }
        else Debug.Log("배치된 가구 없음");
    }
}
