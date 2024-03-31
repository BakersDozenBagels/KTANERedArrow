using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public static class Repository
{
    private static Dictionary<string, SimpleModule> _modules;
    public static Loading Loaded = new Loading();

    public static bool Has(string module)
    {
        return _modules.ContainsKey(module) || _modules.ContainsKey(module.Replace("Needy ", ""));
    }
    public static SimpleModule Get(string module)
    {
        return _modules.ContainsKey(module) ? _modules[module]
            : _modules.ContainsKey(module.Replace("Needy ", "")) ? _modules[module.Replace("Needy ", "")]
            : null;
    }

    public static IEnumerator LoadData()
    {
        if (Loaded)
            yield break;

        var download = new DownloadText("https://ktane.timwi.de/json/raw");
        yield return download;

        var repositoryBackup = Path.Combine(Application.persistentDataPath, "RepositoryBackup-RedArrow.json");

        var rawJSON = download.Text;
        string simpleJSON = null;
        SavedJSON sdata;
        if (rawJSON == null)
        {
            Debug.Log("[Red Arrow] Unable to download the repository.");

            if (File.Exists(repositoryBackup))
                simpleJSON = File.ReadAllText(repositoryBackup);
            else
                Debug.Log("[Red Arrow] Could not find a repository backup.");
        }

        if (rawJSON == null && simpleJSON == null)
        {
            Debug.Log("[Red Arrow] Could not get module information.");

            _modules = new Dictionary<string, SimpleModule>();
            yield break;
        }
        else if (rawJSON != null)
        {
            var wdata = JsonConvert.DeserializeObject<WebsiteJSON>(rawJSON).KtaneModules;
            sdata = new SavedJSON() 
            {
                Modules = wdata
                .Select(m => new SimpleModule() { Name = m.Name, Bossy = m.BossStatus != null, Needy = m.Type == "Needy", Quirkiness = m.Quirkiness })
                .Where(m => m.Bossy || m.Needy || m.Quirkiness != 0)
                .ToList()
            };

            // Vanilla modules are weird
            sdata.Modules.First(m => m.Name == "Venting Gas").Name = "Needy Vent Gas";
            sdata.Modules.First(m => m.Name == "Knob").Name = "Needy Knob";
            sdata.Modules.First(m => m.Name == "Capacitor Discharge").Name = "Needy Capacitor";

            // Save a backup of the repository
            File.WriteAllText(repositoryBackup, JsonConvert.SerializeObject(sdata));
        }
        else
        {
            sdata = JsonConvert.DeserializeObject<SavedJSON>(simpleJSON);
        }

        _modules = sdata.Modules.ToDictionary(m => m.Name, m => m);

        Loaded.Finish();
    }

    public class WebsiteJSON
    {
        public List<KtaneModule> KtaneModules;
    }

    public class KtaneModule
    {
        public string Name, Type, BossStatus, Quirks;
        [JsonIgnore]
        public int Quirkiness
        {
            get
            {
                if (Quirks == null)
                    return 0;
                return Quirks.Count(c => c == ',') + 1;
            }
        }
    }

    public class SavedJSON
    {
        public List<SimpleModule> Modules;
    }

    public class SimpleModule
    {
        public string Name;
        public bool Bossy, Needy;
        public int Quirkiness;
    }

    public class Loading : CustomYieldInstruction
    {
        private bool _finished;

        public override bool keepWaiting { get { return !_finished; } }

        public void Finish() { _finished = true; }

        public static bool operator true(Loading l) { return l._finished; }
        public static bool operator false(Loading l) { return !l._finished; }
        public static bool operator !(Loading l) { return !l._finished; }
    }
}