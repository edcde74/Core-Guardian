using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Pointer hover relay for HUD hints (augment icons).
public sealed class CoreGuardianHoverHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Action<bool> Changed;
    bool inside;

    public void OnPointerEnter(PointerEventData e)
    {
        inside = true;
        Changed?.Invoke(true);
    }

    public void OnPointerExit(PointerEventData e)
    {
        inside = false;
        Changed?.Invoke(false);
    }

    void OnDisable()
    {
        if (inside)
        {
            inside = false;
            Changed?.Invoke(false);
        }
    }
}
