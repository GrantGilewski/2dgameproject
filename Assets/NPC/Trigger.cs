using UnityEngine;

public class TriggerForwarder2D : MonoBehaviour
{
    NPCInteract target;
    void Awake() { target = GetComponentInParent<NPCInteract>(); }

    void OnTriggerEnter2D(Collider2D other)  { if (target) target.SendMessage("OnTriggerEnter2D", other, SendMessageOptions.DontRequireReceiver); }
    void OnTriggerExit2D(Collider2D other)   { if (target) target.SendMessage("OnTriggerExit2D", other, SendMessageOptions.DontRequireReceiver); }
}