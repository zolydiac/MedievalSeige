using UnityEngine;

public class GameMusic : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

    private void Awake()
    {
        // Optional singleton so you don't get doubles if you ever load scenes
        var existing = FindObjectOfType<GameMusic>();
        if (existing != null && existing != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = volume;
        musicSource.spatialBlend = 0f; // 2D sound
        musicSource.ignoreListenerPause = false;

        musicSource.Play();
    }
}
