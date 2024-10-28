using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;  // For TextMeshPro
using UnityEngine.UI;  // For InputField and Button
using System.Collections.Generic;
using System.Linq;  // For LINQ operations

[System.Serializable]
public class UnityAndGeminiKey
{
    public string key;
}

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

public class GeminiManager : MonoBehaviour
{
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

    private string apiKey = "";
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent";
    private List<string> conversationHistory = new List<string>();  // Store conversation history

    void Start()
    {
        // Load the API key from JSON
        UnityAndGeminiKey jsonApiKey = JsonUtility.FromJson<UnityAndGeminiKey>(jsonApi.text);
        apiKey = jsonApiKey.key;

        sendButton.onClick.AddListener(OnSendButtonClick); // Add listener for button click
        closeDetailButton.onClick.AddListener(CloseMessageDetail);
    }


    public void AskGemini(string userInput)
    {
        // Restrict the response length
        string finalPrompt = "Atention, Please limit your response in 50 words or fewer." + userInput;
        inputField.text = "";
        StartCoroutine(SendRequestToGemini(finalPrompt));
    }

    // Method triggered when user clicks the send button
    public void OnSendButtonClick()
    {
        string userInput = inputField.text;  // read the user inputfield
        if (!string.IsNullOrEmpty(userInput))
        {
            AskGemini(userInput);
            DisplayMessageBubble(userInput, true);
        }
    }

    // display the bubble extracted content
    public void DisplayMessageBubble(string fullText, bool isUserMessage)
    {
        // loading different prefab by different source
        GameObject messageBubblePrefab;
        if (isUserMessage)
        {
            messageBubblePrefab = userMessagePrefab;
        }
        else
        {
            messageBubblePrefab = aiMessagePrefab;
        }

        GameObject messageBubble = Instantiate(messageBubblePrefab, logContent);
        TextMeshProUGUI messageText = messageBubble.GetComponentInChildren<TextMeshProUGUI>();

        // split the text into words
        string[] words = fullText.Split(' ');
        string truncatedText = string.Join(" ", words.Take(10));

        if (words.Length > 10)
        {
            truncatedText += "..."; 
        }


        messageText.text = truncatedText;

        // display the complete content when clicking bubble
        Button messageButton = messageBubble.GetComponentInChildren<Button>();
        if (messageButton != null)
        {
            Debug.Log("bubble button exist");
            messageButton.onClick.AddListener(() => {
                Debug.Log("message bubble clicked");
                ShowFullMessage(fullText);
            });
        }
        else
        {
            Debug.LogWarning("bubble button not exist");
        }
    }


    public void ShowFullMessage(string fullText)
    {
        Debug.Log("full message: " + fullText);
        messageDetailText.text = fullText;
        messageDetailWindow.SetActive(true);
    }

    public void CloseMessageDetail()
    {
        messageDetailWindow.SetActive(false);
    }


    public IEnumerator SendRequestToGemini(string promptText)
    {
        // conversation context with previous messages
        conversationHistory.Add(promptText);

        string conversationContext = string.Join(" ", conversationHistory);
        string url = $"{apiEndpoint}?key={apiKey}";
        string jsonData = "{\"contents\": [{\"parts\": [{\"text\": \"{" + conversationContext + "}\"}]}]}";

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
            }
            else
            {
                Debug.Log("Request done");
                Response response = JsonUtility.FromJson<Response>(www.downloadHandler.text);
                if (response.candidates.Length > 0 && response.candidates[0].content.parts.Length > 0)
                {
                    string text = response.candidates[0].content.parts[0].text;
                    Debug.Log(text);

                    DisplayMessageBubble(text, false);

                    // Add Gemini's response to conversation history
                    conversationHistory.Add(text);
                }
                else
                {
                    Debug.Log("No text found.");
                }
            }
        }
    }
}