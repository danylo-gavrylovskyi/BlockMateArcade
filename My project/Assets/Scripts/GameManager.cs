using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    Sprite[] badIconSprites;
    [SerializeField]
    Sprite goodIconSprite;
    [SerializeField]
    Text scoreText;
    [SerializeField]
    Text highScoreText;
    [SerializeField]
    Text gameOverText;
    [SerializeField]
    GameObject gameOverPanel;
    [SerializeField]
    Image[] focusDots;
    [SerializeField]
    GameObject crosshairObj;
    [SerializeField]
    TrailRenderer crosshairTrail;

    int score;
    int highScore;
    int focusCharges;
    bool gameOver;
    float spawnTimer;
    float spawnInterval;
    float fallSpeed;
    float restartCooldown;

    Camera cam;
    Sprite whiteSquare;

    void Start()
    {
        cam = Camera.main;
        whiteSquare = MakeWhiteSquare();
        highScore = PlayerPrefs.GetInt("BlockMateHighScore", 0);
        StartGame();
    }

    void StartGame()
    {
        score = 0;
        focusCharges = 3;
        spawnInterval = 1.5f;
        fallSpeed = 3.5f;
        spawnTimer = 0f;
        gameOver = false;
        restartCooldown = 0f;

        FallingIcon[] allIcons = FindObjectsByType<FallingIcon>(FindObjectsSortMode.None);
        for (int i = 0; i < allIcons.Length; i++)
        {
            Destroy(allIcons[i].gameObject);
        }

        UpdateUI();
        gameOverPanel.SetActive(false);

        if (crosshairTrail != null)
        {
            crosshairTrail.Clear();
        }
    }

    void Update()
    {
        if (gameOver)
        {
            restartCooldown -= Time.deltaTime;
            if (restartCooldown <= 0 && WasClicked())
            {
                StartGame();
            }
            return;
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnIcon();
            spawnTimer = 0f;
            spawnInterval = Mathf.Max(0.5f, spawnInterval - 0.05f);
            fallSpeed += 0.05f;
        }

        HandleSwipe();

        Vector2 screenPos = GetMousePosition();
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        worldPos.z = 0;
        crosshairObj.transform.position = worldPos;

        if (IsHolding())
        {
            crosshairTrail.emitting = true;
        }
        else
        {
            crosshairTrail.emitting = false;
        }
    }

    Vector2 GetMousePosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        return Vector2.zero;
    }

    bool IsHolding()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }

        return false;
    }

    bool WasClicked()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        return false;
    }

    void SpawnIcon()
    {
        float xMin = cam.ViewportToWorldPoint(new Vector3(0.1f, 0, 0)).x;
        float xMax = cam.ViewportToWorldPoint(new Vector3(0.9f, 0, 0)).x;
        float yTop = cam.ViewportToWorldPoint(new Vector3(0, 1.1f, 0)).y;

        float randomX = Random.Range(xMin, xMax);

        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.position = new Vector3(randomX, yTop, 0);
        iconObject.transform.localScale = Vector3.one * 1.3f;

        SpriteRenderer sr = iconObject.AddComponent<SpriteRenderer>();

        FallingIcon fallingIcon = iconObject.AddComponent<FallingIcon>();
        fallingIcon.fallSpeed = fallSpeed;
        fallingIcon.gameManager = this;

        bool isGood = Random.value < 0.10f;
        fallingIcon.isGoodNotification = isGood;

        if (isGood)
        {
            sr.sprite = goodIconSprite;
        }
        else
        {
            sr.sprite = badIconSprites[Random.Range(0, badIconSprites.Length)];
        }

        CircleCollider2D collider = iconObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
    }

    void HandleSwipe()
    {
        if (!IsHolding())
        {
            return;
        }

        Vector2 screenPos = GetMousePosition();
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        worldPos.z = 0;

        Collider2D hit = Physics2D.OverlapCircle(worldPos, 0.5f);
        if (hit == null)
        {
            return;
        }

        FallingIcon icon = hit.GetComponent<FallingIcon>();
        if (icon == null)
        {
            return;
        }

        if (icon.isGoodNotification)
        {
            LoseCharge();
        }
        else
        {
            score += 10;
        }

        SpriteRenderer iconRenderer = icon.GetComponent<SpriteRenderer>();
        SpawnParticles(icon.transform.position, iconRenderer.color);
        Destroy(icon.gameObject);
        UpdateUI();
    }

    public void IconFellOff(FallingIcon icon)
    {
        if (!icon.isGoodNotification)
        {
            LoseCharge();
        }
        Destroy(icon.gameObject);
    }

    void LoseCharge()
    {
        focusCharges--;
        UpdateUI();

        if (focusCharges <= 0)
        {
            DoGameOver();
        }
    }

    void DoGameOver()
    {
        gameOver = true;
        restartCooldown = 0.5f;

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("BlockMateHighScore", highScore);
            PlayerPrefs.Save();
        }

        gameOverPanel.SetActive(true);
        gameOverText.text = "GAME OVER\n\nScore: " + score + "\nBest: " + highScore + "\n\nTap to Restart";
    }

    void UpdateUI()
    {
        scoreText.text = "Score: " + score;
        highScoreText.text = "Best: " + highScore;

        for (int i = 0; i < 3; i++)
        {
            if (i < focusCharges)
            {
                focusDots[i].gameObject.SetActive(true);
            }
            else
            {
                focusDots[i].gameObject.SetActive(false);
            }
        }
    }

    void SpawnParticles(Vector3 position, Color color)
    {
        for (int i = 0; i < 6; i++)
        {
            GameObject particle = new GameObject("Particle");
            particle.transform.position = position;
            particle.transform.localScale = Vector3.one * 0.3f;

            SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSquare;
            sr.color = color;
            sr.sortingOrder = 50;

            Rigidbody2D rb = particle.AddComponent<Rigidbody2D>();
            rb.linearVelocity = new Vector2(Random.Range(-5f, 5f), Random.Range(2f, 8f));
            rb.gravityScale = 3f;

            Destroy(particle, 0.5f);
        }
    }

    Sprite MakeWhiteSquare()
    {
        Texture2D texture = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++)
        {
            pixels[i] = Color.white;
        }
        texture.SetPixels(pixels);
        texture.filterMode = FilterMode.Point;
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }
}
