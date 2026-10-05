using UnityEngine;

public class LoadingScreenAnimator : MonoBehaviour
{
    [SerializeField] Transform gear;
    [SerializeField] Transform needle;
 
    [Header("Gear")]
    [SerializeField] float gearSpeed = 45f;      
 
    [Header("Needle")]
    [SerializeField] float needleMin = -100f;    
    [SerializeField] float needleMax = 100f;     
    [SerializeField] float needleCycle = 2.5f;   
 
    [SerializeField] bool useUnscaledTime = true; 
 
    float t;
 
    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        t += dt;
 
        if (gear != null)
            gear.Rotate(0f, 0f, -gearSpeed * dt);
 
        if (needle != null)
        {
            float k = (Mathf.Sin(t * Mathf.PI * 2f / needleCycle) + 1f) * 0.5f;
            float angle = Mathf.Lerp(needleMin, needleMax, k);
            needle.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }
    }
}
