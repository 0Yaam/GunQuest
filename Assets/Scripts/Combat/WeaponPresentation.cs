using UnityEngine;

public sealed class WeaponPresentation : MonoBehaviour
{
    public PlayerWeapon weapon;
    public Transform viewModel;
    private InputManager input;
    private CharacterController controller;
    private AudioSource audioSource;
    private AudioClip shotClip, hitClip, reloadClip, readyClip, dryClip;
    private float recoil;
    private float recoilVelocity;
    private float horizontalKick;
    private float rollKick;
    private float muzzleUntil;
    private Light muzzleLight;
    private ParticleSystem muzzleFlash;
    private ParticleSystem muzzleSmoke;
    private Material flashMaterial;
    private Material smokeMaterial;
    private Vector3 home;
    private PlayerOptions options;
    private int shotCounter;

    private void Start()
    {
        input = weapon.GetComponent<InputManager>();
        controller = weapon.GetComponent<CharacterController>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.42f;
        audioSource.spatialBlend = 0f;
        shotClip = RifleShot();
        hitClip = Tone("Hit confirmation", 0.08f, 900f, false);
        reloadClip = Tone("Magazine release", 0.12f, 185f, true);
        readyClip = Tone("Bolt ready", 0.10f, 430f, true);
        dryClip = Tone("Dry trigger", 0.055f, 160f, true);
        home = viewModel.localPosition;
        options = Object.FindAnyObjectByType<PlayerOptions>();
        muzzleLight = weapon.muzzle.gameObject.AddComponent<Light>();
        muzzleLight.type = LightType.Point;
        muzzleLight.color = new Color(0.22f, 0.95f, 0.78f);
        muzzleLight.range = 5.5f;
        muzzleLight.intensity = 0f;
        muzzleLight.shadows = LightShadows.None;
        CreateMuzzleEffects();
        CombatFeedback.Warmup();
        weapon.Fired += OnFired;
        weapon.Hit += OnHit;
        weapon.ReloadStarted += OnReloadStarted;
        weapon.ReloadCompleted += OnReloadCompleted;
        weapon.DryFired += OnDryFired;
    }

    private void LateUpdate()
    {
        muzzleLight.intensity = Time.timeScale > 0f && Time.unscaledTime < muzzleUntil ? 11f : 0f;
        if (Time.timeScale == 0f) return;
        bool reduced = options != null && options.ReducedMotion;
        recoil = Mathf.SmoothDamp(recoil, 0f, ref recoilVelocity, reduced ? 0.045f : 0.075f, 30f, Time.deltaTime);
        horizontalKick = Mathf.MoveTowards(horizontalKick, 0f, Time.deltaTime * 4.5f);
        rollKick = Mathf.MoveTowards(rollKick, 0f, Time.deltaTime * 7f);
        bool aiming = input.IsAiming;
        float bob = controller.isGrounded && !reduced ? Mathf.Min(controller.velocity.magnitude, 8f) * 0.0015f : 0f;
        float reloadPose = weapon.IsReloading ? Mathf.Sin(weapon.ReloadProgress * Mathf.PI) : 0f;
        Vector3 target = aiming ? new Vector3(0f, -0.19f, home.z + 0.08f) : home;
        target += new Vector3(Mathf.Sin(Time.time * 8f) * bob + horizontalKick * 0.012f,
            Mathf.Abs(Mathf.Cos(Time.time * 8f)) * bob - reloadPose * 0.035f,
            -recoil * (reduced ? 0.018f : 0.065f));
        viewModel.localPosition = Vector3.Lerp(viewModel.localPosition, target, Time.deltaTime * 16f);
        viewModel.localRotation = Quaternion.Euler(-recoil * (reduced ? 1.2f : 6.5f) + reloadPose * 19f,
            horizontalKick * (reduced ? 0.5f : 2.5f),
            rollKick * (reduced ? 0.8f : 3f) - reloadPose * 28f);
        float fov = options != null ? options.FieldOfView : 75f;
        weapon.aimCamera.fieldOfView = Mathf.Lerp(weapon.aimCamera.fieldOfView, aiming ? fov * 0.6933f : fov, Time.deltaTime * 10f);
        var cameraPosition = weapon.aimCamera.transform.localPosition;
        cameraPosition.y = Mathf.Lerp(cameraPosition.y, controller.height - 0.3f, Time.deltaTime * 12f);
        weapon.aimCamera.transform.localPosition = cameraPosition;
    }

