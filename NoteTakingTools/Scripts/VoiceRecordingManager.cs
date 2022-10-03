using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UVRN.Player;
using UVRN.VoiceChat;
using UnityEngine.UI;
using TMPro;

// Class responsible for creating voice recordings as a form of notes.
// When the recording process starts, the player is muted.
// When the process ends, the player is muted/unmuted - the same he was before recording.
// The recording process is shown on the UI slider
// Maximum time of one recording is 60 seconds. This can be adjusted by changing the "maxClipDurationSec"
public class VoiceRecordingManager : MonoBehaviour
{
    #region Voice Chat Manager
    private UVRN_VivoxVoiceChatManager m_voiceChatManager;
    private UVRN_VivoxVoiceChatManager voiceChat
    {
        get
        {
            if (!m_voiceChatManager)
            {
                m_voiceChatManager = (UVRN_VivoxVoiceChatManager)UVRN_VivoxVoiceChatManager.Instance;
            }
            return m_voiceChatManager;
        }
    }
    #endregion


    // User Interface for the recording
    [SerializeField]
    private GameObject recordingDescriptions;

    [SerializeField]
    private GameObject recordingOnText;

    [SerializeField]
    private GameObject recordingOffText;


    // slider showing the process of the recording
    [SerializeField]
    private Slider timerSlider;

    // text on the slider
    [SerializeField]
    private TextMeshProUGUI timerText;

    public float currTime;
    public float recordingStartTime;

    private bool recording = false;
    private bool voiceRecordingPicked = false;

    private Transform currControllerTransform;

    private AudioClip micRecording;


    private int frequency = 44100;
    public int maxClipDurationSec = 60;


    // how many times should the microphone audio buffer be larger that the reading window
    private const int microphoneBufferSize = 2;

    private UVRN_Player localPlayer;

    // If the player was muted before the recording, they will stay muted after
    private bool mutedPreviously = false;

    void Start()
    {
        // timer set-up
        timerSlider.maxValue = maxClipDurationSec;
        timerSlider.value = 0;
        timerText.text = string.Format("{0:0}:{1:00}", 0, 0);

        // microphone set-up
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone found!");
        }
        else
        {
            int minFreq, maxFreq;
            // Passing null to Microphone methods selects default device
            Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);

            // If max/min frequency = 0, the device supports any frequency
            if (maxFreq > 0 && maxFreq < frequency)
            {
                frequency = maxFreq;
            }
        }

    }

    void Update()
    {
        // move text to the current controller
        if (voiceRecordingPicked)
        {
            recordingDescriptions.transform.position = currControllerTransform.position;
            recordingDescriptions.transform.rotation = currControllerTransform.rotation;
        }

        if (recording)
        {
            currTime = Time.time - recordingStartTime;
            int minutes = Mathf.FloorToInt(currTime / 60);
            int seconds = Mathf.FloorToInt(currTime - minutes * 60);

            timerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);

            // the recording should be stopped with voiceRecording = false
            if (currTime >= maxClipDurationSec)
                // the recording is stopped already 
                timerSlider.value = maxClipDurationSec;
            else
                timerSlider.value = currTime;

        }
    }

    private void LoadLocalPlayer()
    {
        var localPlayerObj = UVRN_Player.LocalPlayerGO;

        if (localPlayerObj)
        {
            localPlayer = localPlayerObj.GetComponent<UVRN_Player>();
        }
        else
        {
            Debug.LogError("Error finding local player");
        }
    }

    public void VoiceRecordingPicked(Transform controllerTransform)
    {
        // timer set-up
        timerSlider.maxValue = maxClipDurationSec;
        timerSlider.value = 0;
        timerText.text = string.Format("{0:0}:{1:00}", 0, 0);

        currControllerTransform = controllerTransform;

        voiceRecordingPicked = true;

        // show menu that says how the voice recording is turned on
        recordingDescriptions.SetActive(true);
        recordingOnText.SetActive(false);
        recordingOffText.SetActive(true);

        if (!localPlayer)
        {
            LoadLocalPlayer();
        }
    }

    public void ActivateVoiceRecording()
    {
        recording = !recording;
        if (recording)
        {
            StartVoiceRecording();
        }
        else
        {
            EndVoiceRecording();
        }
    }

    private void StartVoiceRecording()
    {
        // change text to recording on
        recordingOnText.SetActive(true);
        recordingOffText.SetActive(false);

        micRecording = Microphone.Start(null, true, maxClipDurationSec * microphoneBufferSize, frequency);

        // mute player
        mutedPreviously = voiceChat.IsMicMuted;
        voiceChat.IsMicMuted = true;
        Debug.Log(localPlayer);

        // change the timer slider
        timerSlider.value = 0;
        recordingStartTime = Time.time;
        timerText.text = string.Format("{0:0}:{1:00}", 0, 0);
    }


    private void EndVoiceRecording()
    {
        // change text to recording of
        recordingOnText.SetActive(false);
        recordingOffText.SetActive(true);

        // end recording
        Microphone.End(null);
        SaveVoiceRecording(micRecording);

        // unmute player
        voiceChat.IsMicMuted = mutedPreviously;
    }

    // Saving the voice recording using a script "SavWav" from gitHub
    private void SaveVoiceRecording(AudioClip currRecording)
    {
        Debug.Log("Voice recording saved");

        // Specific name of the recording - makes sure recording names are unique by adding the time they were created
        string name = "/VoiceRecording_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss");

        SavWav.Save(name, currRecording);

    }

    // Ending the entire voice recording tool and resetting the default values
    public void EndRecordingOption()
    {
        if (recording)
            EndVoiceRecording();

        recording = false;
        voiceRecordingPicked = false;
        currControllerTransform = null;
        recordingDescriptions.SetActive(false);
    }

}
