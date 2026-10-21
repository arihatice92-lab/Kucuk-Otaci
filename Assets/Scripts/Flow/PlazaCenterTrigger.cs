using System;
using UnityEngine;

/// <summary>
/// Sorumluluk: Oyuncu meydanın ortasındaki alana girince bir kez haber verir.
/// Girince ne olacağını bilmez, sadece Reached olayını tetikler.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlazaCenterTrigger : MonoBehaviour
{
    public event Action Reached;

    private bool fired;

    // Bileşen eklenince collider otomatik "Is Trigger" olur.
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fired) return;
        if (other.GetComponentInParent<PlayerMovement>() == null) return; // Fındık ya da başka nesne sayılmaz

        fired = true;
        Reached?.Invoke();
    }
}