    private void OnFired()
    {
        recoil = Mathf.Min(1.35f, recoil + 0.78f);
        recoilVelocity = 0f;
        horizontalKick = Random.Range(-1f, 1f);
        rollKick = Random.Range(-1f, 1f);
        muzzleUntil = Time.unscaledTime + 0.04f;
        audioSource.pitch = Random.Range(0.965f, 1.035f);
        audioSource.PlayOneShot(shotClip);
        var flash = new ParticleSystem.EmitParams
        {
            startColor = new Color(0.58f, 1.08f, 0.82f, 1f),
            startLifetime = 0.045f,
            startSize = Random.Range(0.10f, 0.145f),
            rotation = Random.Range(0f, Mathf.PI * 2f)
        };
        muzzleFlash.Emit(flash, 1);
        if ((shotCounter++ & 1) == 0)
        {
            var smoke = new ParticleSystem.EmitParams
            {
                startColor = new Color(0.72f, 0.79f, 0.80f, 0.55f),
                startLifetime = Random.Range(0.35f, 0.55f),
                startSize = Random.Range(0.055f, 0.09f),
                velocity = weapon.muzzle.forward * Random.Range(0.16f, 0.34f) + Vector3.up * 0.08f
            };
            muzzleSmoke.Emit(smoke, 1);
        }
    }
    private void OnHit(bool killed) => audioSource.PlayOneShot(hitClip, killed ? 0.7f : 0.4f);
    private void OnReloadStarted() { audioSource.pitch = 0.92f; audioSource.PlayOneShot(reloadClip, 0.45f); }
    private void OnReloadCompleted() { audioSource.pitch = 1.04f; audioSource.PlayOneShot(readyClip, 0.42f); }
    private void OnDryFired() { audioSource.pitch = Random.Range(0.96f, 1.03f); audioSource.PlayOneShot(dryClip, 0.5f); }

