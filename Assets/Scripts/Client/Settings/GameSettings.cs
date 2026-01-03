using System;
using UnityEngine;

public static class GameSettings
{
    private const string PREF_PREDICTION = "Settings_MovementPrediction";

    public static event Action<bool> OnPredictionSettingChanged;

    private static bool? _useMovementPrediction;

    public static bool UseMovementPrediction
    {
        get
        {
            if (!_useMovementPrediction.HasValue)
            {
                _useMovementPrediction = PlayerPrefs.GetInt(PREF_PREDICTION, 0) == 1; // Default to OFF for stability first
            }
            return _useMovementPrediction.Value;
        }
        set
        {
            if (_useMovementPrediction != value)
            {
                _useMovementPrediction = value;
                PlayerPrefs.SetInt(PREF_PREDICTION, value ? 1 : 0);
                PlayerPrefs.Save();
                OnPredictionSettingChanged?.Invoke(value);
                Debug.Log($"[GameSettings] Movement Prediction set to: {value}");
            }
        }
    }
}
