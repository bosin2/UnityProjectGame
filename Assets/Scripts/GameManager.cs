using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 게임 전반의 상태(스테이지, 무기, 이벤트 플래그)를 관리하는 영구 싱글톤
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool hasGun = false;
    public bool hasPipe = false;
    public int stage = 0;
    public string currentWeapon = "pipe";

    // 이벤트 진행 여부를 문자열 키로 추적하는 플래그 집합
    private HashSet<string> flags = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬 전환 시 메뉴/인트로/튜토리얼에서는 HUD 숨기고, 게임 씬에서는 표시
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 인트로나 튜토리얼 첫 진입시 SpawnPos 초기화
        if (scene.name == "Intro" || scene.name == "Tutorial")
        {
            PlayerPrefs.DeleteKey("SpawnX");
            PlayerPrefs.DeleteKey("SpawnY");
            PlayerPrefs.DeleteKey("SpawnDirX");
            PlayerPrefs.DeleteKey("SpawnDirY");
        }

        if (scene.name == "MainMenu" || scene.name == "Intro")
            UICanvas.Instance?.HideUI();
        else
            UICanvas.Instance?.ShowUI();
    }

    // 플래그 등록
    public void SetFlag(string flag) => flags.Add(flag);

    // 플래그 보유 여부 반환
    public bool HasFlag(string flag) => flags.Contains(flag);
}
