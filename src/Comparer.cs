using Il2CppFluffyUnderware.DevTools.Extensions;
using Il2CppScheduleOne.UI.Phone.Delivery;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DeliverySaver
{
    enum EntryAction
    {
        Overwrite,
        None
    }

    internal class EntryRegister
    {
        public EntryRegister(GameObject gameObject, ImmutableEntry newEntry, ImmutableEntry originalEntry) 
        { 
            this.gameObject = gameObject;
            this.newEntry = newEntry;
            this.originalEntry = originalEntry;
        }

        public GameObject gameObject;
        public ImmutableEntry newEntry;
        public ImmutableEntry originalEntry;
        public EntryAction action = EntryAction.None;
    }

    internal class Comparator
    {
        private static Comparator _instance;
        private List<EntryRegister> _entries = new List<EntryRegister>();
        private Transform _templates;

        public static Comparator Instance
        {
            get
            {
                if(_instance == null)
                    _instance = new Comparator();
                return _instance;
            }
        }

        public GameObject gameObject;

        public Comparator()
        {
        }

        public void Init()
        {
            AssetsManager.Instance.LoadAssetBundleFromResources("Comparator", "ui.comparator");
            AssetsManager.Instance.LoadAssetBundleFromResources("EntriesDiff", "ui.entriesdiff");
        }

        public void Instantiate()
        {
            gameObject = AssetsManager.Instance.Instantiate("Comparator");
            gameObject.transform.SetParent(DeliveryApp.Instance.appContainer, false);

            Button button = gameObject.transform.Find("Original/Close").GetComponent<Button>();
            button.onClick.AddListener((Action)OnClose);

            Button confirm = gameObject.transform.Find("Original/Buttons/Confirm").GetComponent<Button>();
            confirm.onClick.AddListener((Action)OnConfirm);

            Button overwriteAll = gameObject.transform.Find("Original/Buttons/OverwriteAll").GetComponent<Button>();
            overwriteAll.onClick.AddListener((Action)OnOverwriteAll);

            gameObject.SetActive(false);
            _templates = gameObject.transform.Find("Original/Scroll/View/Templates");
        }

        private void OnConfirm()
        {
            foreach (var entry in _entries)
            {
                if(entry.action == EntryAction.Overwrite)
                {
                    TemplateManager.Instance.SetEntry(entry.newEntry);
                }
            }

            Clear();
        }

        private void OnOverwriteAll()
        {
            foreach (var entry in _entries)
            {
                TemplateManager.Instance.SetEntry(entry.newEntry);
            }

            Clear();
        }

        private void Clear()
        {
            foreach (EntryRegister entry in _entries)
            {
                entry.newEntry.Destroy();
                entry.originalEntry.Destroy();
                entry.gameObject.Destroy();
            }

            _entries.Clear();
            gameObject.SetActive(false);
        }

        private void OnClose()
        {
            Clear();
        }

        private void UpdateAction(bool value, int index)
        {
            if (value)
            {
                _entries[index].action = EntryAction.Overwrite;
            }
            else
            {
                _entries[index].action = EntryAction.None;
            }
        }

        public void Compare(List<EntryData> newData, List<EntryData> originalData)
        {
            // Ensure that the original data is in the same order as the new data
            // This is important for displaying the entries difference
            List<EntryData> b = originalData.OrderBy(d => newData.IndexOf(d)).ToList();

            for (int i = 0; i < newData.Count; i++)
            {
                Compare(newData[i], b[i]);
            }
        }

        public void Compare(EntryData newData, EntryData originalData)
        {
            GameObject goEntries = AssetsManager.Instance.Instantiate("EntriesDiff");
            Toggle toggle = goEntries.transform.Find("arrow").GetComponent<Toggle>();

            // New entry instance
            // Set the new entry to be the first child of the ebtries difference
            // Since entries difference has a vertical layout group
            // The end result displayed will be "[New] -> Arrow -> [Original]"
            ImmutableEntry newEntry = new ImmutableEntry(newData, goEntries.transform);
            newEntry.gameObject.transform.SetSiblingIndex(0);

            // Original instance
            ImmutableEntry originalEntry = new ImmutableEntry(originalData, goEntries.transform);

            // Add the entries difference gameobject to the template comparator
            goEntries.transform.SetParent(_templates, false);

            // Register both entries into a register to be able to execute action later
            // And keep track of the gameobject and memory use
            _entries.Add(new EntryRegister(goEntries, newEntry, originalEntry));
            gameObject.SetActive(true);

            // Add action to the toggle to update the action of the entry difference
            int index = _entries.Count - 1;
            Action<bool> action = (bool toggle) => { UpdateAction(toggle, index); };
            toggle.onValueChanged.AddListener(action);
        }
    }
}