    private void CreateMuzzleEffects()
    {
        flashMaterial = RuntimeMaterial("MuzzleFlash", "GunQuest/Muzzle Flash", "Universal Render Pipeline/Particles/Unlit");
        flashMaterial.SetFloat("_Intensity", 3.2f);
        var flashObject = new GameObject("Pooled muzzle flash");
        flashObject.transform.SetParent(weapon.muzzle, false);
        muzzleFlash = flashObject.AddComponent<ParticleSystem>();
        var flashMain = muzzleFlash.main;
        flashMain.loop = false;
        flashMain.playOnAwake = false;
        flashMain.simulationSpace = ParticleSystemSimulationSpace.World;
        flashMain.startSpeed = 0f;
        flashMain.maxParticles = 8;
        var flashEmission = muzzleFlash.emission;
        flashEmission.enabled = false;
        var flashShape = muzzleFlash.shape;
        flashShape.enabled = false;
        var flashRenderer = muzzleFlash.GetComponent<ParticleSystemRenderer>();
        flashRenderer.sharedMaterial = flashMaterial;
        flashRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        flashRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        flashRenderer.receiveShadows = false;

        smokeMaterial = RuntimeMaterial("WeaponSmoke", "GunQuest/Weapon Smoke", "Universal Render Pipeline/Particles/Unlit");
        var smokeObject = new GameObject("Pooled muzzle smoke");
        smokeObject.transform.SetParent(weapon.muzzle, false);
        muzzleSmoke = smokeObject.AddComponent<ParticleSystem>();
        var smokeMain = muzzleSmoke.main;
        smokeMain.loop = false;
        smokeMain.playOnAwake = false;
        smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;
        smokeMain.startSpeed = 0f;
        smokeMain.gravityModifier = -0.035f;
        smokeMain.maxParticles = 12;
        var smokeEmission = muzzleSmoke.emission;
        smokeEmission.enabled = false;
        var smokeShape = muzzleSmoke.shape;
        smokeShape.enabled = false;
        var smokeSize = muzzleSmoke.sizeOverLifetime;
        smokeSize.enabled = true;
        smokeSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 2.3f));
        var smokeColor = muzzleSmoke.colorOverLifetime;
        smokeColor.enabled = true;
        smokeColor.color = new ParticleSystem.MinMaxGradient(new Gradient
        {
            alphaKeys = new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.6f, 0.12f), new GradientAlphaKey(0f, 1f) },
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.7f, 0.76f, 0.8f), 1f) }
        });
        var smokeRenderer = muzzleSmoke.GetComponent<ParticleSystemRenderer>();
        smokeRenderer.sharedMaterial = smokeMaterial;
        smokeRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        smokeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        smokeRenderer.receiveShadows = false;
    }

    private static Material RuntimeMaterial(string resourceName, string shaderName, string fallback)
    {
        var template = Resources.Load<Material>(resourceName);
        if (template != null) return new Material(template) { name = shaderName + " runtime" };
        var shader = Shader.Find(shaderName);
        if (shader == null) shader = Shader.Find(fallback);
        return new Material(shader) { name = shaderName + " runtime" };
    }

    private static AudioClip RifleShot()
    {
        const int rate = 44100;
        const float duration = 0.28f;
        var samples = new float[Mathf.CeilToInt(rate * duration)];
        var random = new System.Random(3107);
        float previousNoise = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate;
            float noise = (float)random.NextDouble() * 2f - 1f;
            previousNoise = Mathf.Lerp(previousNoise, noise, 0.32f);
            float crack = noise * Mathf.Exp(-t * 82f);
            float body = Mathf.Sin(t * 118f * Mathf.PI * 2f) * Mathf.Exp(-t * 24f);
            float mechanism = previousNoise * Mathf.Exp(-Mathf.Abs(t - 0.035f) * 95f) * 0.22f;
            float tail = Mathf.Sin(t * 57f * Mathf.PI * 2f) * Mathf.Exp(-t * 11f) * 0.16f;
            samples[i] = Mathf.Clamp((crack * 0.68f + body * 0.52f + mechanism + tail) * Mathf.Min(1f, t * 3000f), -1f, 1f);
        }
        var clip = AudioClip.Create("Layered GQ-30 shot", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip Tone(string name, float duration, float frequency, bool noise)
    {
        const int rate = 22050;
        var samples = new float[Mathf.CeilToInt(rate * duration)];
        var random = new System.Random(42);
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / rate;
            float envelope = Mathf.Exp(-t * (noise ? 35f : 45f)) * Mathf.Min(1f, t * 1500f);
            samples[i] = envelope * (Mathf.Sin(t * frequency * 2f * Mathf.PI) * 0.45f + (noise ? ((float)random.NextDouble() * 2f - 1f) * 0.55f : 0f));
        }
        var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (weapon != null)
        {
            weapon.Fired -= OnFired;
            weapon.Hit -= OnHit;
            weapon.ReloadStarted -= OnReloadStarted;
            weapon.ReloadCompleted -= OnReloadCompleted;
            weapon.DryFired -= OnDryFired;
        }
        if (shotClip != null) Destroy(shotClip);
        if (hitClip != null) Destroy(hitClip);
        if (reloadClip != null) Destroy(reloadClip);
        if (readyClip != null) Destroy(readyClip);
        if (dryClip != null) Destroy(dryClip);
        if (flashMaterial != null) Destroy(flashMaterial);
        if (smokeMaterial != null) Destroy(smokeMaterial);
    }
}
