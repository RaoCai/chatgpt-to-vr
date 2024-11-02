using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;  // For TextMeshPro
using UnityEngine.UI;  // For InputField and Button
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;  // For LINQ operations
using UnityEngine.EventSystems;

// store api key data loaded from a json file
[System.Serializable]
public class UnityAndGeminiKey
{
    public string key;
}

// define the format of Gemini's response
[System.Serializable]
public class Response
{
    public Candidate[] candidates;
}

[System.Serializable]
public class Candidate
{
    public Content content;
}

[System.Serializable]
public class Content
{
    public Part[] parts;
}

[System.Serializable]
public class Part
{
    public string text;
}

// class for preparing requests to gemini api
[System.Serializable]
public class GeminiRequest
{
    public GeminiContent[] contents;
}

[System.Serializable]
public class GeminiContent
{
    public GeminiPart[] parts;
}

[System.Serializable]
public class GeminiPart
{
    public string text;
}

//structure to store message details
[System.Serializable]
public class Message
{
    public string content; 
    public bool isSelected; //check if the message is selected and change colar
    public GameObject bubbleObject; 
    public float verticalPosition; // Y position of the messagebubble
    public int index; // to order the messagebubble in the same level
}

public class GeminiManager : MonoBehaviour
{
    // UI and prefabs
    public TextAsset jsonApi;  // Store API key
    public TextMeshProUGUI textDisplay;  // Display responses
    public TMP_InputField inputField;  // Store user input
    public Button sendButton;  // Send the question
    public GameObject userMessagePrefab;  
    public GameObject aiMessagePrefab;  
    public Transform logContent; //the container to store the messagebubble
    public GameObject messageDetailWindow;
    public TextMeshProUGUI messageDetailText;
    public Button closeDetailButton;

    private string apiKey = ""; // Load from the json file
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent";

    // Layout of messagebubbles 
    private const float Horizontal = 100f; // horizontal distance between bubbles
    private const float Vertical = 100f; // vertical distance
    private const float InitialY = 100f; // initial Y offset for the first row

    // default and selected colar
    private Color selectedColor = new Color(0.8f, 1f, 0.8f, 1f); //light green
    private Color defaultColor = Color.white;

    // use list to store the messages
    private List<List<Message>> messageLists = new List<List<Message>>();
    private float currentVertical = 0f; // tracks the current Y position 
    private ScrollRect scrollRect; 
    public Button TellMeMoreButton; 
    private Message currentSelectedMessage; // a tmp to the current selected message
    private bool isFollowup = false; // check if the request is a followup query

    void Start()
    {
        Debug.Log("start() called");
        // Load the API key from JSON
        UnityAndGeminiKey jsonApiKey = JsonUtility.FromJson<UnityAndGeminiKey>(jsonApi.text);
        apiKey = jsonApiKey.key;

        sendButton.onClick.AddListener(OnSendButtonClick); // Add listener for button click
        closeDetailButton.onClick.AddListener(CloseMessageDetail);


        // listener for the tellmemore button
        if (TellMeMoreButton == null)
        {
            Debug.LogError("tellmemore is null");
        }
        else
        {
            TellMeMoreButton.onClick.AddListener(() => OnTMMClick(currentSelectedMessage));
        }



        scrollRect = logContent.GetComponentInParent<ScrollRect>();// get scrollrect component for the message log

        // set content area anchor and pivot
        RectTransform contentRect = logContent.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);



