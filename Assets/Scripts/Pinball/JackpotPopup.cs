using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("Pinball/Jackpot Popup (잭팟 팝업)")]
[DisallowMultipleComponent]
public sealed class JackpotPopup : MonoBehaviour
{
    [Header("연출")]
    [Tooltip("비우면 자식에서 Animator를 찾습니다.")]
    [SerializeField] Animator animator;
    [SerializeField] string animationState = "UIPopup";
    [Tooltip("애니메이션이 끝난 뒤 클릭을 받기까지 추가로 기다릴 시간입니다.")]
    [SerializeField] float extraDelay;
    [Tooltip("애니메이션이 끝난 뒤 클릭하면 팝업을 끕니다.")]
    [SerializeField] bool closeOnClick = true;

    bool _canClose;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        EnsureClickCatcher();
    }

    void OnEnable()
    {
        _canClose = false;
        StopAllCoroutines();
        StartCoroutine(WaitForAnimation());
    }

    void OnDisable()
    {
        _canClose = false;
        StopAllCoroutines();
    }

    IEnumerator WaitForAnimation()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(animationState))
            {
                animator.Play(animationState, 0, 0f);
            }

            yield return null;

            while (animator != null && animator.gameObject.activeInHierarchy)
            {
                var info = animator.GetCurrentAnimatorStateInfo(0);
                if (!animator.IsInTransition(0) && info.normalizedTime >= 1f)
                {
                    break;
                }

                yield return null;
            }
        }

        if (extraDelay > 0f)
        {
            yield return new WaitForSeconds(extraDelay);
        }

        _canClose = closeOnClick;
    }

    void HandleClick()
    {
        if (_canClose)
        {
            gameObject.SetActive(false);
        }
    }

    void EnsureClickCatcher()
    {
        var catcher = new GameObject("ClickCatcher", typeof(RectTransform), typeof(Image));
        catcher.transform.SetParent(transform, false);
        catcher.transform.SetAsLastSibling();

        var rect = catcher.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = catcher.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        var trigger = catcher.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener(_ => HandleClick());
        trigger.triggers.Add(entry);
    }
}
