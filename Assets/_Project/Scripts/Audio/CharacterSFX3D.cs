using UnityEngine;

// CharacterSFX3D — 3D звуки персонажа (враг/игрок).
// Вешается на ПРЕФАБ (Enemy / Player). Внутри должен быть AudioSource с Spatial Blend = 1 (3D).
public class CharacterSFX3D : MonoBehaviour
{
    [Header("AudioSource (3D)")]
    [SerializeField] private AudioSource source;

    [Header("Clips")]
    [SerializeField] private AudioClip[] hitClips;    // получение удара
    [SerializeField] private AudioClip[] attackClips; // атака
    [SerializeField] private AudioClip[] deathClips;  // смерть

    [Header("Randomize (чтобы не звучало одинаково)")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private void Awake()
    {
        if (source == null)
            source = GetComponent<AudioSource>();

        if (source == null)
            Debug.LogWarning("CharacterSFX3D: нет AudioSource на объекте.");
    }

    public void PlayHit() => PlayRandom(hitClips);
    public void PlayAttack() => PlayRandom(attackClips);
    public void PlayDeath() => PlayRandom(deathClips);

    private void PlayRandom(AudioClip[] clips)
    {
        if (source == null) return;
        if (clips == null || clips.Length == 0) return;

        var clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        float oldPitch = source.pitch;
        source.pitch = Random.Range(pitchRange.x, pitchRange.y);

        source.PlayOneShot(clip, volume);

        source.pitch = oldPitch;
    }
}

