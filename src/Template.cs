using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using Il2CppScheduleOne.UI.Phone.Delivery;

namespace DeliverySaver
{
    internal class Template
    {
        public GameObject gameObject { get; private set; }
        public bool isOpen { get => _isOpen; }

        private Button _openButton;
        private AssetBundle _entry;
        private Transform _templates;
        private VerticalLayoutGroup _vls;
        private bool _isOpen;
        private bool _deliveryAppWasEnabled;
        private Animator _animator;
        private bool _rebuildLayout = false;
        private float _timer = 0.0f;

        private List<Entry> _entries = new List<Entry>();
        private Transform _container;

        public Transform templateContent { get => _templates; }

        public Template()
        {
            _container = DeliveryApp.Instance.appContainer.transform;
            gameObject = AssetsManager.Instance.Instantiate("Template");
            gameObject.transform.SetParent(_container, false);
            gameObject.transform.localPosition = new Vector3(393, -32, 0);

            _templates = gameObject.transform.Find("Mask/Content/Entries/Scroll/View/Templates");
            _vls = _templates.GetComponent<VerticalLayoutGroup>();

            Action callback = () => { OnExportTemplate(); };
            gameObject.transform.Find("Mask/Content/ExportSeed").GetComponent<Button>().onClick.AddListener(callback); ;

            //GameObject scrollGo = gameObject.transform.Find("Mask/Content/Scroll").gameObject;

            _animator = gameObject.transform.Find("Mask/Content").GetComponent<Animator>();

            // When the template open button is click, change the value of _isOpen
            // Also rebuild the whole layout
            _openButton = gameObject.transform.Find("Mask/Content/Open").GetComponent<Button>();
            
            Action rebuilder = () => RebuildEveryLayout();
            Action open = () => _isOpen = !_isOpen;
            _openButton.onClick.AddListener(open);
            _openButton.onClick.AddListener(rebuilder);

            _isOpen = false;

            Action action = () =>
            {
                if (_isOpen)
                {
                    _animator.SetTrigger("OpenFast");
                }
            };

            DeliveryApp.Instance.appIconButton.onClick.AddListener(action);
        }

        private void OnExportTemplate()
        {
            EntryData[] entries = TemplateManager.Instance.GetActualTemplateData();
            Seeder.Instance.SeedToClipboard(entries);
            Notification.Instance.Show("Template seed copied to clipboard");
        }

        public void RebuildEveryLayout()
        {
            foreach (var entry in _entries)
            {
                GUIUtils.MarkRebuildLayout(entry.gameObject.GetComponent<VerticalLayoutGroup>());
            }
        }

        public void OnUpdate()
        {
            if(_rebuildLayout && _timer <= 0)
            {
                GUIUtils.RebuildLayout(_vls);
                _rebuildLayout = false;
                _timer = 0;
            }

            if(_timer > 0)
            {
                _timer -= Time.deltaTime;

            }
        }

        public Entry GetLastEntry()
        {
            return _entries.Last();
        }

        public Entry[] GetAllEntries()
        {
            return _entries.ToArray();
        }

        public void AddEntry(string name, DeliveryShop shop)
        {
            _entries.Add(new Entry(name, shop, templateContent.transform));
        }

        public void AddEntryData(EntryData data)
        {
            _entries.Add(new Entry(data, templateContent.transform));
        }

        internal void RemoveEntry(ImmutableEntry entry)
        {
            _entries.Remove((Entry)entry);
        }

        public void RebuildLayout()
        {
            _timer = Time.deltaTime;
            _rebuildLayout = true;
        }

        internal void Open()
        {
            if(!isOpen)
            {
                _openButton.onClick.Invoke();
            }
        }

        internal void Close()
        {
            if(isOpen)
            {
                _openButton.onClick.Invoke();
            }
        }

        public void SetEntry(Entry newEntry)
        {
            int index = _entries.IndexOf(newEntry);

            if (index != -1)
            {
                _entries[index] = newEntry;
                return;
            }

            throw new Exception($"Entry {newEntry.title} has not been found in template");
        }

        public void SetEntryData(EntryData newEntryData)
        {
            int index = _entries.FindIndex(e => e.title == newEntryData.title);

            if (index != -1)
            {
                _entries[index].SetDataFromEntryData(newEntryData);
                return;
            }

            throw new Exception($"EntryData {newEntryData.title} has not been found in template");
        }
    }

}
