using GunQuest.Game;
using UnityEngine;

/// <summary>Small procedural soundscape so every generated mission has an audio identity without external files.</summary>
[DisallowMultipleComponent]
public sealed class MissionSoundscape : MonoBehaviour
{
    private GameSession session;
    private AudioSource ambience;
    private AudioSource stinger;
    private AudioClip ambienceClip, victoryClip, defeatClip;
    private float targetVolume;

    private void Awake()
    {
        session = GetComponent<GameSession>();
        AudioListener.volume = PlayerPrefs.GetFloat("GunQuest.Audio", 0.75f);
        ambience = gameObject.AddComponent<AudioSource>();
        ambience.loop = true;
        ambience.playOnAwake = false;
        ambience.spatialBlend = 0f;
        ambienceClip = CreateAmbience(session.MissionIndex);
        ambience.clip = ambienceClip;
        ambience.volume = 0f;
        ambience.Play();
        stinger = gameObject.AddComponent<AudioSource>();
        stinger.playOnAwake = false;
        stinger.spatialBlend = 0f;
        stinger.volume = 0.32f;
        victoryClip = CreateStinger("Operation secured", true);
        defeatClip = CreateStinger("Signal lost", false);
        session.StateChanged += OnStateChanged;
    }

    private void Update()
    {
        ambience.volume = Mathf.MoveTowards(ambience.volume, targetVolume, Time.unscaledDeltaTime * 0.08f);
    }

    private void OnStateChanged()
    {
        targetVolume = session.State == SessionState.Playing ? 0.18f : session.State == SessionState.Paused ? 0.045f : 0.1f;
        if (session.State == SessionState.Victory) stinger.PlayOneShot(victoryClip);
        else if (session.State == SessionState.Defeat) stinger.PlayOneShot(defeatClip);
    }

    private static AudioClip CreateAmbience(int mission)
    {
        const int rate = 22050;
        const float duration = 8f;
        var samples = new float[Mathf.RoundToInt(rate * duration)];
        var random = new System.Random(711 + mission * 97);
        float filteredNoise = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate;
            float noise = (float)random.NextDouble() * 2f - 1f;
            filteredNoise = Mathf.Lerp(filteredNoise, noise, mission == 1 ? 0.004f : 0.012f);
            float value;
            if (mission == 1)
            {
                float distantCall = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 0.5f - 0.9f)), 18f) * Mathf.Sin(t * 1450f);
                value = filteredNoise * 0.26f + Mathf.Sin(t * 47f * Mathf.PI * 2f) * 0.035f + distantCall * 0.045f;
            }
            else if (mission == 2)
            {
                float pulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 2f)), 10f);
                value = filteredNoise * 0.10f + Mathf.Sin(t * 55f * Mathf.PI * 2f) * (0.025f + pulse * 0.035f)
                    + Mathf.Sin(t * 110f * Mathf.PI * 2f) * pulse * 0.018f;
            }
            else
            {
                value = filteredNoise * 0.23f + Mathf.Sin(t * 42f * Mathf.PI * 2f) * 0.025f;
            }
            samples[i] = Mathf.Clamp(value, -0.22f, 0.22f);
        }
        var clip = AudioClip.Create("Mission ambience " + mission, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateStinger(string name, bool victory)
    {
        const int rate = 22050;
        const float duration = 1.8f;
        var samples = new float[Mathf.RoundToInt(rate * duration)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate;
            int step = Mathf.Min(2, Mathf.FloorToInt(t / 0.42f));
            float[] high = { 220f, 277f, 330f };
            float[] low = { 196f, 155f, 116f };
            float frequency = victory ? high[step] : low[step];
            float local = t - step * 0.42f;
            float envelope = Mathf.Clamp01(local * 20f) * Mathf.Exp(-local * 2.8f) * Mathf.Clamp01((duration - t) * 3f);
            samples[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * envelope * 0.2f;
        }
        var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (session != null) session.StateChanged -= OnStateChanged;
        if (ambienceClip != null) Destroy(ambienceClip);
        if (victoryClip != null) Destroy(victoryClip);
        if (defeatClip != null) Destroy(defeatClip);
    }
}
