using UnityEngine;

    public static class Sounds
    {
        public static void MakeSound(Sound sound)
        {
            Collider[] collider = Physics.OverlapSphere(sound.position, sound.range);
            for (int index = 0; index < collider.Length; index++)
            {
                if (collider[index].TryGetComponent(out IHear enemy)) enemy.SoundResponse(sound);
            }
        }
    }
