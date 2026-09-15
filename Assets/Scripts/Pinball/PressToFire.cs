using UnityEngine;
using UnityEngine.EventSystems;

[AddComponentMenu("Pinball/Press To Fire (누르는 동안 발사)")]
[DisallowMultipleComponent]
public sealed class PressToFire : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] PinballManager manager;
    [Tooltip("누르고 있는 동안 쿨다운마다 계속 발사합니다.")]
    [SerializeField] bool repeatWhileHeld = true;

    bool _held;

    void Awake()
    {
        if (manager == null)
        {
            manager = FindFirstObjectByType<PinballManager>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _held = true;
        Fire();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _held = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _held = false;
    }

    void Update()
    {
        if (_held && repeatWhileHeld)
        {
            Fire();
        }
    }

    void Fire()
    {
        if (manager == null)
        {
            manager = FindFirstObjectByType<PinballManager>();
        }

        if (manager != null)
        {
            manager.Fire();
        }
    }
}
