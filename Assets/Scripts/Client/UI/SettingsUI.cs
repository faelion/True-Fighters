using UnityEngine;
using UnityEngine.InputSystem;

namespace Client.UI
{
    public class SettingsUI : MonoBehaviour
    {
        private bool showMenu = false;

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                showMenu = !showMenu;
            }
        }

        void OnGUI()
        {
            if (!showMenu) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Box("Game Settings");

            bool currentPred = GameSettings.UseMovementPrediction;
            bool newPred = GUILayout.Toggle(currentPred, "Enable Movement Prediction (CSP)");
            if (newPred != currentPred)
            {
                GameSettings.UseMovementPrediction = newPred;
            }

            bool currentClamp = GameSettings.ClampCastToMaxRange;
            bool newClamp = GUILayout.Toggle(currentClamp, "Clamp Cast Values to Max Range");
            if (newClamp != currentClamp)
            {
                GameSettings.ClampCastToMaxRange = newClamp;
            }

            GUILayout.EndArea();
        }
    }
}
