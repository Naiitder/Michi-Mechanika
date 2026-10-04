using UnityEngine;

/// <summary>
/// Mantiene el arma enfundada pegada a un hueso (la espalda) mientras la capa
/// "Weapon" del Animator no esté reproduciendo ningún clip de arma.
/// Cuando hay clip de arma (ataque / enfundar), suelta el arma y manda la animación.
/// Va en el mismo objeto que el Animator (la raíz del Player).
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(100)]
public class WeaponSheath : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [Tooltip("Player/WeaponRig (hijo directo del Player)")]
    [SerializeField] private Transform weaponRig;
    [Tooltip("Objeto vacío hijo del hueso de la espalda (p. ej. CC_Base_Spine02). Marca dónde va el arma enfundada.")]
    [SerializeField] private Transform sheathSocket;
    [SerializeField] private string weaponLayerName = "Weapon";
    [Tooltip("Segundos que tarda en pasar de la espalda a la animación y viceversa")]
    [SerializeField] private float blendTime = 0.08f;

    [Header("Pose del WeaponRig durante los clips de arma (la del FBX, relativa al Player)")]
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

        // En el editor (sin Play) el arma sigue siempre al socket, para poder colocarlo a ojo.
        if (!Application.isPlaying)
        {
            weaponRig.SetPositionAndRotation(sheathSocket.position, sheathSocket.rotation);
            return;
        }

        float target = WeaponClipPlaying() ? 0f : 1f;
        sheathWeight = blendTime <= 0f
            ? target
            : Mathf.MoveTowards(sheathWeight, target, Time.deltaTime / blendTime);

        // Pose que tiene el WeaponRig en los clips (es constante en todos): no dependemos de que
        // el Animator la escriba, porque en el prefab el WeaponRig está guardado en la pose del socket.
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

    /// <summary>Crea (o recoloca) el socket justo donde está ahora el WeaponRig.</summary>
    [ContextMenu("Crear socket donde está el arma ahora")]
    private void CreateSocketAtWeapon()
    {
        if (weaponRig == null) { Debug.LogWarning("Asigna primero Weapon Rig.", this); return; }
        if (sheathSocket == null)
        {
            Debug.LogWarning("Crea un objeto vacío hijo del hueso de la espalda y asígnalo en Sheath Socket.", this);
            return;
        }
        sheathSocket.SetPositionAndRotation(weaponRig.position, weaponRig.rotation);
    }
}
