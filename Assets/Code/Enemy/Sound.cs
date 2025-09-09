using UnityEngine;

 public class Sound
    {
        public readonly Vector3 position;
        public readonly float range;
        public SoundType type;
        public Sound(Vector3 Position, float Range)
        {
            position = Position;
            range = Range;
        }
        public enum SoundType { Default = -1, Player, Rock }
    }
