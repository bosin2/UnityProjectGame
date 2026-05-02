using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class IntroDialogue : MonoBehaviour
{
    public TextMeshProUGUI dialogueText;
    public GameObject clickHint;
    public Image fadePanel;
    public Image cutscene;

    [System.Serializable]
    public class DialogueLine
    {
        public string text;
        public Sprite image;      // null이면 이미지 안 바뀜
        public bool clearImage;   // true면 이미지 숨김
    }

    public DialogueLine[] lines;


    private int currentLine = 0;
    private bool isTyping = false;
    private bool canClick = false;

    void Start()
    {
        cutscene.color = new Color(1, 1, 1, 0); // 시작은 투명
        clickHint.SetActive(false);
        StartCoroutine(FadeIn());
    }

    void Update()
    {
        if (canClick && Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogueText.text = lines[currentLine].text;
                isTyping = false;
                canClick = true;
                clickHint.SetActive(true);
            }
            else
            {
                NextLine();
            }
        }
    }

    IEnumerator FadeIn()
    {
        float t = 1f;
        while (t > 0)
        {
            t -= Time.deltaTime;
            fadePanel.color = new Color(0, 0, 0, t);
            yield return null;
        }
        fadePanel.color = new Color(0, 0, 0, 0);
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(TypeLine(lines[currentLine]));
    }

    IEnumerator TypeLine(DialogueLine line)
    {
        isTyping = true;
        canClick = false;
        clickHint.SetActive(false);
        dialogueText.text = "";

        // 이미지 교체
        if (line.clearImage)
        {
            yield return StartCoroutine(FadeImage(cutscene, 0f));
        }
        else if (line.image != null)
        {
            cutscene.sprite = line.image;
            yield return StartCoroutine(FadeImage(cutscene, 1f));
        }

        // 타이핑
        foreach (char c in line.text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.07f);
        }

        isTyping = false;
        canClick = true;
        clickHint.SetActive(true);
    }

    IEnumerator FadeImage(Image img, float targetAlpha)
    {
        float start = img.color.a;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            img.color = new Color(1, 1, 1, Mathf.Lerp(start, targetAlpha, t));
            yield return null;
        }
    }

    void NextLine()
    {
        currentLine++;
        if (currentLine >= lines.Length)
        {
            StartCoroutine(FadeOutAndLoad());
            return;
        }
        StartCoroutine(TypeLine(lines[currentLine]));
    }

    IEnumerator FadeOutAndLoad()
    {
        canClick = false;
        clickHint.SetActive(false);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            fadePanel.color = new Color(0, 0, 0, t);
            yield return null;
        }
        SceneManager.LoadScene("Tutorial");
    }
}