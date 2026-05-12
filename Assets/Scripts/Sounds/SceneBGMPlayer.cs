using UnityEngine;

// 씬이 시작될 때 지정된 BGM을 자동 재생.
// 각 씬에 하나씩 빈 GameObject(BGMPlayer)에 붙여서 사용.
public class SceneBGMPlayer : MonoBehaviour
{
    [Header("이 씬에서 재생할 BGM 이름")]
    [SerializeField] private string bgmName;

    [Header("옵션")]
    [Tooltip("씬 시작 시 자동 재생 여부")]
    [SerializeField] private bool playOnStart = true;

    void Start()
    {
        if (!playOnStart) return;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SceneBGMPlayer] AudioManager가 없");
            return;
        }

        if (string.IsNullOrEmpty(bgmName))
        {
            Debug.LogWarning("[SceneBGMPlayer] BGM 이름이 비어있");
            return;
        }

        AudioManager.Instance.PlayBGM(bgmName);
    }
}