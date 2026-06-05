using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Reusable interaction event for: choice callback -> image popup -> follow-up dialogue.
/// Attach this to the special Interactable object and call Play() from DialoguePhase.onChoiceYes/onChoiceNo.
/// </summary>
public class DialogueEvent : MonoBehaviour
{
    [Header("Popup Content")]
    [SerializeField] private Sprite popupSprite;
    [SerializeField] private string popupTitle = "";
    [SerializeField] private string closeHint = "Space / Esc";

    [Header("Follow-up Dialogue")]
    [TextArea(2, 5)]
    [SerializeField] private string[] afterPopupLines;

    [Header("Behavior")]
    [SerializeField] private bool showOnlyOnce = true;
    [SerializeField] private bool pauseWhileOpen = true;
    [SerializeField] private bool closeWithSpace = true;
    [SerializeField] private bool closeWithEscape = true;
    [SerializeField] private string doneFlag = "";

    [Header("Audio")]
    [SerializeField] private AudioClip openSfx;
    [SerializeField] private AudioClip closeSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Optional UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private Image popupImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private Button closeButton;

    [Header("Events")]
    public UnityEvent onPopupOpened;
    public UnityEvent onPopupClosed;
    public UnityEvent onEventFinished;

    private bool isPlaying;
    private bool closeRequested;
    private bool generatedUi;
    private float previousTimeScale = 1f;
    private PlayerInteract playerInteract;

    public void Play()
    {
        if (isPlaying) return;
        if (showOnlyOnce && IsAlreadyDone()) return;

        playerInteract = FindFirstObjectByType<PlayerInteract>();
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        isPlaying = true;
        closeRequested = false;

        EnsurePopupUi();
        ApplyPopupContent();

        previousTimeScale = Time.timeScale;
        if (pauseWhileOpen)
            Time.timeScale = 0f;

        PlayClip(openSfx);
        popupPanel.SetActive(true);
        onPopupOpened?.Invoke();

        while (!closeRequested)
        {
            if (closeWithSpace && Input.GetKeyDown(KeyCode.Space))
                closeRequested = true;
            if (closeWithEscape && Input.GetKeyDown(KeyCode.Escape))
                closeRequested = true;

            yield return null;
        }

        popupPanel.SetActive(false);
        PlayClip(closeSfx);
        onPopupClosed?.Invoke();

        if (!string.IsNullOrEmpty(doneFlag))
            GameManager.Instance?.SetFlag(doneFlag);

        if (pauseWhileOpen)
            Time.timeScale = previousTimeScale;

        isPlaying = false;

        if (afterPopupLines != null && afterPopupLines.Length > 0)
        {
            if (playerInteract == null)
                playerInteract = FindFirstObjectByType<PlayerInteract>();

            if (playerInteract != null)
            {
                playerInteract.StartDialogue(afterPopupLines, () => onEventFinished?.Invoke());
                yield break;
            }
        }

        onEventFinished?.Invoke();
    }

    public void RequestClose()
    {
        closeRequested = true;
    }

    private bool IsAlreadyDone()
    {
        return !string.IsNullOrEmpty(doneFlag)
            && GameManager.Instance != null
            && GameManager.Instance.HasFlag(doneFlag);
    }

    private void EnsurePopupUi()
    {
        if (popupPanel != null && popupImage != null)
            return;

        generatedUi = true;

        GameObject canvasObject = new GameObject("DialogueEventCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        popupPanel = new GameObject("DialogueEventPanel");
        popupPanel.transform.SetParent(canvasObject.transform, false);
        Image backdrop = popupPanel.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.86f);
        RectTransform panelRect = popupPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject imageObject = new GameObject("PopupImage");
        imageObject.transform.SetParent(popupPanel.transform, false);
        popupImage = imageObject.AddComponent<Image>();
        popupImage.preserveAspect = true;
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.12f, 0.16f);
        imageRect.anchorMax = new Vector2(0.88f, 0.86f);
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        titleText = CreateText("Title", popupPanel.transform, new Vector2(0.12f, 0.86f), new Vector2(0.88f, 0.95f), 32, TextAlignmentOptions.Center);
        hintText = CreateText("Hint", popupPanel.transform, new Vector2(0.12f, 0.04f), new Vector2(0.88f, 0.14f), 22, TextAlignmentOptions.Center);

        GameObject closeObject = new GameObject("CloseButton");
        closeObject.transform.SetParent(popupPanel.transform, false);
        Image buttonImage = closeObject.AddComponent<Image>();
        buttonImage.color = new Color(0f, 0f, 0f, 0f);
        closeButton = closeObject.AddComponent<Button>();
        RectTransform closeRect = closeObject.GetComponent<RectTransform>();
        closeRect.anchorMin = Vector2.zero;
        closeRect.anchorMax = Vector2.one;
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;

        popupPanel.SetActive(false);
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return text;
    }

    private void ApplyPopupContent()
    {
        if (popupImage != null)
        {
            popupImage.sprite = popupSprite;
            popupImage.enabled = popupSprite != null;
        }

        if (titleText != null)
            titleText.text = popupTitle;

        if (hintText != null)
            hintText.text = closeHint;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(RequestClose);
            closeButton.onClick.AddListener(RequestClose);
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, sfxVolume);
    }

    private void OnDisable()
    {
        if (isPlaying && pauseWhileOpen)
            Time.timeScale = previousTimeScale;
    }

    private void OnDestroy()
    {
        if (generatedUi && popupPanel != null)
            Destroy(popupPanel.transform.root.gameObject);
    }
}
