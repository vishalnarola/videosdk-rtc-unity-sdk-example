using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeviceCloneController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI deviceNameText;
    [SerializeField] private Image checkBox;
    [SerializeField] private Sprite select;
    public Button button;

    public void SetData(string deviceName)
    {
        deviceNameText.text = deviceName;
        checkBox.sprite = null;
    }

    public void SelectDevice()
    {
        checkBox.sprite = select;
    }
}
