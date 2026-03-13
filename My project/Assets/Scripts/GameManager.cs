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
    Sprite crosshairSprite;

    int score;
    int highScore;
    int focusCharges;
    bool gameOver;
    float spawnTimer;
    float spawnInterval;
    float fallSpeed;
    float restartCooldown;

    Text scoreText;
    Text highScoreText;
    Text gameOverText;
    Image[] focusDots;
    GameObject gameOverPanel;
    GameObject crosshairObj;
    TrailRenderer crosshairTrail;
    Camera cam;
    Sprite whiteSquare;

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
            cam = FindFirstObjectByType<Camera>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(0, 0, -10);

        whiteSquare = MakeWhiteSquare();
        highScore = PlayerPrefs.GetInt("BlockMateHighScore", 0);

        CreateUI();
        CreateCrosshair();
        CreateBottomLine();
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

    void CreateUI()
    {
        Font font = Font.CreateDynamicFontFromOSFont("Arial", 14);

        GameObject canvasObject = new GameObject("GameCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasObject.AddComponent<GraphicRaycaster>();

        scoreText = CreateTextElement("Score: 0", font, 48, Color.white, canvasObject.transform);
        scoreText.rectTransform.anchorMin = new Vector2(1, 1);
        scoreText.rectTransform.anchorMax = new Vector2(1, 1);
        scoreText.rectTransform.pivot = new Vector2(1, 1);
        scoreText.rectTransform.anchoredPosition = new Vector2(-30, -30);
        scoreText.rectTransform.sizeDelta = new Vector2(400, 70);
        scoreText.alignment = TextAnchor.UpperRight;

        highScoreText = CreateTextElement("Best: 0", font, 36, Color.gray, canvasObject.transform);
        highScoreText.rectTransform.anchorMin = new Vector2(1, 1);
        highScoreText.rectTransform.anchorMax = new Vector2(1, 1);
        highScoreText.rectTransform.pivot = new Vector2(1, 1);
        highScoreText.rectTransform.anchoredPosition = new Vector2(-30, -100);
        highScoreText.rectTransform.sizeDelta = new Vector2(400, 50);
        highScoreText.alignment = TextAnchor.UpperRight;

        Text focusLabel = CreateTextElement("Focus", font, 36, Color.white, canvasObject.transform);
        focusLabel.rectTransform.anchorMin = new Vector2(0, 1);
        focusLabel.rectTransform.anchorMax = new Vector2(0, 1);
        focusLabel.rectTransform.pivot = new Vector2(0, 1);
        focusLabel.rectTransform.anchoredPosition = new Vector2(30, -30);
        focusLabel.rectTransform.sizeDelta = new Vector2(200, 50);
        focusLabel.alignment = TextAnchor.UpperLeft;

        focusDots = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject dotObject = new GameObject("FocusDot" + i);
            dotObject.transform.SetParent(canvasObject.transform, false);

            Image dot = dotObject.AddComponent<Image>();
            dot.color = Color.blue;

            dot.rectTransform.anchorMin = new Vector2(0, 1);
            dot.rectTransform.anchorMax = new Vector2(0, 1);
            dot.rectTransform.pivot = new Vector2(0, 1);
            dot.rectTransform.anchoredPosition = new Vector2(30 + i * 55, -85);
            dot.rectTransform.sizeDelta = new Vector2(45, 45);

            focusDots[i] = dot;
        }

        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(canvasObject.transform, false);

        Image panelBackground = gameOverPanel.AddComponent<Image>();
        panelBackground.color = new Color(0, 0, 0, 0.8f);
        panelBackground.rectTransform.anchorMin = Vector2.zero;
        panelBackground.rectTransform.anchorMax = Vector2.one;
        panelBackground.rectTransform.sizeDelta = Vector2.zero;

        gameOverText = CreateTextElement("", font, 56, Color.white, gameOverPanel.transform);
        gameOverText.rectTransform.anchorMin = Vector2.zero;
        gameOverText.rectTransform.anchorMax = Vector2.one;
        gameOverText.rectTransform.sizeDelta = new Vector2(-60, -60);
        gameOverText.alignment = TextAnchor.MiddleCenter;

        gameOverPanel.SetActive(false);
    }

    Text CreateTextElement(string text, Font font, int fontSize, Color color, Transform parent)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);

        Text textComponent = textObject.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = font;
        textComponent.fontSize = fontSize;
        textComponent.color = color;

        return textComponent;
    }

    void CreateCrosshair()
    {
        crosshairObj = new GameObject("Crosshair");

        SpriteRenderer sr = crosshairObj.AddComponent<SpriteRenderer>();
        sr.sprite = crosshairSprite;
        sr.sortingOrder = 100;
        crosshairObj.transform.localScale = Vector3.one * 2f;

        crosshairTrail = crosshairObj.AddComponent<TrailRenderer>();
        crosshairTrail.time = 0.2f;
        crosshairTrail.startWidth = 0.25f;
        crosshairTrail.endWidth = 0.05f;
        crosshairTrail.material = new Material(Shader.Find("Sprites/Default"));
        crosshairTrail.startColor = Color.white;
        crosshairTrail.endColor = new Color(1, 1, 1, 0);
        crosshairTrail.sortingOrder = 99;
        crosshairTrail.emitting = false;
    }

    void CreateBottomLine()
    {
        GameObject line = new GameObject("BottomLine");

        SpriteRenderer sr = line.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSquare;
        sr.color = new Color(1, 0, 0, 0.5f);

        float bottomY = cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).y;
        float screenWidth = cam.ViewportToWorldPoint(new Vector3(1, 0, 0)).x - cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;

        line.transform.position = new Vector3(0, bottomY, 0);
        line.transform.localScale = new Vector3(screenWidth, 0.1f, 1);
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
