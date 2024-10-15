using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;  // For TextMeshPro
using UnityEngine.UI;  // For InputField and Button
using System.Collections.Generic;

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
    public TextAsset jsonApi;  //store API key
    public TextMeshProUGUI textDisplay;  //display responses
    public TMP_InputField inputField;  //store user input
    public Button sendButton;  //send the question
    private string apiKey = "";
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent";
    private List<string> conversationHistory = new List<string>();  //store conversation history

    void Start()
    {
        // Load the API key from JSON
        UnityAndGeminiKey jsonApiKey = JsonUtility.FromJson<UnityAndGeminiKey>(jsonApi.text);
        apiKey = jsonApiKey.key;

        sendButton.onClick.AddListener(OnSendButtonClick);  // Add listener for button click
    }

    public void AskGemini(string userInput)
    {
        // Restrict the response length
        string finalPrompt = "Atention, Please limit your response in 50 words or fewer." + userInput;
        StartCoroutine(SendRequestToGemini(finalPrompt));
        inputField.text = "";
    }

    // Method triggered when user clicks the send button
    public void OnSendButtonClick()
    {
        string userInput = inputField.text;  // read the user inputfield
        if (!string.IsNullOrEmpty(userInput))
        {
            string finalPrompt = "Atention, Please limit your response in 50 words or fewer." + userInput;
            StartCoroutine(SendRequestToGemini(finalPrompt));
            inputField.text = "";  // clear the inputfield
        }
    }

    // communicate with the gemini
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

                    // Add Gemini's response to conversation history
                    conversationHistory.Add(text);

                    // Display Gemini's response in TextMeshPro
                    textDisplay.text = text;
                }
                else
                {
                    Debug.Log("No text found.");
                }
            }
        }
    }
}
