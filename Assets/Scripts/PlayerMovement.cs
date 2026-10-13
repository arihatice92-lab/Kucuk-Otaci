using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private Transform cameraTransform;

    [Header("Fizik Ayarları")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float rotationSpeed = 15f;

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Inspector'dan Camera Transform atanmadıysa ana kamerayı otomatik bul
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        // UI üzerinde yazı yazılıyorsa (diyalog/konsol) hareketi tamamen durdur
        if (UIInputState.IsTyping) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 1. Girdi Kontrolleri (WASD & Ok Tuşları)
        float h = 0f;
        float v = 0f;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;

        Vector2 input = new Vector2(h, v).normalized;
        Vector3 moveDir = Vector3.zero;

        // 2. Kamera Açısına Göre Yön Hesaplama
        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            moveDir = (forward * input.y + right * input.x).normalized;

            // Karakteri yürüdüğü yöne yumuşakça döndür
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 3. Kararlı Yerçekimi Mantığı (Zemin Titremesini Önler)
        if (controller.isGrounded)
        {
            // Yerdeyken sürekli sıfırlamak yerine hafif bir bastırma kuvveti uygula
            verticalVelocity = -1f;
        }
        else
        {
            // Havada süzülüyorsa yerçekimi hızlanarak uygula
            verticalVelocity += gravity * Time.deltaTime;
        }

        // 4. Nihai Hareket Vektörünü Uygula
        Vector3 finalVelocity = (moveDir * speed);
        finalVelocity.y = verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);
    }
}