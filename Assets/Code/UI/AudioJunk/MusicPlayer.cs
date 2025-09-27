using UnityEngine;
using UnityEngine.Rendering;

public class MenuAudio : MonoBehaviour
{
    [SerializeField] AudioSource musicSource;

    public AudioClip music;

    private double musicDuration;
    private double time;

    private void Start()
    {
        time = AudioSettings.dspTime + 0.5;

        musicSource.clip = music;
        musicSource.PlayScheduled(time);

        musicDuration = (double)music.samples / music.frequency;
        time = time + musicDuration;
    }

    private void Update()
    {
        if(AudioSettings.dspTime > time - 0.25)
        {
            musicSource.clip = music;
            musicSource.PlayScheduled(time);

            musicDuration = (double)music.samples / music.frequency;
            time = time + musicDuration;
        }
    }
}
