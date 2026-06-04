/*
 * AudioManager.cs
 * 역할: BGM과 SFX를 이름 기반으로 재생하는 전역 오디오 싱글톤입니다.
 * 연결: 문, 전투, 피격, 아이템 획득, 컷씬, 헬기 연출 등 대부분의 런타임 스크립트가 이 클래스를 호출합니다.
 * 주의: PlayBGM/PlaySFX에 넘기는 문자열은 MainMenu 씬의 AudioManager 라이브러리에 등록된 이름과 정확히 같아야 합니다.
 */using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("오디오 출력 소스")]
    [SerializeField] private AudioSource bgmSource;   // BGM 전용 (Loop ON)
    [SerializeField] private AudioSource sfxSource;   // 일반 SFX (Loop OFF)

    [Header("소리 목록")]
    [SerializeField] private Sound[] bgmClips;
    [SerializeField] private Sound[] sfxClips;

    // 이름 → AudioClip 딕셔너리: Inspector 배열을 Awake에서 한 번 구축 후 O(1) 조회
    private Dictionary<string, AudioClip> bgmDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();
    // BGM별 개별 볼륨 스케일 저장 (PlayBGM volumeScale 파라미터 기록)
    private Dictionary<string, float>     bgmVolumeScales = new Dictionary<string, float>();

    // 현재 재생 중인 BGM의 볼륨 스케일 (ApplyVolumes에서 반영)
    private float currentBgmScale = 1f;

    [Header("음량 설정")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeDictionaries();
        ApplyVolumes();
    }

    private void InitializeDictionaries()
    {
        foreach (Sound s in bgmClips)
            bgmDict[s.name] = s.clip;
        foreach (Sound s in sfxClips)
            sfxDict[s.name] = s.clip;
    }

    // === BGM 제어 ===
    public void PlayBGM(string name, bool fadeIn = false, float volumeScale = 1f)
    {
        if (!bgmDict.ContainsKey(name))
        {
            Debug.LogWarning($"[AudioManager] BGM '{name}' 없냥!");
            return;
        }

        // 같은 곡이 이미 재생 중이면 중복 재생 방지 (씬 전환 후 같은 BGM 요청 시 자연스럽게 이어짐)
        if (bgmSource.clip == bgmDict[name] && bgmSource.isPlaying) return;

        bgmVolumeScales[name] = volumeScale;
        currentBgmScale = volumeScale;
        bgmSource.volume = bgmVolume * masterVolume * currentBgmScale;
        bgmSource.clip = bgmDict[name];
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void PauseBGM() => bgmSource.Pause();
    public void ResumeBGM() => bgmSource.UnPause();

    // === SFX 제어 ===
    // PlayOneShot 사용: 동시에 여러 SFX가 겹쳐 재생 가능 (단일 sfxSource에서 오버랩 허용)
    public void PlaySFX(string name, float volumeScale = 1f)
    {
        if (!sfxDict.ContainsKey(name))
        {
            Debug.LogWarning($"[AudioManager] SFX '{name}' 없냥!");
            return;
        }
        sfxSource.PlayOneShot(sfxDict[name], sfxVolume * masterVolume * volumeScale);
    }

    /// <summary>AudioClip을 직접 재생 (AudioManager 라이브러리 등록 불필요)</summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * masterVolume * volumeScale);
    }

    // 위치 기반 SFX (몬스터 거리감 표현용)
    public void PlaySFXAtPoint(string name, Vector3 position)
    {
        if (!sfxDict.ContainsKey(name)) return;
        AudioSource.PlayClipAtPoint(sfxDict[name], position, sfxVolume * masterVolume);
    }

    // === 볼륨 ===
    public void SetMasterVolume(float v) { masterVolume = v; ApplyVolumes(); }
    public void SetBGMVolume(float v) { bgmVolume = v; ApplyVolumes(); }
    public void SetSFXVolume(float v) { sfxVolume = v; ApplyVolumes(); }

    private void ApplyVolumes()
    {
        bgmSource.volume = bgmVolume * masterVolume * currentBgmScale;
        // sfxSource는 PlayOneShot 볼륨에서 직접 곱함
    }
}

[System.Serializable]
public class Sound
{
    public string name;       // 호출용 이름 (예: "bgm_corridor")
    public AudioClip clip;    // 실제 오디오 파일
}

