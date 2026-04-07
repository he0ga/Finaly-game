using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    private const string KeyResolution   = "ResolutionPreference";
    private const string KeyFullscreen   = "FullscreenPreference";
    private const string KeySensitivity  = "MouseSensitivityPreference";

    /// <summary>Default mouse sensitivity matching WASDMouseMovement default.</summary>
    private const float DefaultSensitivity = 0.15f;

    /// <summary>Slider range mapped to WASDMouseMovement.mouseSensitivity.</summary>
    private const float SensitivityMin = 0.03f;
    private const float SensitivityMax = 0.5f;

    public Dropdown resolutionDropdown;

    [Tooltip("Slider in GeneralSettings tab for mouse sensitivity.")]
    public Slider mouseSensitivitySlider;

    private Resolution[] resolutions;

    void Start()
    {
        resolutions = Screen.resolutions;
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " " + resolutions[i].refreshRate + "Hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentResolutionIndex = i;
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.RefreshShownValue();

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = SensitivityMin;
            mouseSensitivitySlider.maxValue = SensitivityMax;
            mouseSensitivitySlider.wholeNumbers = false;
        }

        LoadSettings(currentResolutionIndex);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        SaveSettings();
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        SaveSettings();
    }

    /// <summary>
    /// Called by the slider's OnValueChanged event.
    /// Saves the value and applies it to WASDMouseMovement in all loaded scenes.
    /// </summary>
    public void SetMouseSensitivity(float value)
    {
        ApplySensitivityToPlayer(value);
        SaveSettings();
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt(KeyResolution,  resolutionDropdown.value);
        PlayerPrefs.SetInt(KeyFullscreen,  System.Convert.ToInt32(Screen.fullScreen));

        if (mouseSensitivitySlider != null)
            PlayerPrefs.SetFloat(KeySensitivity, mouseSensitivitySlider.value);

        PlayerPrefs.Save();
    }

    public void LoadSettings(int currentResolutionIndex)
    {
        if (PlayerPrefs.HasKey(KeyResolution))
        {
            resolutionDropdown.value = PlayerPrefs.GetInt(KeyResolution);
            SetResolution(resolutionDropdown.value);
        }
        else
        {
            resolutionDropdown.value = currentResolutionIndex;
        }

        if (PlayerPrefs.HasKey(KeyFullscreen))
            Screen.fullScreen = System.Convert.ToBoolean(PlayerPrefs.GetInt(KeyFullscreen));
        else
            Screen.fullScreen = true;

        float sensitivity = PlayerPrefs.HasKey(KeySensitivity)
            ? PlayerPrefs.GetFloat(KeySensitivity)
            : DefaultSensitivity;

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.value = sensitivity;

        ApplySensitivityToPlayer(sensitivity);
    }

    /// <summary>
    /// Finds WASDMouseMovement in any loaded scene and applies the sensitivity.
    /// Works both from Menu (persists via PlayerPrefs) and in-game (live update).
    /// </summary>
    private static void ApplySensitivityToPlayer(float value)
    {
        WASDMouseMovement player = Object.FindObjectOfType<WASDMouseMovement>();
        if (player != null)
            player.mouseSensitivity = value;
    }
}