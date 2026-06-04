/*
 * SceneBGMPlayer.cs
 * 역할: 씬 시작 시 지정된 BGM을 AudioManager를 통해 재생하는 씬 단위 음악 트리거입니다.
 * 연결: 각 씬의 BGM 오브젝트에 붙어 AudioManager.PlayBGM을 호출합니다.
 * 주의: 특수 몬스터 BGM처럼 임시로 음악을 바꾸는 시스템과 겹칠 수 있으므로 씬 진입/이탈 타이밍을 확인해야 합니다.
 */using UnityEngine;

// 씬이 시작될 때 지정된 BGM을 자동 재생.
// 각 씬에 하나씩 빈 GameObject(BGMPlayer)에 붙여서 사용.
public class SceneBGMPlayer : MonoBehaviour
{
    [Header("이 씬에서 재생할 BGM 이름")]
    [SerializeField] private string bgmName;

    [Range(0f, 1f)]
    [SerializeField] private float volumeScale = 1f;

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

        AudioManager.Instance.PlayBGM(bgmName, volumeScale: volumeScale);
    }
}


