using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Ses seviyesini slider'dan alır, mixer'a uygular ve kaydeder
public class VolumeSettings : MonoBehaviour
{
    private const string VolumeKey = "MasterVolume";
    private const string MixerParameter = "MasterVolume";   // Mixer'da verdiğin ad
    private const float DefaultVolume = 0.8f;

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider slider;

    private void Start()
    {
        float saved = PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
        slider.minValue = 0.0001f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(saved);
        Apply(saved);
    }

    private void OnEnable() => slider.onValueChanged.AddListener(OnSliderChanged);
    private void OnDisable() => slider.onValueChanged.RemoveListener(OnSliderChanged);

    private void OnSliderChanged(float value)
    {
        Apply(value);
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    // Slider 0..1 doğrusal, mixer desibel (log) ister
    private void Apply(float value) =>
        mixer.SetFloat(MixerParameter, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
}