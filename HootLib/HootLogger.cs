using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using ILogHandler = HootLib.Interfaces.ILogHandler;
using Logger = BepInEx.Logging.Logger;

namespace HootLib
{
    public class HootLogger : ILogHandler
    {
        private readonly ManualLogSource _log;
        private readonly List<string> _ingameMessages;
        private bool _isReady;
        private string _mainMenuPrefix;

        public HootLogger(string name, string mainMenuPrefix = null)
        {
            _log = Logger.CreateLogSource(name);
            _ingameMessages = new List<string>();
            _isReady = false;
            // If we're not using a custom prefix just use the mod's name.
            if (string.IsNullOrEmpty(mainMenuPrefix))
                mainMenuPrefix = Hootils.GetPluginInfoFromAssembly(Hootils.GetAssembly()).Metadata.Name;
            _mainMenuPrefix = mainMenuPrefix;
            // Get notified as soon as the game has loaded and is ready to display in-game messages.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void Debug(string message)
        {
            _log.LogDebug(message);
        }

        public void Info(string message)
        {
            _log.LogInfo(message);
        }

        public void Warn(string message)
        {
            _log.LogWarning(message);
        }

        public void Error(string message)
        {
            _log.LogError(message);
        }

        public void Fatal(string message)
        {
            _log.LogFatal(message);
        }
        
        /// <summary>Send an in-game message to the player.</summary>
        public void InGameMessage(string message, bool error = false)
        {
            message = $"[{_mainMenuPrefix}] {message}";
            _log.LogMessage("Main Menu Message: " + message);

            // Colour the message a deep red if it is an error.
            if (error)
                message = $"<color=#FF0000>{message}</color>";
            if (!_isReady)
                _ingameMessages.Add(message);
            else
                ErrorMessage.AddMessage(message);
        }

        private IEnumerator DisplayDelayedMessages()
        {
            yield return new WaitForSeconds(3f);
            _isReady = true;

            foreach (string message in _ingameMessages)
            {
                ErrorMessage.AddMessage(message);
            }

            _ingameMessages.Clear();
        }

        /// <summary>
        /// Get notified and proceed as soon as the main menu scene has loaded.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "XMenu")
                return;

            // Remove this event listener.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            // Increase the vanilla fadeout time for messages.
            ErrorMessage.main.timeDelay = 10f;
            ErrorMessage.main.StartCoroutine(DisplayDelayedMessages());
        }
    }
}