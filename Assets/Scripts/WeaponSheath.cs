using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(100)]
public class WeaponSheath : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private Transform weaponRig;
    [SerializeField] private Transform sheathSocket;
    [SerializeField] private string weaponLayerName = "Weapon";
    [SerializeField] private float blendTime = 0.08f;

    [Header("Pose of weaponRig")]
    [SerializeField] private Vector3 animatedLocalPosition = new Vector3(0.17938927f, 0.35950994f, -0.08791083f);
    [SerializeField] private Quaternion animatedLocalRotation = new Quaternion(-0.6810162f, -0.19030766f, 0.19030759f, 0.68101615f);

    private int weaponLayer = -1;
    private float sheathWeight = 1f;

    private void OnEnable()
    {
        if (anim == null) anim = GetComponent<Animator>();
        weaponLayer = -1;
    }

    private void LateUpdate()
    {
        if (weaponRig == null || sheathSocket == null) return;
        
        if (!Application.isPlaying)
        {
            weaponRig.SetPositionAndRotation(sheathSocket.position, sheathSocket.rotation);
            return;
        }

        float target = WeaponClipPlaying() ? 0f : 1f;
        sheathWeight = blendTime <= 0f
            ? target
            : Mathf.MoveTowards(sheathWeight, target, Time.deltaTime / blendTime);

      
        Transform parent = weaponRig.parent;
        Vector3 animatedPosition = parent != null ? parent.TransformPoint(animatedLocalPosition) : animatedLocalPosition;
        Quaternion animatedRotation = parent != null ? parent.rotation * animatedLocalRotation : animatedLocalRotation;

        weaponRig.SetPositionAndRotation(
            Vector3.Lerp(animatedPosition, sheathSocket.position, sheathWeight),
            Quaternion.Slerp(animatedRotation, sheathSocket.rotation, sheathWeight));
    }

    private bool WeaponClipPlaying()
    {
        if (anim == null || anim.runtimeAnimatorController == null) return false;
        if (weaponLayer < 0)
        {
            weaponLayer = anim.GetLayerIndex(weaponLayerName);
            if (weaponLayer < 0) return false;
        }

        if (anim.GetCurrentAnimatorClipInfoCount(weaponLayer) > 0) return true;
        return anim.IsInTransition(weaponLayer) && anim.GetNextAnimatorClipInfoCount(weaponLayer) > 0;
    }

  
    [ContextMenu("Create Socket where weapon is now")]
    private void CreateSocketAtWeapon()
    {
        if (weaponRig == null) { Debug.LogWarning("[WeaponSheath] assign first the weapon rig.", this); return; }
        if (sheathSocket == null)
        {
            Debug.LogWarning("[WeaponSheath] Create the item weaponSocket and assign it.", this);
            return;
        }
        sheathSocket.SetPositionAndRotation(weaponRig.position, weaponRig.rotation);
    }
}
