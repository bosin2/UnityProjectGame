using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 현재 장착 무기 아이콘을 표시하는 UI 컴포넌트.
/// PlayerCombat의 CurrentWeapon 값을 폴링해 아이콘을 갱신한다.
/// </summary>
public class WeaponSlotUI : MonoBehaviour
{
    // 소프트 참조
    public static WeaponSlotUI Instance { get; private set; }

    [Header("UI")]
    public Image  slotImage;
    public Sprite pipeSprite; // currentWeapon == 0
    public Sprite gunSprite;  // currentWeapon == 1

    private PlayerCombat  combat;
    private GameManager   gm;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 플레이어 오브젝트에서 PlayerCombat을 탐색
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            combat = player.GetComponent<PlayerCombat>();
    }

    void Update()
    {
        // 플레이어가 아직 없으면 재탐색
        if (combat == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) combat = player.GetComponent<PlayerCombat>();
            return;
        }

        if (GameManager.Instance == null) return;

        if (!GameManager.Instance.hasPipe && !GameManager.Instance.hasGun)
        {
            slotImage.sprite = null;
            slotImage.color  = new Color(1, 1, 1, 0);
            return;
        }

        slotImage.color  = Color.white;
        slotImage.sprite = combat.CurrentWeapon == 0 ? pipeSprite : gunSprite;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
