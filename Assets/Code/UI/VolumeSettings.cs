using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer myMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundEffectsSlider;
    [SerializeField] private Slider menuSoundEffectSlider;

    private void Start()
    {
        if(PlayerPrefs.HasKey("soundEffectsVolume"))
        {
            LoadVolume();
        }
        else
        {
            SetMusicVolume();
            SetSoundEffectVolume();
            SetMenuSoundEffectVolume();
        }
    }
    public void SetMusicVolume()
    {
        float volume = musicSlider.value;
        myMixer.SetFloat("Music", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("musicVolume", volume);
    }

    public void SetSoundEffectVolume()
    {
        float volume = soundEffectsSlider.value;
        myMixer.SetFloat("SoundEffects", Mathf.Log10(volume)*20);
        PlayerPrefs.SetFloat("soundEffectsVolume", volume);
    }
    public void SetMenuSoundEffectVolume()
    {
        float volume = menuSoundEffectSlider.value;
        myMixer.SetFloat("MenuSoundEffects", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("menuSoundEffectsVolume", volume);
    }

    private void LoadVolume()
    {
        musicSlider.value = PlayerPrefs.GetFloat("musicVolume");
        soundEffectsSlider.value = PlayerPrefs.GetFloat("soundEffectsVolume");
        menuSoundEffectSlider.value = PlayerPrefs.GetFloat("menuSoundEffectSVolume");

        SetMusicVolume();
        SetSoundEffectVolume();
        SetMenuSoundEffectVolume();
    }
}
