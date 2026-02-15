using UnityEngine;

// SFXPlayer — 2D звуки интерфейса и "джинглы" (start/win/lose).
// Боевые звуки (удар/урон/смерть/атака) — только 3D на префабах через CharacterSFX3D.
public class SFXPlayer : MonoBehaviour
{
    public static SFXPlayer I { get; private set; }

    [Header("AudioSource (2D)")]
    [SerializeField] private AudioSource source;

    [Header("Loot / UI Clips")]
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip medkitClip;

    [Header("Game Flow Clips")]
    [SerializeField] private AudioClip startClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        if (source == null)
            source = GetComponent<AudioSource>();

        if (source == null)
        {
            Debug.LogWarning("SFXPlayer: нет AudioSource на объекте SFXPlayer.");
            return;
        }

        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D
    }

    public void PlayCoin() => PlayOneShotSafe(coinClip);
    public void PlayMedkit() => PlayOneShotSafe(medkitClip);

    public void PlayStart() => PlayOneShotSafe(startClip);
    public void PlayWin() => PlayOneShotSafe(winClip);
    public void PlayLose() => PlayOneShotSafe(loseClip);

    private void PlayOneShotSafe(AudioClip clip)
    {
        if (source == null) return;
        if (clip == null) return;

        source.pitch = 1f;
        source.PlayOneShot(clip);
    }
}
