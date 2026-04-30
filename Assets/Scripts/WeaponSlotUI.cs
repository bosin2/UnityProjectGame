using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotUI : MonoBehaviour
{
    public Image slotImage;
    public Sprite pipeSprite;
    public Sprite gunSprite;

    private PlayerMovement player;

    void Start()
    {
        player = FindAnyObjectByType<PlayerMovement>();
    }

    void Update()
    {
        if (player == null) return;

        slotImage.sprite = player.currentWeapon == 0
            ? pipeSprite
            : gunSprite;
    }
}