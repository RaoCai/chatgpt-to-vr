using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class window : MonoBehaviour
{
    private void Awake()
    {
        Setup();
    }

    private void Setup()
    {
        TextMeshProUGUI textComponent = GetComponentInChildren<TextMeshProUGUI>();

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
    }
}
