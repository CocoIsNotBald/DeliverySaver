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

        private List<Entry> _entries = new List<Entry>();
        private Transform _container;

        public Transform templateContent { get => _templates; }

        public Template()
        {
            _container = DeliveryApp.Instance.appContainer.transform;
            gameObject = AssetsManager.Instance.Instantiate("Template");
            gameObject.transform.SetParent(_container, false);
            gameObject.transform.localPosition = new Vector3(393, -32, 0);

            _templates = gameObject.transform.Find("Mask/Content/Scroll/View/Templates");
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

        private void RebuildEveryLayout()
        {
            foreach (var entry in _entries)
            {
                GUIUtils.RebuildLayout(entry.gameObject.GetComponent<VerticalLayoutGroup>());
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
            _entries.Add(new Entry(name, shop, this));
        }

        public void AddEntryData(EntryData data)
        {
            _entries.Add(new Entry(data, this));
        }

        internal void RemoveEntry(Entry entry)
        {
            _entries.Remove(entry);
        }

        public void RebuildLayout()
        {
            GUIUtils.RebuildLayout(_vls);
        }

        internal void Open()
        {
            if(!isOpen)
            {
                _openButton.onClick.Invoke();
            }
        }
    }

}
