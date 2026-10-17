using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum OptionState { Neutral, Selected, Dimmed }

public class CharacterOption : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PlayerCharacter character;
    [SerializeField] private float selectedScale = 1.15f;
    [SerializeField] private float spinSpeed = 25f;
    [SerializeField] private float transitionSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float dimAmount = 0.6f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private struct TintedMaterial
    {
        public Material Material;
        public int ColorProperty;
        public Color Original;
    }

    private readonly List<TintedMaterial> tinted = new List<TintedMaterial>();
    private Vector3 baseScale;
    private Quaternion baseRotation;
    private OptionState state = OptionState.Neutral;
    private float dim;
    private float appliedDim;

    public PlayerCharacter Character => character;
    public event System.Action<CharacterOption> Clicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke(this);
    }
    private void Awake()
    {
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.materials)
            {
                int id = FindColorProperty(m);
                if (id == -1) continue;

                tinted.Add(new TintedMaterial
                {
                    Material = m,
                    ColorProperty = id,
                    Original = m.GetColor(id)
                });
            }
        }
    }

    private void OnDestroy()
    {
        foreach (TintedMaterial t in tinted)
        {
            if (t.Material != null) Destroy(t.Material);
        }
    }

    public void SetState(OptionState newState)
    {
        state = newState;
    }

    private static int FindColorProperty(Material m)
    {
        if (m == null) return -1;
        if (m.HasProperty(BaseColorId)) return BaseColorId;
        if (m.HasProperty(ColorId)) return ColorId;
        return -1;
    }

    private void Update()
    {
        float t = Time.deltaTime * transitionSpeed;

        Vector3 targetScale = state == OptionState.Selected ? baseScale * selectedScale : baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);

        if (state == OptionState.Selected)
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
        else
            transform.localRotation = Quaternion.Slerp(transform.localRotation, baseRotation, t);

        dim = Mathf.MoveTowards(dim, state == OptionState.Dimmed ? 1f : 0f, t);
        ApplyDim();
    }

    private void ApplyDim()
    {
        if (Mathf.Approximately(dim, appliedDim)) return;
        appliedDim = dim;

        foreach (TintedMaterial t in tinted)
        {
            Color dimmed = Color.Lerp(t.Original, Color.gray, dim * dimAmount);
            t.Material.SetColor(t.ColorProperty, dimmed);
        }
    }
}