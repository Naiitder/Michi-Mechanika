using UnityEngine;

public class CharacterAudio : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource stepSource;
    [SerializeField] private AudioSource swingSource;
    [SerializeField] private AudioSource dieSource;
    
    [Header("Footsteps")]
    [SerializeField] private AudioClip[] floorToFloorSteps;
    [SerializeField] private AudioClip[] floorToRoofSteps;
    [SerializeField] private AudioClip[] roofToFloorSteps;
    [SerializeField] private AudioClip[] roofToRoofSteps;
    
    [Header("Combat")]
    [SerializeField] private AudioClip[] attackSwings;
    [SerializeField] private AudioClip[] die;
    
    [Header("Pitch")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    
    public void PlayStep(Tile.Type from, Tile.Type to)
    {
        AudioClip[] clips;
        if (from == Tile.Type.Floor && to == Tile.Type.Floor)      clips = floorToFloorSteps;
        else if (from == Tile.Type.Floor && to == Tile.Type.Roof)  clips = floorToRoofSteps;
        else if (from == Tile.Type.Roof && to == Tile.Type.Floor)  clips = roofToFloorSteps;
        else                                                       clips = roofToRoofSteps;
        Play(stepSource, clips);
    }

    public void PlaySwing() => Play(swingSource, attackSwings); 
    public void PlayDeath() => Play(dieSource, die); 

    private void Play(AudioSource source, AudioClip[] clips)
    {
        if (source == null || clips == null || clips.Length == 0) return;
        source.pitch = Random.Range(minPitch, maxPitch);
        source.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }
}
