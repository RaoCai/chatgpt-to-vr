using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Oculus.Voice;
using Meta.WitAi.CallbackHandlers;
using System.Reflection;

public class VoiceManager : MonoBehaviour
{
    [Header("Wit Configuration")]
    [SerializeField] private AppVoiceExperience appVoiceExperience;
    [SerializeField] private WitResponseMatcher responseMatcher;
    [SerializeField] private TextMeshProUGUI transcriptionText;

    [Header("Voice Events")]
    [SerializeField] private UnityEvent wakeWordDetected;
    [SerializeField] private UnityEvent<string> completeTranscription;

    private bool _voiceCommandReady;

    private void Awake()
    {

        if (appVoiceExperience != null && appVoiceExperience.VoiceEvents != null)
        {
            // register listeners for voice events
            appVoiceExperience.VoiceEvents.OnRequestCompleted.AddListener(ReactivateVoice);
            appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);

            // bind wake word detection with response matcher
            var eventField = typeof(WitResponseMatcher).GetField("onMultiValueEvent", BindingFlags.NonPublic | BindingFlags.Instance);
            if (eventField != null && eventField.GetValue(responseMatcher) is MultiValueEvent onMultiValueEvent)
            {
                onMultiValueEvent.AddListener(WakeWordDetected);
            }

            // activate voice commands
            if (!appVoiceExperience.Active)
            {
                appVoiceExperience.Activate(); // Activate on start
            }
        }
    }


    private void WakeWordDetected(string[] args)
    {
        _voiceCommandReady = true;
        wakeWordDetected?.Invoke();
    }

    private void OnPartialTranscription(string transcription)
    {
        if (!_voiceCommandReady) return;
        transcriptionText.text = transcription;
    }

    private void OnFullTranscription(string transcription)
    {
        if (!_voiceCommandReady) return;
        _voiceCommandReady = false;
        completeTranscription?.Invoke(transcription);
    }

    private void ReactivateVoice()
    {
        appVoiceExperience.Activate();
    }

    private void OnDestroy()
    {
        // remove listeners when object is destroyed
        appVoiceExperience.VoiceEvents.OnRequestCompleted.RemoveListener(ReactivateVoice);
        appVoiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
        appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);

        var eventField = typeof(WitResponseMatcher).GetField("onMultiValueEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        if (eventField != null && eventField.GetValue(responseMatcher) is MultiValueEvent onMultiValueEvent)
        {
            onMultiValueEvent.RemoveListener(WakeWordDetected);
        }
    }
}
