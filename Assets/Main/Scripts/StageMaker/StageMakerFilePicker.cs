using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StageMaker
{
    public static class StageMakerFilePicker
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void StageMakerSelectJsonFile(string receiverName, string successMethod, string errorMethod);
#endif

        public static void PickJson(MonoBehaviour owner, Action<string> onJson, Action<string> onError)
        {
            if (owner == null)
            {
                onError?.Invoke("Import failed: no receiver.");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            var receiver = owner.gameObject.AddComponent<WebGLFilePickerReceiver>();
            receiver.Initialize(onJson, onError);
            StageMakerSelectJsonFile(owner.gameObject.name, nameof(WebGLFilePickerReceiver.OnStageMakerFileLoaded), nameof(WebGLFilePickerReceiver.OnStageMakerFileError));
#else
            owner.StartCoroutine(PickJsonDesktop(onJson, onError));
#endif
        }

#if !(UNITY_WEBGL && !UNITY_EDITOR)
        private static IEnumerator PickJsonDesktop(Action<string> onJson, Action<string> onError)
        {
            string path = PickDesktopPath();
            yield return null;

            if (string.IsNullOrEmpty(path)) { yield break; }
            try
            {
                onJson?.Invoke(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                onError?.Invoke("Import failed: " + ex.Message);
            }
        }

        private static string PickDesktopPath()
        {
#if UNITY_EDITOR
            return EditorUtility.OpenFilePanel("Import Stage JSON", string.Empty, "json");
#else
            if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                return RunProcess("/usr/bin/osascript",
                    "-e",
                    "POSIX path of (choose file with prompt \"Import stage JSON\" of type {\"json\"})");
            }

            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                const string script =
                    "Add-Type -AssemblyName System.Windows.Forms; " +
                    "$d=New-Object System.Windows.Forms.OpenFileDialog; " +
                    "$d.Filter='Stage JSON (*.json)|*.json|All files (*.*)|*.*'; " +
                    "$d.Multiselect=$false; " +
                    "if($d.ShowDialog() -eq 'OK'){ [Console]::Write($d.FileName) }";
                return RunProcess("powershell.exe", "-NoProfile", "-STA", "-Command", script);
            }

            return string.Empty;
#endif
        }

        private static string RunProcess(string fileName, params string[] arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                foreach (string argument in arguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }

                using var process = Process.Start(startInfo);
                if (process == null) { return string.Empty; }
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0 ? output.Trim() : string.Empty;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[StageMakerFilePicker] " + ex.Message);
                return string.Empty;
            }
        }
#endif
    }

    public class WebGLFilePickerReceiver : MonoBehaviour
    {
        private Action<string> onJson;
        private Action<string> onError;

        public void Initialize(Action<string> success, Action<string> error)
        {
            onJson = success;
            onError = error;
        }

        public void OnStageMakerFileLoaded(string json)
        {
            onJson?.Invoke(json);
            Destroy(this);
        }

        public void OnStageMakerFileError(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                onError?.Invoke(message);
            }
            Destroy(this);
        }
    }
}
