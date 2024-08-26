using System.Collections.Generic;
using UnityEngine;

public class RedArrowPrefab : MonoBehaviour
{
    [SerializeField]
    private GameObject _arrow;
    [SerializeField]
    private AudioClip[] _sounds;
    [SerializeField]
    private Transform _parent;

    private readonly List<GameObject> _arrows = new List<GameObject>();

    private void Start()
    {
        _arrow.SetActive(false);
    }

    public void AddArrow(KMAudio audio)
    {
        var arrow = Instantiate(_arrow, _parent);
        arrow.transform.localPosition = Vector3.zero;
        arrow.transform.localScale = Vector3.one;
        arrow.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        arrow.SetActive(true);
        _arrows.Add(arrow);

        audio.PlaySoundAtTransform(_sounds[Random.Range(0, _sounds.Length)].name, arrow.transform);
    }

    public void RemoveArrows()
    {
        foreach (var arrow in _arrows)
            Destroy(arrow);
        _arrows.Clear();
    }
}
