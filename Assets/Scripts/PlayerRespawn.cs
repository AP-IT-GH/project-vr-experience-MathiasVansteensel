using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerFallRespawn : MonoBehaviour
{
    [Header("References")]
    public Transform player;             // Assign the Player object here
    public Transform spawnPoint;         // Assign the respawn point
    public Canvas fadeCanvas;            // The canvas with the fade image
    public Image fadeImage;              // The black full-screen image inside the canvas

    [Header("Settings")]
    public float fallThresholdY = 2f;
    public float fadeDuration = 1f;

    private bool isRespawning = false;

    private void Start()
    {
        // Ensure canvas starts disabled and fully transparent
        fadeCanvas.gameObject.SetActive(false);
        SetFadeAlpha(0f);
    }

    private void Update()
    {
        if (!isRespawning && player.position.y <= fallThresholdY)
        {
            StartCoroutine(RespawnSequence());
        }
    }

    private IEnumerator RespawnSequence()
    {
        isRespawning = true;

        // Enable canvas
        fadeCanvas.gameObject.SetActive(true);

        // Fade to black
        yield return StartCoroutine(Fade(0f, 1f));

        // Teleport player
        player.position = spawnPoint.position;

        // Optional: Reset velocity if Rigidbody exists
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Wait a short moment (optional)
        yield return new WaitForSeconds(0.1f);

        // Fade back in
        yield return StartCoroutine(Fade(1f, 0f));

        // Disable canvas
        fadeCanvas.gameObject.SetActive(false);

        isRespawning = false;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;
        fadeImage.enabled = true; // Ensure the fade image is enabled

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = color;
            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;
    }

    private void SetFadeAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }
}
