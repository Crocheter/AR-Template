using UnityEngine;
using UnityEngine.Video;
using Vuforia;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
public class VideoTargetHandler : MonoBehaviour
{
    public GameObject videoPlane;
    public VideoPlayer videoPlayer;
   // public GameObject loadingSpinner;   // Spinner object
    
    // public Button playPauseButton;
    // public Button restartButton;
    // public Button forward10Button;
    // public Button back10Button;
    // public Button muteButton;
    // public Canvas mainCanvas;
     // public TextMeshProUGUI arMessageText;
    private ObserverBehaviour observer;
    private bool isMuted = false;
    public Material transparentMaterial;
    public Material videoMaterial; // the video 
    private Vector3 targetLocalPos = Vector3.zero; // centered on the target
    private Quaternion targetLocalRot = Quaternion.Euler(0, 180f, 0);
    public float smoothFactor = 10f;
    private readonly List<string> validTargets = new List<string>
    {
        "OneTarget",
        "TwoTarget",
        "ThreeTarget"
    };
    void Update()
    {
        if (videoPlane.activeSelf)
        {
            // Smoothly follow the target
            videoPlane.transform.localPosition = Vector3.Lerp(videoPlane.transform.localPosition, targetLocalPos, Time.deltaTime * smoothFactor);
            videoPlane.transform.localRotation = Quaternion.Slerp(videoPlane.transform.localRotation, targetLocalRot, Time.deltaTime * smoothFactor);
        }
    }
    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();

        if (observer != null)
        {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }
        // Initial state
        videoPlayer.Stop();
        videoPlane.SetActive(false);

        ShowMessage("Initializing AR... Loading, please wait");

        videoPlayer.errorReceived += OnVideoError;

          // --- Hook up UI button events ---
        // if (playPauseButton) playPauseButton.onClick.AddListener(TogglePlayPause);
        // if (restartButton) restartButton.onClick.AddListener(RestartVideo);
        // if (forward10Button) forward10Button.onClick.AddListener(() => SeekVideo(10));
        // if (back10Button) back10Button.onClick.AddListener(() => SeekVideo(-10));
        // if (muteButton) muteButton.onClick.AddListener(ToggleMute);

       // if (loadingSpinner)
          //  loadingSpinner.SetActive(false);
        // Prepare callback
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoCompleted;

