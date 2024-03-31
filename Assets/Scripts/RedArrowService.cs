using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UObject = UnityEngine.Object;
using Module = Repository.SimpleModule;
using Random = UnityEngine.Random;

public class RedArrowService : MonoBehaviour
{
    [SerializeField]
    private GameObject _prefab;
    [SerializeField]
    private AnimationCurve _timeDistribution;

    private bool _active;
    private readonly List<Coroutine> _handlers = new List<Coroutine>();
    private KMAudio _audio;

    private void OnEnable()
    {
        _active = true;
    }
    private void OnDisable()
    {
        _active = false;
    }
    void Start()
    {
        _prefab.SetActive(false);
        _audio = GetComponent<KMAudio>();
        if (!Repository.Loaded)
            StartCoroutine(Repository.LoadData());
        GetComponent<KMGameInfo>().OnStateChange += StateChange;
    }

    private void StateChange(KMGameInfo.State state)
    {
        if (state != KMGameInfo.State.Gameplay || !_active)
        {
            foreach (var handler in _handlers)
                StopCoroutine(handler);
            _handlers.Clear();
            return;
        }

        StartCoroutine(DetermineModules());
    }

    private static readonly Type s_bombComponentType = ReflectionHelper.FindGameType("BombComponent");
    private static readonly Func<MonoBehaviour, string> s_getModuleName = GenerateGetModuleName();
    private static Func<MonoBehaviour, string> GenerateGetModuleName()
    {
#if UNITY_EDITOR
        return m => "";
#endif
        var param = Expression.Parameter(typeof(MonoBehaviour), "module");
        var method = s_bombComponentType.GetMethod("GetModuleDisplayName", BindingFlags.Public | BindingFlags.Instance);
        var component = Expression.Convert(param, s_bombComponentType);
        var result = Expression.Call(component, method);

        return Expression.Lambda<Func<MonoBehaviour, string>>(result, param).Compile();
    }

    private IEnumerator DetermineModules()
    {
        Debug.Log("[Red Arrow] Looking for important modules...");
        UObject[] components = FindObjectsOfType(s_bombComponentType);
        while (components == null || components.Length == 0 || !components.Any(c => !new string[] { "None", "Timer" }.Contains(s_getModuleName((MonoBehaviour)c))))
        {
            yield return null;
            components = FindObjectsOfType(s_bombComponentType);
        }
        yield return Repository.Loaded;

        var modulesnames = components
            .Cast<MonoBehaviour>()
            .ToDictionary(x => x, m =>
                //m.GetComponent<KMBombModule>() != null 
                //? m.GetComponent<KMBombModule>().ModuleDisplayName
                //: m.GetComponent<KMNeedyModule>() != null
                //? m.GetComponent<KMNeedyModule>().ModuleDisplayName
                //:
                s_getModuleName(m));
        var modules = modulesnames
            .Where(kvp => Repository.Has(kvp.Value))
            .ToDictionary(kvp => kvp.Key, kvp => Repository.Get(kvp.Value));
        Debug.Log("<Red Arrow> Found modules:");
        foreach (var m in modulesnames.Values)
            Debug.Log("<Red Arrow> " + m);

        if (modules.Count == 0)
            Debug.Log("[Red Arrow] Found no important modules.");
        else
            Debug.Log("[Red Arrow] Found important modules:");

        foreach (var module in modules)
        {
            _handlers.Add(StartCoroutine(Highlight(module.Key, Importance(module.Value))));
            Debug.Log("[Red Arrow] " + module.Value.Name);
        }
    }

    private IEnumerator Highlight(MonoBehaviour obj, int importance)
    {
        var arrows = Instantiate(_prefab, obj.transform);
        arrows.SetActive(false);
        arrows.transform.localPosition = Vector3.zero;
        arrows.transform.localRotation = Quaternion.identity;
        arrows.transform.localScale = Vector3.one;

        var comp = arrows.GetComponent<RedArrowPrefab>();
        Coroutine addArrows = null;

        while (true)
        {
            arrows.transform.GetChild(0).rotation = Camera.main.transform.rotation;
            if (arrows.activeSelf && Vector3.Dot(-arrows.transform.up, Camera.main.transform.forward) < 0.3)
            {
                comp.RemoveArrows();
                arrows.SetActive(false);
                StopCoroutine(addArrows);
            }
            else if (!arrows.activeSelf && Vector3.Dot(-arrows.transform.up, Camera.main.transform.forward) > 0.3)
            {
                arrows.SetActive(true);
                addArrows = StartCoroutine(AddArrows(comp, importance));
                _handlers.Add(addArrows);
            }
            yield return null;
        }
    }

    private IEnumerator AddArrows(RedArrowPrefab comp, int importance)
    {
        for (int i = 0; i < importance; i++)
        {
            yield return new WaitForSeconds(_timeDistribution.Evaluate(Random.Range(0f, 1f)));
            comp.AddArrow(_audio);
        }
    }

    private static int Importance(Module module)
    {
        return (module.Bossy ? 2 : 0) + (module.Needy ? 3 : 0) + module.Quirkiness;
    }
}
