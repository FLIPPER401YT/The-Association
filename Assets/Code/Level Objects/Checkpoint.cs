using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] GameObject player, checkPoint;
    [SerializeField] Vector3 move;
    [SerializeField] float dead;
    // Update is called once per frame
    void Update()
    {
        if (player.transform.position.y < -dead) player.transform.position = move;
    }
    private void OnTriggerEnter(Collider other)
    {
        move = player.transform.position;
        Destroy(other.gameObject);
    }
}
