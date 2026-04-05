using UnityEngine;

public class MouseWorldPosition : MonoBehaviour
{
    void Update()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Debug.Log($"World: {mousePos.x}, {mousePos.y}");
    }
}