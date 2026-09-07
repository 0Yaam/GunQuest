using UnityEngine;
using GunQuest.Game;

/// <summary>Local first-person foley and readable combat/status cues, owned by each run.</summary>
[DisallowMultipleComponent]
public sealed class OperatorAudio : MonoBehaviour
{
    private GameSession session;
    private PlayerWeapon weapon;
    private PlayerHealth health;
    private CharacterController controller;
    private AudioSource source;
    private AudioClip step, landing, magazine, bolt, empty, damage, radio, warning;
    private float travelled, airtime, nextWarning;
    private bool grounded;
    private Vector3 lastPosition;

    private void Start()
    {
        session = Object.FindAnyObjectByType<GameSession>();
        weapon = GetComponent<PlayerWeapon>();
        health = GetComponent<PlayerHealth>();
        controller = GetComponent<CharacterController>();
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        source.volume = 0.6f;
        step = MakeClip("Boot on ground", 0.16f, 85f, 0.85f, 26f);
        landing = MakeClip("Landing", 0.23f, 55f, 0.65f, 17f);
        magazine = MakeClip("Magazine release", 0.19f, 280f, 0.85f, 22f);
        bolt = MakeClip("Bolt locked", 0.13f, 420f, 0.9f, 32f);
        empty = MakeClip("Empty chamber", 0.06f, 1100f, 0.75f, 50f);
        damage = MakeClip("Armor impact", 0.18f, 65f, 0.5f, 24f);
        radio = MakeClip("Radio notification", 0.1f, 1250f, 0.1f, 30f);
        warning = MakeClip("Critical vitals", 0.13f, 440f, 0f, 25f);
        grounded = controller.isGrounded;
        lastPosition = transform.position;
        weapon.ReloadStarted += OnReload;
        weapon.ReloadCompleted += OnReloaded;
        weapon.DryFired += OnEmpty;
        health.Damaged += OnDamage;
        session.Transmission += OnTransmission;
        session.StateChanged += OnState;
    }

    private void Update()
    {
        Vector3 displacement = transform.position - lastPosition;
        lastPosition = transform.position;
        displacement.y = 0;
        if (session.State != SessionState.Playing) return;
        bool onGround = controller.isGrounded;
        if (!onGround) airtime += Time.deltaTime;
        else
        {
            if (!grounded && airtime > 0.18f) Play(landing, 0.4f);
            airtime = 0;
            if (displacement.sqrMagnitude > 0.00001f && displacement.sqrMagnitude < 4f)
            {
                travelled += displacement.magnitude;
                if (travelled >= 2.1f)
                {
                    travelled %= 2.1f;
                    Play(step, controller.height < 1.5f ? 0.12f : 0.28f);
                }
            }
            else travelled = 0;
        }
        grounded = onGround;
        if (health.GetCurrentHealth() <= 25f && Time.time >= nextWarning)
        {
            nextWarning = Time.time + 2f;
            Play(warning, 0.18f);
        }
    }

    private void Play(AudioClip clip, float volume)
    {
        if (session.State == SessionState.Playing) source.PlayOneShot(clip, volume);
    }
    private void OnReload() => Play(magazine, 0.6f);
    private void OnReloaded() => Play(bolt, 0.6f);
    private void OnEmpty() => Play(empty, 0.5f);
    private void OnDamage(float amount) => Play(damage, 0.5f);
    private void OnTransmission() => Play(radio, 0.25f);
    private void OnState()
    {
        if (session.State == SessionState.Playing) source.UnPause();
        else if (session.State == SessionState.Paused) source.Pause();
        else source.Stop();
    }

    private static AudioClip MakeClip(string label, float duration, float frequency, float noiseMix, float decay)
    {
        const int rate = 22050;
        float[] samples = new float[Mathf.CeilToInt(duration * rate)];
        var random = new System.Random(Mathf.RoundToInt(frequency));
        float filtered = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2f - 1f, 0.35f);
            float envelope = Mathf.Min(1f, t * 1400f) * Mathf.Exp(-t * decay) * Mathf.Clamp01((duration - t) * 100f);
            samples[i] = (Mathf.Sin(t * frequency * Mathf.PI * 2f) * (1f - noiseMix) + filtered * noiseMix) * envelope;
        }
        var clip = AudioClip.Create(label, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (weapon != null) { weapon.ReloadStarted -= OnReload; weapon.ReloadCompleted -= OnReloaded; weapon.DryFired -= OnEmpty; }
        if (health != null) health.Damaged -= OnDamage;
        if (session != null) { session.Transmission -= OnTransmission; session.StateChanged -= OnState; }
        foreach (var clip in new[] { step, landing, magazine, bolt, empty, damage, radio, warning }) if (clip != null) Destroy(clip);
    }
}