        SendToFlutter.Send("scene_loaded");
    }
    private void OnVideoCompleted(VideoPlayer vp)
    {
        SendToFlutter.Send("video_completed");
    }
    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool isVisible =
            status.Status == Status.TRACKED ||
            status.Status == Status.LIMITED;   // Optional based on how strict you want detection

        if (status.StatusInfo == StatusInfo.INSUFFICIENT_LIGHT)
        SendToFlutter.Send("too_dark");

        if (status.StatusInfo == StatusInfo.EXCESSIVE_MOTION)
            SendToFlutter.Send("moving_too_fast");

        if (!isVisible)
        {
            ShowMessage("Point your camera at your Picsible Memory Frame");
            SendToFlutter.Send("cloud_reco_ready");
        }
        if (isVisible)
            OnTargetFound();
        else
            OnTargetLost();

       if (!isVisible && !validTargets.Contains(observer.TargetName))
            {
                ShowNoMatchError();
            }

    }
    private void OnTargetFound()
    {
        ShowMessage("Matched! Loading video...");
        videoPlane.transform.localPosition = Vector3.Lerp(videoPlane.transform.localPosition, targetLocalPos, Time.deltaTime * smoothFactor);
        videoPlane.transform.localRotation = Quaternion.Slerp(videoPlane.transform.localRotation, targetLocalRot, Time.deltaTime * smoothFactor);
        // Show spinner during video load
       // if (loadingSpinner)
          //  loadingSpinner.SetActive(true);
        videoPlane.SetActive(true);
        videoPlane.GetComponent<Renderer>().material = transparentMaterial;
        videoPlayer.Prepare();

        SendToFlutter.Send("target_found:" + observer.TargetName);
    }
    private void OnVideoPrepared(VideoPlayer vp)
    {
      //  if (loadingSpinner)
          //   loadingSpinner.SetActive(false);

        // Switch from invisible to actual material
        videoPlane.GetComponent<Renderer>().material = videoMaterial;

        videoPlayer.Play();
        SendToFlutter.Send("video_started");
    }
    private void OnTargetLost()
    {
    videoPlayer.Stop();
    videoPlane.SetActive(false);

    // if (loadingSpinner)
     //   loadingSpinner.SetActive(false);

    ShowMessage("Tracking lost... Reposition camera");
    SendToFlutter.Send("tracking_lost");
    SendToFlutter.Send("searching");
    }
     // 🎥 VIDEO PLAYBACK CONTROLS
  private void TogglePlayPause()
{
    if (videoPlayer.isPlaying)
    {
        videoPlayer.Pause();
        SendToFlutter.Send("video_paused");
    }
    else
    {
        videoPlayer.Play();
        SendToFlutter.Send("video_resumed");
    }
}

    private void RestartVideo()
    {
        videoPlayer.time = 0;
        videoPlayer.Play();
        SendToFlutter.Send("video_restart");
    }
    private void SeekVideo(double seconds)
    {
        double newTime = videoPlayer.time + seconds;

        // Keep time inside valid range
        newTime = Mathf.Clamp((float)newTime, 0f, (float)videoPlayer.length);

        videoPlayer.time = newTime;
        SendToFlutter.Send("video_seek");
    }
    private void ToggleMute()
    {
        isMuted = !isMuted;
        videoPlayer.SetDirectAudioMute(0, isMuted);
        SendToFlutter.Send("video_toggle_mute");
    }
    private void ShowMessage(string msg)
    {
       // if (arMessageText != null)
         //   arMessageText.text = msg;
    }
    private void ShowNoMatchError()
    {
        ShowMessage("No match found... Not the right image");
        SendToFlutter.Send("Error:no_target_found");
    }
    private void ShowLowLightError()
    {
        ShowMessage("Low light... Please turn on the light");
        SendToFlutter.Send("Error:low_light");
    }
    private void ShowPoorNetworkError()
    {
        ShowMessage("Low internet connection");
        SendToFlutter.Send("Error:poor_network");

    }
    private void ShowVideoUnavailableError()
    {
        ShowMessage("Can't load video right now");
        SendToFlutter.Send("Error:video_unavailable");    
    }
    private void OnVideoError(VideoPlayer vp, string msg)
    {
        ShowVideoUnavailableError();
        SendToFlutter.Send("video_error:" + msg);

    }
    // FLUTTER → UNITY ACTIONS
    public void FlutterPlay(string _)
    {
    videoPlayer.Play();
    SendToFlutter.Send("video_resumed");
    }
    public void FlutterPause(string _)
    {
    videoPlayer.Pause();
    SendToFlutter.Send("video_paused");
    }
    public void FlutterRestart(string _)
    {
    RestartVideo();
    }
    public void FlutterMute(string _)
    {
    videoPlayer.SetDirectAudioMute(0, true);
    SendToFlutter.Send("video_muted");
    }

    public void FlutterUnmute(string _)
    {
    videoPlayer.SetDirectAudioMute(0, false);
    SendToFlutter.Send("video_unmuted");
    }

    public void FlutterSeek(string seconds)
    {
    double offset;
    if (double.TryParse(seconds, out offset))
    {
    SeekVideo(offset);
    }
    }
    public void FlutterScanNew(string _)
    {
        {
    // stop video and reset
    videoPlayer.Stop();
    videoPlane.SetActive(false);
    SendToFlutter.Send("scan_new");
}
    }
    public void FlutterShowMessage(string message)
    {
    ShowMessage(message);
    }
}
