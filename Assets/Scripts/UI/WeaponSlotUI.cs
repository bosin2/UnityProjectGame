using UnityEngine;
using UnityEngine.UI;

// PlayerMovement의 현재 무기 상태에 따라 핫바 무기 슬롯 아이콘을 갱신하는 컴포넌트
public class WeaponSlotUI : MonoBehaviour
{
    public static WeaponSlotUI Instance;
    public Image slotImage;
    public Sprite pipeSprite; // currentWeapon == 0 (파이프)
    public Sprite gunSprite;  // currentWeapon == 1 (권총)

    private PlayerMovement player;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        player = FindAnyObjectByType<PlayerMovement>();
    }

    // 매 프레임 플레이어의 currentWeapon 값에 맞춰 아이콘 스프라이트 변경
    void Update()
    {
        if (player == null) return;
        if (GameManager.Instance == null) return; // GameManager 초기화 전 접근 방지

        // 무기 없으면 슬롯 투명하게
        if (!GameManager.Instance.hasPipe && !GameManager.Instance.hasGun)
        {
            slotImage.sprite = null;
            slotImage.color = new Color(1, 1, 1, 0);
            return;
        }

        slotImage.color = new Color(1, 1, 1, 1);
        slotImage.sprite = player.currentWeapon == 0
            ? pipeSprite
            : gunSprite;
    }
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
