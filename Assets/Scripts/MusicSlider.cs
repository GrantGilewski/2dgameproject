using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MusicVolumeControl : MonoBehaviour
{
    public AudioMixer mixer;
    public Slider volumeSlider;

    void Start()
    {
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    void SetVolume(float value)
    {
        // Convert slider (0–1) to decibels
        mixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
    }
}
