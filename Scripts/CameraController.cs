using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    Player player;

    [SerializeField]
    float viewSpeed;

    Vector3 offset;

    [SerializeField]
    float maxCamViewRadius = 10f;

    private void Start()
    {
        player = Player.Instance;

        offset = transform.position - player.transform.position;
    }
    Vector3 pos;
    private void FixedUpdate()
    {
        Vector3 offsetTarget = player.transform.position + offset;
        
        if (!player.CamViewMode)
            pos = Vector3.one * float.Epsilon;

        Vector3 input = new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        float scalar = Mathf.Clamp01(-(Mathf.Clamp(pos.magnitude, .1f, float.MaxValue) / maxCamViewRadius) + 1);

        pos += viewSpeed * scalar * Time.fixedDeltaTime * input;
        Vector3 target = offsetTarget + (player.CamViewMode ? pos : Vector3.zero);

        transform.position = Vector3.Lerp(transform.position, target, 10f * Time.fixedDeltaTime);
    }
}