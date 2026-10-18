using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Springy button: pops in, grows on hover, squashes on press, clicks.
public class UIButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.08f;
    public Vector2 pressScale = new Vector2(1.12f, 0.86f);
    public float stiffness = 380f;
    public float damping = 18f;
    public float appearDelay;   // seconds before it springs in
    public bool wiggle;         // idle wobble for the main call-to-action

    bool hover, down;
    Vector3 vel;
    float born;
    public Button button;

    // OnEnable, not Start, so the button springs in again every time its screen is shown.
    void OnEnable()
    {
        hover = down = false;
        vel = Vector3.zero;
        born = Time.unscaledTime;
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        Vector3 target = Vector3.one * (hover ? hoverScale : 1f);
        if (down) target = new Vector3(pressScale.x, pressScale.y, 1f);
        if (Time.unscaledTime - born < appearDelay) target = Vector3.zero;

        // spring instead of lerp so it overshoots and wobbles
        vel += (target - transform.localScale) * stiffness * dt;
        vel *= Mathf.Exp(-damping * dt);
        transform.localScale += vel * dt;

        float rot = wiggle ? Mathf.Sin(Time.unscaledTime * 4f) * 3f : 0f;
        if (hover && !down) rot += Mathf.Sin(Time.unscaledTime * 18f) * 1.5f;
        transform.localRotation = Quaternion.Euler(0, 0, rot);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        hover = true;
        vel += Vector3.one * 2f;
    }

    public void OnPointerExit(PointerEventData e) { hover = false; down = false; }

    public void OnPointerDown(PointerEventData e)
    {
        if (button != null && !button.interactable)
        {
            if (Sfx.I != null) Sfx.Play("bonk", 0.6f);
            vel += new Vector3(-3f, 3f, 0);
            return;
        }
        down = true;
        if (Sfx.I != null) Sfx.Play("click");
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (down) vel += new Vector3(-2f, 4f, 0); // pop back up
        down = false;
        // deselect so Space (drop trash) never re-presses this button
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
}
