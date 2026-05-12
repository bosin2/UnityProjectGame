using UnityEngine;

public class AudioTest : MonoBehaviour
{
    void Start()
    {
        // BGM 재생 테스트
        AudioManager.Instance.PlayBGM("mainmenu");
    }

    void Update()
    {
        // 스페이스바 누르면 효과음 테스트
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AudioManager.Instance.PlaySFX("swing");
        }
    }
}