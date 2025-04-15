using Il2CppFluffyUnderware.DevTools.Extensions;
using Il2CppNewtonsoft.Json;
using Il2CppNewtonsoft.Json.Utilities;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.Persistence.Loaders;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppSystem.Net;
using MelonLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Services.Core.Internal.Serialization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Il2CppScheduleOne.Tools;
using Il2CppScheduleOne;
using static Il2CppMono.Security.X509.X520;
using Harmony;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Persistence;

namespace DeliverySaver
{
    public class EntryAlreadyExistsException : Exception
    {

    }

    public class EntryIsEmpty : Exception
    {

    }

    class GameInfo
    {
        private static GameInfo _instance;

        public string organisationName;
        public int seed;

        public static GameInfo Instance { 
            get 
            {
                if (_instance == null)
                {
                    _instance = new GameInfo();
                }

                return _instance;
            } 
        }
        public string GameName { get => $"{GameManager.Instance.OrganisationName}_{GameManager.Instance.seed}"; }

        public GameInfo()
        {
            organisationName = GameManager.Instance.OrganisationName;
            seed = GameManager.Instance.seed;
        }
    }

    internal class TemplateManager
    {
        private Dictionary<string, List<EntryData>> _toSaves = new Dictionary<string, List<EntryData>>();
        private Template _template;
        private static TemplateManager _instance;
        public static TemplateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TemplateManager();
                }
                return _instance;
            }
        }

        public Template template { get => _template; }

        public void Init()
        {
            AssetsManager.Instance.LoadAssetBundleFromResources("Template", "ui.template");
            AssetsManager.Instance.LoadAssetBundleFromResources("Entry", "ui.entry");
            AssetsManager.Instance.LoadAssetBundleFromResources("Component", "ui.component");
        }

        public void CreateTemplateGameObject()
        {
            _template = new Template();
        }

        public void AddEntryData(EntryData entry, bool showSameEntryMessage = true)
        {
            var entryData = FindDataFromTitle(entry.title);

            if (entryData != null)
            {
                if (showSameEntryMessage)
                {
                    throw new EntryAlreadyExistsException();
                }

                return;
            }

            AddEntryDataBase(entry);
            _toSaves[GameInfo.Instance.GameName].Add(entry);
        }

        private void AddEntryDataBase(EntryData entry)
        {
            _template.AddEntryData(entry);
            _template.RebuildLayout();
        }

        public void AddEntry(string name, DeliveryShop shop)
        {
            _template.AddEntry(name, shop);
            _toSaves[GameInfo.Instance.GameName].Add(_template.GetLastEntry().ToEntryData());
            _template.RebuildLayout();
        }

        public void UpdateEntryTitle(string oldTitle, Entry entry)
        {
            for (int i = 0; i < _toSaves[GameInfo.Instance.GameName].Count; i++)
            {
                if (_toSaves[GameInfo.Instance.GameName][i].title == oldTitle)
                {
                    _toSaves[GameInfo.Instance.GameName][i] = entry.ToEntryData();
                    break;
                }
            }
        }

        public void UpdateEntry(Entry entry)
        {
            for (int i = 0; i < _toSaves[GameInfo.Instance.GameName].Count; i++)
            {
                if (_toSaves[GameInfo.Instance.GameName][i].title == entry.title)
                {
                    _toSaves[GameInfo.Instance.GameName][i] = entry.ToEntryData();
                    break;
                }
            }
        }
        public EntryData[] GetActualTemplateData()
        {
            string text = Newtonsoft.Json.JsonConvert.SerializeObject(_toSaves[GameInfo.Instance.GameName].ToArray());
            return _toSaves[GameInfo.Instance.GameName].ToArray();
        }

        public void Save()
        {
            foreach(var pair in _toSaves)
            {
                string path = Path.Combine(ModConfig.ModRootFile, $"template_{pair.Key}_v2.json");
                string entries = Newtonsoft.Json.JsonConvert.SerializeObject(pair.Value);
                File.WriteAllText(path, entries);
            }
        }

        public void Load()
        {
            string gameName = GameInfo.Instance.GameName;

            if (_toSaves.ContainsKey(gameName))
            {
                foreach (EntryData data in _toSaves[gameName])
                {
                    AddEntryDataBase(data);
                }
                return;
            }

            if (!_toSaves.ContainsKey(gameName))
            {
                _toSaves.Add(gameName, new List<EntryData>());
            }

            string path = Path.Combine(ModConfig.ModRootFile, $"template_{GameInfo.Instance.GameName}_v2.json");

            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);
                List<EntryData> entries = Newtonsoft.Json.JsonConvert.DeserializeObject<List<EntryData>>(content);

                foreach (EntryData entry in entries)
                {
                    AddEntryData(entry);
                }
            }
        }

        internal void RemoveEntry(Entry entry)
        {
            _template.RemoveEntry(entry);
            _toSaves[GameInfo.Instance.GameName].Remove(FindDataFromEntry(entry));
        }

        private EntryData FindDataFromEntry(Entry entry)
        {
            return _toSaves[GameInfo.Instance.GameName].Find(e => e.title == entry.title);
        }

        private EntryData FindDataFromTitle(string title)
        {
            return _toSaves[GameInfo.Instance.GameName].Find(e => e.title == title);
        }

        internal bool IsEntryRegister(string title)
        {
            return _toSaves[GameInfo.Instance.GameName].Find(e => e.title == title) != null;
        }
    }
}
