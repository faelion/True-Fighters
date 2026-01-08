using System;
using UnityEngine;

public static class GameSettings
{
    private const string PREF_PREDICTION = "Settings_MovementPrediction";
    private const string PREF_CLAMP_CAST = "Settings_ClampCastToMaxRange";

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

    private static bool? _clampCast;
    public static bool ClampCastToMaxRange
    {
        get
        {
            if (!_clampCast.HasValue)
            {
                _clampCast = PlayerPrefs.GetInt(PREF_CLAMP_CAST, 1) == 1; // Default True
            }
            return _clampCast.Value;
        }
        set
        {
            if (_clampCast != value)
            {
                _clampCast = value;
                PlayerPrefs.SetInt(PREF_CLAMP_CAST, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
