using UnityEngine;
using System.IO;
using System;

namespace ArtoonGinRummy
{
    public class CustomLogSaver : MonoBehaviour
    {

        string filename = "";
        [SerializeField] string logFileName;
        private void Awake()
        {
            string dynamicFileName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Debug.Log($"filename {dynamicFileName}");
            filename = Application.persistentDataPath + $"/{logFileName}_{dynamicFileName}.txt";
            if (File.Exists(filename))
                File.Delete(filename);
            Debug.Log(filename);
        }
        private void OnDisable()
        {
            Application.logMessageReceived -= Log;
        }
        private void OnEnable()
        {
            Application.logMessageReceived += Log;
        }

        public void Log(string LogMsg, string logStack, LogType log)
        {
            if (SystemInfo.deviceName == "Galaxy A13" || SystemInfo.deviceUniqueIdentifier == "5f33502fe67d4f97") return;

            TextWriter tw = new StreamWriter(filename, true);

            tw.WriteLine("[" + System.DateTime.Now + "]" + LogMsg);

            tw.Close();
        }
    }
}