        currentVertical = -InitialY;
        UpdateScrollViewContentSize();
    }

    // Method triggered when user clicks the send button
    public void OnSendButtonClick()
    {
        string userInput = inputField.text;  // read the user inputfield

        // when the click is from submit
        if (!string.IsNullOrEmpty(userInput))
        {
            // Create a message bubble and set position
            GameObject userBubble = Instantiate(userMessagePrefab, logContent);
            RectTransform userBubbleRect = userBubble.GetComponent<RectTransform>();
            userBubbleRect.anchorMin = new Vector2(0.5f, 1);
            userBubbleRect.anchorMax = new Vector2(0.5f, 1);
            userBubbleRect.pivot = new Vector2(0.5f, 0.5f);
            //userBubbleRect.sizeDelta = new Vector2(200f, 100f);
            userBubbleRect.anchoredPosition = new Vector2(0, currentVertical);

            userBubble.GetComponentInChildren<TextMeshProUGUI>().text = userInput;

            // build the user messagebubble and put in list
            List<Message> newList = new List<Message>();
            Message userMessage = new Message
            {
                content = userInput,
                bubbleObject = userBubble,
                verticalPosition = currentVertical,
                index = messageLists.Count
            };
            newList.Add(userMessage);
            messageLists.Add(newList);
            inputField.text = "";


            StartCoroutine(SendRequestToGemini(userInput, false));

            // move the current position down offset
            currentVertical -= Vertical;
            UpdateScrollViewContentSize();
            StartCoroutine(ScrollToBottom());
        }
    }

    // send request to the Gemini and process the response
    private IEnumerator SendRequestToGemini(string userInput, bool isFollowup)
    {
        Debug.Log($"sendrequest called Input: {userInput}, isFollowup: {isFollowup}");

        string prompt;
        if (isFollowup)
        {
            prompt = $"Based on this message: \"{userInput}\", please provide 1 to 5 detailed points in a numbered list format.";
        }
        else
        {
            prompt = $"Please respond with exactly 1 to 5 points in a numbered list format. Here's the query: {userInput}";
        }

        // prepare the request data
        GeminiPart part = new GeminiPart();
        GeminiContent content = new GeminiContent();
        GeminiRequest request = new GeminiRequest();
        part.text = prompt;
        content.parts = new GeminiPart[] { part };
        request.contents = new GeminiContent[] { content };


        string jsonData = JsonUtility.ToJson(request);
        string url = $"{apiEndpoint}?key={apiKey}";

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
            
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {www.error}");
                Debug.LogError($"Full error response: {www.downloadHandler.text}");
            }
            else
            {
                Response response = JsonUtility.FromJson<Response>(www.downloadHandler.text);
                if (response != null && response.candidates != null && response.candidates.Length > 0)
                {
                    string responseText = response.candidates[0].content.parts[0].text;
                    DisplayMessageBubble(responseText, isFollowup);
                    Debug.Log("request done");
                }
                else
                {
                    Debug.LogError("No valid response candidates received from Gemini API");
                }
            }
        }
    }

    // split response into individual points, and display them as message bubbles
    public void DisplayMessageBubble(string response, bool isFollowup)
    {
        Debug.Log("displayMessageBubble called");

        List<string> points = ExtractResponse(response);
        Debug.Log($"extracted {points.Count} points");

        // create a new list for the response
        // the list contains mutlti bubbles for a row
        if (points.Count > 0)
        {
            List<Message> newList = new List<Message>();

            // x offset
            float startX = -(points.Count-1) * Horizontal/2f;

            for (int i = 0; i < points.Count; i++)
            {
                Message message = new Message
                {
                    content = points[i],
                    isSelected = false, // defaultly not selected
                    verticalPosition = currentVertical,
                    index = messageLists.Count
                };

                GameObject bubble = Instantiate(aiMessagePrefab, logContent);
                Debug.Log("new bubble created");

                Button bubbleButton = bubble.GetComponent<Button>();
                if (bubbleButton == null)
                {
                    Debug.LogWarning("button null");
                    bubbleButton = bubble.AddComponent<Button>();
                }

                // add a background color interactable img
                Image bubbleImg= bubble.GetComponent<Image>();
                if (bubbleImg == null)
                {
                    Debug.LogWarning("Image component null");
                    bubbleImg = bubble.AddComponent<Image>();
                }
                bubbleImg.raycastTarget = true;
                bubbleImg.color = defaultColor; 

                // event trigger to handle click on the bubble
                EventTrigger eventTrigger = bubble.GetComponent<EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = bubble.AddComponent<EventTrigger>();
                }

                EventTrigger.Entry entry = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerClick
                };

                eventTrigger.triggers.Add(entry);

                // capture the message and listen for to click selection
                Message capturedMessage = message;
                bubbleButton.onClick.AddListener(() => {
                    Debug.Log($"button clicked {capturedMessage.content}");
                    OnMessageBubbleClick(capturedMessage);
                });

                RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
                bubbleRect.anchorMin = new Vector2(0.5f, 1);
                bubbleRect.anchorMax = new Vector2(0.5f, 1);
                bubbleRect.pivot = new Vector2(0.5f, 0.5f);
                bubbleRect.sizeDelta = new Vector2(200f, 100f);
                bubbleRect.anchoredPosition = new Vector2(startX + (i * Horizontal), currentVertical);

                TextMeshProUGUI bubbleText = bubble.GetComponentInChildren<TextMeshProUGUI>();
                if (bubbleText != null)
                {
                    bubbleText.text = points[i];
                }
                else
                {
                    Debug.LogError("bubbleText not found in bubble");
                }

                message.bubbleObject = bubble;
                newList.Add(message);
            }

            messageLists.Add(newList);
            currentVertical -= Vertical;

            UpdateScrollViewContentSize();
            StartCoroutine(ScrollToBottom());
        }
    }

    // extract numbered list member from gemini response
    private List<string> ExtractResponse(string text)
    {
        if (text == null)
        {
            Debug.LogWarning("text null");
            return new List<string>();
        }
        List<string> points = new List<string>();
        string[] lines = text.Split('\n');

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.Length > 2 && char.IsDigit(trimmedLine[0]) && trimmedLine[1] == '.')
            {
                string point = trimmedLine.Substring(2).Trim();
                if (!string.IsNullOrEmpty(point))
                {
                    points.Add(point);
                }
            }
        }
        return points;
    }

    // click bubble and foloowup query
    private void OnMessageBubbleClick(Message message)
    {
        Debug.Log("on bubble click");
        message.isSelected = !message.isSelected;
        Debug.Log($"state: {message.isSelected}");

        // change the bubble color based on selection state
        Image bubbleImage = message.bubbleObject.GetComponent<Image>();
        if (bubbleImage != null)
        {
            Color newColor;

            if (message.isSelected)
            {
                newColor = selectedColor;
            }
            else
            {
                newColor = defaultColor;
            }

            bubbleImage.color = newColor;
        }

        if (message.isSelected)
        {
            ShowFullMessage(message.content);
        }
    }

    // Display the full message content in a detail window
    private void ShowFullMessage(string fullText)
    {
        Debug.Log("show full message clicked" + fullText);

        messageDetailText.text = fullText;
        messageDetailWindow.SetActive(true); 

        // use selectmany to transfor the lists into a enumerator
        // and then search for the target message
        currentSelectedMessage = messageLists.SelectMany(group => group).FirstOrDefault(msg => msg.content == fullText);
    }

    // close the detail window
    private void CloseMessageDetail()
    {
        Debug.Log("close window.");
        messageDetailWindow.SetActive(false);
    }

    // adjust and refresh scroll view based on the current message count
    private void UpdateScrollViewContentSize()
    {
        RectTransform contentRect = logContent.GetComponent<RectTransform>();
        float totalHeight = Mathf.Abs(currentVertical) + InitialY;
        contentRect.sizeDelta = new Vector2(0, totalHeight); // set new height based on message count
    }

    // scroll the log view to the bottom to follow the messages
    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }

    // tell me more button
    public void OnTMMClick(Message message)
    {
        if (message == null)
        {
            Debug.LogError("no message selected");
            return;
        }

        Debug.Log($"ttm: {message.content}");

        isFollowup = true; 
        StartCoroutine(SendRequestToGemini(message.content, true));

        CloseMessageDetail();
    }
}
