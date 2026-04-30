using UnityEngine;
using System.Collections.Generic;

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