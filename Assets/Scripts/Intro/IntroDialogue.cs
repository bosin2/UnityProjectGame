using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

// 인트로 씬의 컷씬 대사, 이미지 전환, 페이드 효과를 순서대로 처리하는 컴포넌트
public class IntroDialogue : MonoBehaviour
{
    public TextMeshProUGUI dialogueText;
    public GameObject clickHint;
    public Image fadePanel;
    public Image cutscene;

    // 한 줄의 대사와 함께 표시할 이미지 정보를 담는 데이터 클래스
    [System.Serializable]
    public class DialogueLine
    {
        public string text;
        public Sprite image;      // null이면 이미지 변경 없음
        public bool clearImage;   // true면 현재 이미지 페이드 아웃
    }

    public DialogueLine[] lines;

    private int currentLine = 0;
    private bool isTyping = false;
    private bool canClick = false;

    void Start()
    {
        cutscene.color = new Color(1, 1, 1, 0);
        clickHint.SetActive(false);
        StartCoroutine(FadeIn());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // 타이핑 중 Space: 현재 줄 즉시 완성
                StopAllCoroutines();
                dialogueText.text = lines[currentLine].text;
                isTyping = false;
                canClick = true;
                clickHint.SetActive(true);
            }
            else if (canClick)
            {
                NextLine();
            }
        }
    }

    // 씬 시작 시 검정 화면을 페이드 인 후 첫 대사 시작
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

    // 이미지 전환 후 한 글자씩 타이핑 연출
    IEnumerator TypeLine(DialogueLine line)
    {
        isTyping = true;
        canClick = false;
        clickHint.SetActive(false);
        dialogueText.text = "";

        if (line.clearImage)
            yield return StartCoroutine(FadeImage(cutscene, 0f));
        else if (line.image != null)
        {
            cutscene.sprite = line.image;
            yield return StartCoroutine(FadeImage(cutscene, 1f));
        }

        foreach (char c in line.text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.07f);
        }

        isTyping = false;
        canClick = true;
        clickHint.SetActive(true);
    }

    // 이미지를 지정 알파값으로 부드럽게 전환
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

    // 다음 줄로 이동. 마지막 줄이면 페이드 아웃 후 튜토리얼 씬 전환
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

    // 화면을 검정으로 페이드 아웃 후 튜토리얼 씬 로드
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
