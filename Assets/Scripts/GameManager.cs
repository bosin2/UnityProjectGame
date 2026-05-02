using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public int stage = 0;
    public bool hasGun = false;
    public string currentWeapon = "pipe"; 

    private HashSet<string> flags = new HashSet<string>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // 씬 로드될 때 UI 켜기
    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 메인메뉴, 인트로, 튜토리얼에서는 UI 숨기기
        if (scene.name == "MainMenu" || scene.name == "Intro" || scene.name == "Tutorial")
        {
            UICanvas.Instance.HideUI();
        }
        else
        {
            UICanvas.Instance.ShowUI();
        }
    }

    // 어떤 씬에서든 없으면 자동 생성
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("GameManager");
                obj.AddComponent<GameManager>();
            }
            return instance;
        }
    }

    public void SetFlag(string flag) => flags.Add(flag);
    public bool HasFlag(string flag) => flags.Contains(flag);
}