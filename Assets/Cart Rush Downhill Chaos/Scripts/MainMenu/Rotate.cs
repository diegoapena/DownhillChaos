using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float SpeedRotate;
    
    void Update()
    {
        transform.Rotate(new Vector3(0, 1, 0) * SpeedRotate);
    }
}
