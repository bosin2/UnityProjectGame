using UnityEngine;

public class FogOfWarController : MonoBehaviour
{
    [SerializeField] private Material fogMaterial;
    [SerializeField] private Transform player;

    [Header("시야 범위 (타일 수 기준)")]
    [SerializeField] private float visibleTiles = 32f;
    [SerializeField] private float gradientTiles = 8f; 

    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
        DontDestroyOnLoad(transform.root.gameObject);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
                       UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        _cam = Camera.main;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (player == null || fogMaterial == null || _cam == null) return;

        Vector3 vp = _cam.WorldToViewportPoint(player.position);
        fogMaterial.SetVector("_Center", new Vector4(vp.x, vp.y, 0, 0));

        // orthographicSize 기준으로 단순 계산냥
        float camHeight = _cam.orthographicSize * 2f;
        float innerRadius = visibleTiles / camHeight * 0.5f;
        float outerRadius = (visibleTiles + gradientTiles) / camHeight * 0.5f;

        fogMaterial.SetFloat("_InnerRadius", innerRadius);
        fogMaterial.SetFloat("_OuterRadius", outerRadius);
    }
}