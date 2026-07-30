using UnityEngine;

public class FloatingItem : MonoBehaviour
{
    public float floatSpeed = 2f;
    public float floatAmount = 0.2f;
    public float rotateSpeed = 45f;
    
    private float startY;

    void Start()
    {
        startY = transform.position.y;
    }

    void Update()
    {
        // Tairne ka effect (bobbing)
        float newY = startY + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        
        // Ghoomne ka effect (rotation)
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }
}
