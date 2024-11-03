using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BubbleSetup : MonoBehaviour
{
    private void Awake()
    {
        Setup();
    }

    private void Setup()
    {

        RectTransform rect = GetComponent<RectTransform>();
        Button button = GetComponent<Button>();
        Image backgroundImage = GetComponent<Image>();
        TextMeshProUGUI textComponent = GetComponentInChildren<TextMeshProUGUI>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200f, 100f);

            rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0);
        }

        if (backgroundImage != null)
        {
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.raycastTarget = true;
        }

        if (textComponent != null)
        {
            textComponent.fontSize = 14;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.enableWordWrapping = true;
            textComponent.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform textRect = textComponent.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.sizeDelta = new Vector2(-20f, -20f); 
                textRect.anchoredPosition = Vector2.zero;

                textRect.localPosition = Vector3.zero;
            }
        }

        if (button != null)
        {
            button.targetGraphic = backgroundImage;
        }

        Canvas.ForceUpdateCanvases();
    }
}
