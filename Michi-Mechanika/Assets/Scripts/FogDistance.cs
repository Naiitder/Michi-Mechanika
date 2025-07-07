using UnityEngine;

public class FogDistance : MonoBehaviour
{
    private Transform player;
    private Vector3 offset;
    [SerializeField] private float smoothSpeed = 5f;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        offset = transform.position;
    }

    void Update()
    {
        if(player == null) return;
        
        float targetY = player.position.y + offset.y;
        float smoothedY = Mathf.Lerp(transform.position.y, targetY, smoothSpeed * Time.deltaTime);

        transform.position = new Vector3(transform.position.x, smoothedY, transform.position.z);
        
    }
}
