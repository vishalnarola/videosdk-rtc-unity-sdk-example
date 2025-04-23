using EasyUI.Toast;
using live.videosdk;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private bool _micToggle;
    private bool micToggle
    {
        get => _micToggle;
        set
        {
            micBtn.image.color = value ? Color.green : Color.red;
            _micToggle = value;
        }
    }
    private bool _camToggle;
    private bool camToggle
    {
        get => _camToggle;
        set
        {
            camBtn.image.color = value ? Color.green : Color.red;
            _camToggle = value;
        }
    }

    [SerializeField] GameObject _videoSurfacePrefab;
    [SerializeField] Transform _parent;
    [SerializeField] GameObject _meetingJoinPanel;
    [SerializeField] GameObject _meetingPanel;

    [SerializeField] Button micBtn, camBtn;

    private VideoSurface _localParticipant;
    private Meeting meeting;
    private readonly string _token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJhcGlrZXkiOiI0Y2NhMmM3YS0wYmM2LTQzMmQtYTA5Zi1kZTVjNzJlNTY0YzgiLCJwZXJtaXNzaW9ucyI6WyJhbGxvd19qb2luIl0sImlhdCI6MTc0NDY4ODEwNCwiZXhwIjoxNzQ3MjgwMTA0fQ.jJT8LF724vmbd0fCU8GeHoxXSqmx9qf-TFlO3yO6s2Q";

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
        meeting.OnFetchAudioDeviceCallback += OnFetchAudioDevice;
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

    private void OnStreamDisable(StreamKind kind)
    {
        Debug.Log($"OnStreamDisable {kind}");
        camToggle = _localParticipant.CamEnabled;
        micToggle = _localParticipant.MicEnabled;
    }

    private void OnStreamEnable(StreamKind kind)
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
        Debug.Log($"OnCreateMeeting {meetingId}");
        meeting.Join(_token, meetingId, "User", true, false);
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

    private void OnMeetingStateChanged(MeetingState meetingState)
    {
        Toast.Show($"<color=yellow>MeetingStateChanged: </color> {meetingState}", 2f, ToastPosition.TopCenter);
        Debug.Log($"MeetingStateChanged: {meetingState}");
    }

    public void JoinMeeting()
    {
        if (string.IsNullOrEmpty(_meetingIdInputField.text)) return;

        // Alert the user if microphone or camera permission is not granted.
        AlertNoPermission();

        try
        {
            meeting.Join(_token, _meetingIdInputField.text, "User", true, true);
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

    public void GetAudioDevices()
    {
        OnFetchAudioDevice(new string[] { "vishal", "narola", "jemit", "savaliya" });
        meeting?.GetAudioDevices();
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
                            participant.PauseStream(StreamKind.AUDIO);
                            break;
                        }
                    case false:
                        {
                            participant.ResumeStream(StreamKind.AUDIO);
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
                            participant.PauseStream(StreamKind.VIDEO);
                            break;
                        }
                    case false:
                        {
                            participant.ResumeStream(StreamKind.VIDEO);
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

            CheckAndRequestBluetoothPermissions();
        }

    }
    public void CheckAndRequestBluetoothPermissions()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            string BluetoothConnectPermission = "android.permission.BLUETOOTH_CONNECT";
            if (AndroidVersion() >= 31) // Android 12+
            {
                if (!Permission.HasUserAuthorizedPermission(BluetoothConnectPermission))
                {
                    Permission.RequestUserPermission(BluetoothConnectPermission);
                }
                else
                {
                    Debug.Log("Bluetooth permission already granted.");
                }
            }
            else
            {
                Debug.Log("No runtime permission needed for Bluetooth below Android 12.");
            }
        }
        else
            Debug.Log("Bluetooth permission check skipped (not Android device).");
    }

    private int AndroidVersion()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
        }

        return 0;
    }

    #region Get Devices
    [Header("=== Get Devices ===")]
    [SerializeField] private DeviceCloneController devicePrefab;
    [SerializeField] private Transform deviceCloneContent;
    [SerializeField] private GameObject devicesContentPanel;
    private List<DeviceCloneController> devicesList = new List<DeviceCloneController>();
    private void OnFetchAudioDevice(string[] devices)
    {
        devicesList.ForEach(device => Destroy(device.gameObject));
        devicesList.Clear();
      
        for (int i = 0; i < devices.Length; i++)
        {
            DeviceCloneController deviceClone = Instantiate(devicePrefab, deviceCloneContent);
            devicesList.Add(deviceClone);
            deviceClone.SetData(devices[i]);
            string deviceName = devices[i];
            deviceClone.button.onClick.AddListener(() =>
            {
                OnDeviceSelect(deviceName, deviceClone);
            });
        }
        devicesContentPanel.SetActive(true);
    }

    public void OnDeviceSelect(string deviceName, DeviceCloneController deviceClone)
    {
        Debug.Log($"selected {deviceName}");
        deviceClone.SelectDevice();
        devicesContentPanel.SetActive(false);
        // Optional: Handle value change
        //Debug.Log("Dropdown value changed to: " + deviceDropdown.options[value].text);
    }

    #endregion
}


