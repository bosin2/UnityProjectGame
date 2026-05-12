using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;   // BGM 전용 (Loop ON)
    [SerializeField] private AudioSource sfxSource;   // 일반 SFX (Loop OFF)

    [Header("Sound Library")]
    [SerializeField] private Sound[] bgmClips;
    [SerializeField] private Sound[] sfxClips;

    // 빠른 조회용 딕셔너리
    private Dictionary<string, AudioClip> bgmDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();

    [Header("Volume Settings")]
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
    public void PlayBGM(string name, bool fadeIn = false)
    {
        if (!bgmDict.ContainsKey(name))
        {
            Debug.LogWarning($"[AudioManager] BGM '{name}' 없냥!");
            return;
        }

        if (bgmSource.clip == bgmDict[name] && bgmSource.isPlaying) return; // 같은 곡이면 무시

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
    public void PlaySFX(string name)
    {
        if (!sfxDict.ContainsKey(name))
        {
            Debug.LogWarning($"[AudioManager] SFX '{name}' 없냥!");
            return;
        }
        sfxSource.PlayOneShot(sfxDict[name], sfxVolume * masterVolume);
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
        bgmSource.volume = bgmVolume * masterVolume;
        // sfxSource는 PlayOneShot 볼륨에서 직접 곱함
    }
}

[System.Serializable]
public class Sound
{
    public string name;       // 호출용 이름 (예: "bgm_corridor")
    public AudioClip clip;    // 실제 오디오 파일
}