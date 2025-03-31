using EasyUI.Toast;
using live.videosdk;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
public class GameManager : MonoBehaviour
{
    private bool micToggle;
    private bool camToggle;

    [SerializeField] GameObject _videoSurfacePrefab;
    [SerializeField] Transform _parent;
    [SerializeField] GameObject _meetingJoinPanel;
    [SerializeField] GameObject _meetingPanel;

    private VideoSurface _localParticipant;
    private Meeting meeting;
    private readonly string _token = "YOUR_TOKEN";

    [SerializeField] TMP_Text _meetingIdTxt;
    [SerializeField] TMP_InputField _meetingIdInputField;

    private List<VideoSurface> _participantList = new List<VideoSurface>();

    private void Awake()
    {
        _meetingPanel.SetActive(false);
        _meetingJoinPanel.SetActive(false);
        // Request for Camera and Mic Permission
        RequestForPermission();
    }
    void Start()
    {
        meeting = Meeting.GetMeetingObject();

        meeting.OnCreateMeetingIdCallback += OnCreateMeeting;
        meeting.OnParticipantJoinedCallback += OnParticipantJoined;
        meeting.OnParticipantLeftCallback += OnParticipantLeft;
        meeting.OnCreateMeetingIdFailedCallback += OnCreateMeetingFailed;
        meeting.OnMeetingStateChangedCallback += OnMeetingStateChanged;
        meeting.OnErrorCallback += OnError;
        _meetingJoinPanel.SetActive(true);
    }

    private void OnError(Error error)
    {
        Debug.LogError($"Error-Code: {error.Code} Message: {error.Message} Type: {error.Type}");
        Toast.Show($"OnError: Error-Code: {error.Code} Message: {error.Message}", 3f, Color.red, ToastPosition.MiddleCenter);
    }

    private void OnParticipantJoined(IParticipant participant)
    {
        Debug.Log($"On Pariticpant Joined: " + participant.ToString());
        Toast.Show($"<color=green>PariticpantJoined: </color> {participant.ToString()}", 1f, ToastPosition.TopCenter);
        VideoSurface surface = Instantiate(_videoSurfacePrefab, _parent.transform).GetComponentInChildren<VideoSurface>();
        surface.SetVideoSurfaceType(VideoSurfaceType.RawImage);//For raw Image
        surface.SetParticipant(participant);
        surface.SetEnable(true);
        _participantList.Add(surface);
        if (participant.IsLocal)
        {
            _localParticipant = surface;
            _localParticipant.OnStreamEnableCallback += OnStreamEnable;
            _localParticipant.OnStreamDisableCallback += OnStreamDisable;
            _meetingIdTxt.text = meeting.MeetingID;
            _meetingIdInputField.text = string.Empty;
            _meetingJoinPanel.SetActive(false);
            _meetingPanel.SetActive(true);

        }
    }

    private void OnStreamDisable(string kind)
    {
        Debug.Log($"OnStreamDisable {kind}");
        camToggle = _localParticipant.CamEnabled;
        micToggle = _localParticipant.MicEnabled;
    }

    private void OnStreamEnable(string kind)
    {
        Debug.Log($"OnStreamEnable {kind}");
        camToggle = _localParticipant.CamEnabled;
        micToggle = _localParticipant.MicEnabled;
    }

    private void OnParticipantLeft(IParticipant participant)
    {
        Debug.Log($"On Pariticpant Left: " + participant.ToString());
        Toast.Show($"<color=yellow>PariticpantLeft: </color> {participant.ToString()}", 2f, ToastPosition.TopCenter);
        if (participant.IsLocal)
        {
            OnLeave();
        }
        else
        {
            // For remote participants, find the VideoSurface object and destroy it
            VideoSurface surfaceToRemove = null;
            for (int i = 0; i < _participantList.Count; i++)
            {
                if (participant.ParticipantId == _participantList[i].Id)
                {
                    surfaceToRemove = _participantList[i];
                    _participantList.RemoveAt(i);
                    break;
                }

            }
            if (surfaceToRemove != null)
            {
                Destroy(surfaceToRemove.transform.parent.gameObject);
            }
        }
    }

