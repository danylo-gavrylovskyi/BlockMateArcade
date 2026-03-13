using UnityEngine;

public class FallingIcon : MonoBehaviour
{
    public float fallSpeed = 3f;
    public bool isGoodNotification = false;
    public GameManager gameManager;

    void Update()
    {
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        float bottomY = Camera.main.ViewportToWorldPoint(new Vector3(0, -0.05f, 0)).y;
        if (transform.position.y < bottomY)
        {
            gameManager.IconFellOff(this);
        }
    }
}
