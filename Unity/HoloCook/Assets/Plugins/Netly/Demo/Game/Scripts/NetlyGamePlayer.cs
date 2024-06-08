using Byter;
using Netly;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NetlyGamePlayer : MonoBehaviour
{
    public Camera myCamera;
    public float speed = 4;
    public float sensivity = 100;
    public float syncDelay = 0.1f;
    private CharacterController controller;
    [Header("Debug")]
    [SerializeField] private Vector2 mouseInput;
    [SerializeField] private Vector2 mouseValue;
    [SerializeField] private Vector3 moveInput;
    [SerializeField] private Vector3 moveValue;
    [SerializeField] private Vector3 gravityValue;

    internal string UUID { get; set; } = string.Empty;
    internal string Id { get; set; } = string.Empty;
    internal string Name { get; set; } = string.Empty;
    internal bool IsMain { get; set; } = true;
    internal Vector3 Position { get; set; } = Vector3.zero;
    internal Quaternion Rotation { get; set; } = Quaternion.identity;

    internal UdpClient client { get; set; } = null;

    private bool initMe = false;
    private Vector3 syncPosition;
    private Quaternion syncRotation;
    private float jumpTimer, syncTimer;
    private Vector3 _position;
    private const float _speed = 5;

    private void Awake()
    {
        if (IsMain)
        {
            controller = GetComponent<CharacterController>();
        }
        else
        {
            initMe = true;
            myCamera.gameObject.SetActive(false);
        }

    }

    private void Update()
    {
        if (IsMain)
        {
            // update mouse
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                mouseInput *= sensivity;
                mouseInput *= Time.deltaTime;
                mouseValue.x += mouseInput.x;
                mouseValue.y -= mouseInput.y;
                mouseValue.y = Mathf.Clamp(mouseValue.y, -70, 70);
                transform.rotation = Quaternion.Euler(0, mouseValue.x, 0);
            }

            // update move
            moveInput = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Jump"), Input.GetAxis("Vertical"));
            moveValue = transform.forward * moveInput.z + transform.right * moveInput.x;
            moveValue.Normalize();
            moveValue *= speed;

            // gravity
            gravityValue.y += Physics.gravity.y * Time.deltaTime;
            if (gravityValue.y < 0 && controller.isGrounded) gravityValue.y = 0;

            // jump
            jumpTimer += Time.deltaTime;
            if (moveInput.y > 0 && controller.isGrounded && jumpTimer > 0.5f)
            {
                jumpTimer = 0f;
                const float len = 1f;
                gravityValue.y += Mathf.Sqrt(len * (Physics.gravity.y * -3f));
            }

            // merge positions
            controller.Move((moveValue + gravityValue) * Time.deltaTime);
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Rotation, _speed * Time.deltaTime);
            transform.position = Vector3.SmoothDamp(transform.position, Position, ref _position, _speed * Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        if (IsMain)
        {
            myCamera.transform.localRotation = Quaternion.Euler(mouseValue.y, 0, 0);

            syncTimer += Time.deltaTime;

            if (syncTimer > syncDelay)
            {
                syncTimer = 0f;

                if (syncPosition != transform.position || syncRotation != transform.rotation)
                {
                    syncPosition = transform.position;
                    syncRotation = transform.rotation;

                    // send data
                    if (client != null && client.IsOpened)
                    {
                        using Writer w = new Writer();

                        w.Write(Id);

                        w.Write(syncPosition.x);
                        w.Write(syncPosition.y);
                        w.Write(syncPosition.z);

                        w.Write(syncRotation.x);
                        w.Write(syncRotation.y);
                        w.Write(syncRotation.z);
                        w.Write(syncRotation.w);

                        client.ToEvent("sync_transform", w.GetBytes());
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1) || Input.GetKeyDown(KeyCode.Mouse2) || Input.GetKeyDown(KeyCode.Mouse3))
            {
                Cursor.visible = !Cursor.visible;
                Cursor.lockState = (Cursor.visible) ? CursorLockMode.None : CursorLockMode.Locked;
            }
        }
        else if (initMe is false)
        {
            Awake();
        }
    }

    internal void Destroy()
    {
        Destroy(base.gameObject);
    }
}