    private void OnLeave()
    {
        _meetingJoinPanel.SetActive(true);
        _meetingPanel.SetActive(false);
        camToggle = true;
        micToggle = true;
        for (int i = 0; i < _participantList.Count; i++)
        {
            Destroy(_participantList[i].transform.parent.gameObject);
        }
        _participantList.Clear();
        _meetingIdTxt.text = "VideoSDK Unity Demo";
    }

    private void OnCreateMeeting(string meetingId)
    {
        _meetingIdTxt.text = meetingId;
        meeting.Join(_token, meetingId, "User", true, true);
    }

    public void CreateMeeting()
    {
        Debug.Log("User Request for Create meet-ID");

        // Alert the user if microphone or camera permission is not granted.
        AlertNoPermission();

        _meetingJoinPanel.SetActive(false);
        meeting.CreateMeetingId(_token);
    }

    private void OnCreateMeetingFailed(string obj)
    {
        _meetingJoinPanel.SetActive(true);
        Debug.LogError(obj);
        Toast.Show($"OnCreateMeetFailed: {obj}", 1f, Color.red, ToastPosition.TopCenter);
    }

    private void OnMeetingStateChanged(string obj)
    {
        Toast.Show($"<color=yellow>MeetingStateChanged: </color> {obj}", 2f, ToastPosition.TopCenter);
        Debug.Log($"MeetingStateChanged: {obj}");
    }

    public void JoinMeeting()
    {
        if (string.IsNullOrEmpty(_meetingIdInputField.text)) return;

        // Alert the user if microphone or camera permission is not granted.
        AlertNoPermission();

        try
        {
            meeting.Join(_token, _meetingIdInputField.text, "User", true, false);
        }
        catch (Exception ex)
        {
            Debug.LogError("Join Meet Failed: " + ex.Message);
        }
    }

    public void CamToggle()
    {
        camToggle = !camToggle;
        Debug.Log("Cam Toggle " + camToggle);
        _localParticipant?.SetVideo(camToggle);
    }
    public void AudioToggle()
    {
        micToggle = !micToggle;
        Debug.Log("Mic Toggle " + micToggle);
        _localParticipant?.SetAudio(micToggle);
    }

    public void LeaveMeeting()
    {
        meeting?.Leave();
    }

    private void OnApplicationPause(bool pause)
    {
        if (_participantList.Count > 1)
        {
            AudioStream(pause);
            VideoStream(pause);

        }

    }

    private void AudioStream(bool status)
    {
        foreach (var participant in _participantList)
        {
            if (!participant.IsLocal)
            {
                switch (status)
                {
                    case true:
                        {
                            participant.PauseAudio();
                            break;
                        }
                    case false:
                        {
                            participant.ResumeAudio();
                            break;
                        }
                }
            }

        }
        _localParticipant?.SetAudio(!status);
    }

    private void VideoStream(bool status)
    {
        foreach (var participant in _participantList)
        {
            if (!participant.IsLocal)
            {
                switch (status)
                {
                    case true:
                        {
                            participant.PauseVideo();
                            break;
                        }
                    case false:
                        {
                            participant.ResumeVideo();
                            break;
                        }
                }
            }

        }
    }



    private void OnPermissionGranted(string permissionName)
    {
        // Debug.Log($"{permissionName} allowed by the user.");
    }

    private void OnPermissionDenied(string permissionName)
    {
        // Debug.LogError($"VideoSDK can't Initialize {permissionName} Denied");

    }

    private void OnPermissionDeniedAndDontAskAgain(string permissionName)
    {
        // Debug.LogError($"VideoSDK can't Initialize {permissionName} Denied And DontAskAgain");
    }

    private void AlertNoPermission()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            if (!(Permission.HasUserAuthorizedPermission(Permission.Microphone) && Permission.HasUserAuthorizedPermission(Permission.Camera)))
            {
                Toast.Show($"You have not granted microphone or camera permission.", 3f, Color.red, ToastPosition.TopCenter);
            }
        }
    }


    private void RequestForPermission()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone) && Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                // The user authorized use of the microphone and camera.
                OnPermissionGranted(string.Empty);
            }
            else
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionDenied += OnPermissionDenied;
                callbacks.PermissionGranted += OnPermissionGranted;
                callbacks.PermissionDeniedAndDontAskAgain += OnPermissionDeniedAndDontAskAgain;
                Permission.RequestUserPermissions(new string[] { Permission.Microphone, Permission.Camera }, callbacks);
            }
        }

    }


}
