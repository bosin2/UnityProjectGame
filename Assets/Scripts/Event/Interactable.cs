using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("대사")]
    public string[] dialogueLines;

    [Header("조건")]
    public string requiredFlag = "";
    public string hintMessage = "...";

    [Header("완료 후 동작")]
    public UnityEvent onComplete;
    public string setFlag = "";
}