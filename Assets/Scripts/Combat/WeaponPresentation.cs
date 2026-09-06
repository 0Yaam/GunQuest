using UnityEngine;

public sealed class WeaponPresentation : MonoBehaviour
{
    public PlayerWeapon weapon;
    public Transform viewModel;
    private InputManager input;
    private CharacterController controller;
    private AudioSource audioSource;
    private AudioClip shotClip, hitClip;
    private float recoil;
    private float muzzleUntil;
    private Light muzzleLight;
    private Vector3 home;

    private void Start()
    {
        input = weapon.GetComponent<InputManager>();
        controller = weapon.GetComponent<CharacterController>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.35f;
        shotClip = Tone("Rifle shot", 0.15f, 90f, true);
        hitClip = Tone("Hit confirmation", 0.08f, 900f, false);
        home = viewModel.localPosition;
        muzzleLight = weapon.muzzle.gameObject.AddComponent<Light>();
        muzzleLight.type = LightType.Point;
        muzzleLight.color = new Color(0.3f, 1f, 0.85f);
        muzzleLight.range = 4f;
        muzzleLight.intensity = 0f;
        weapon.Fired += OnFired;
        weapon.Hit += OnHit;
    }

    private void LateUpdate()
    {
        if (Time.timeScale == 0f) return;
        recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 7f);
        muzzleLight.intensity = Time.unscaledTime < muzzleUntil ? 5f : 0f;
        bool aiming = input.IsAiming;
        float bob = controller.isGrounded ? Mathf.Min(controller.velocity.magnitude, 8f) * 0.0015f : 0f;
        Vector3 target = aiming ? new Vector3(0f, -0.19f, home.z + 0.08f) : home;
        target += new Vector3(Mathf.Sin(Time.time * 8f) * bob, Mathf.Abs(Mathf.Cos(Time.time * 8f)) * bob, -recoil * 0.07f);
        viewModel.localPosition = Vector3.Lerp(viewModel.localPosition, target, Time.deltaTime * 14f);
        viewModel.localRotation = Quaternion.Euler(-recoil * 6f + (weapon.IsReloading ? 24f : 0f), 0f, weapon.IsReloading ? -25f : 0f);
        weapon.aimCamera.fieldOfView = Mathf.Lerp(weapon.aimCamera.fieldOfView, aiming ? 52f : 75f, Time.deltaTime * 10f);
        var cameraPosition = weapon.aimCamera.transform.localPosition;
        cameraPosition.y = Mathf.Lerp(cameraPosition.y, controller.height - 0.3f, Time.deltaTime * 12f);
        weapon.aimCamera.transform.localPosition = cameraPosition;
    }

    private void OnFired() { recoil = 1f; muzzleUntil = Time.unscaledTime + 0.045f; audioSource.pitch = Random.Range(0.95f, 1.05f); audioSource.PlayOneShot(shotClip); }
    private void OnHit(bool killed) => audioSource.PlayOneShot(hitClip, killed ? 0.7f : 0.4f);

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
        if (weapon != null) { weapon.Fired -= OnFired; weapon.Hit -= OnHit; }
        if (shotClip != null) Destroy(shotClip);
        if (hitClip != null) Destroy(hitClip);
    }
}
