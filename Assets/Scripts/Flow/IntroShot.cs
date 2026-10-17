using UnityEngine;

[System.Serializable]
public class IntroShot
{
    [SerializeField] private Transform cameraStart;
    [SerializeField] private Transform cameraEnd;
    [SerializeField] private float duration = 10f;
    [SerializeField, TextArea(2, 4)] private string subtitle;

    public Transform CameraStart => cameraStart;
    public Transform CameraEnd => cameraEnd;
    public float Duration => duration;
    public string Subtitle => subtitle;
}