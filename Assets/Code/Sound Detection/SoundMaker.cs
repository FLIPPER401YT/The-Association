using UnityEngine;

namespace SoundMakerSpace
{
    public class SoundMaker : MonoBehaviour
    {
        [SerializeField] private AudioSource sound;
        [SerializeField] private float range;

        private void OnMouseDown()
        {
            if (sound.isPlaying) return;
            sound.Play();
            var noise = new Sound(transform.position, range);
            Sounds.MakeSound(noise);
        }
    }
}
