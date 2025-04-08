using Il2CppFluffyUnderware.DevTools.Extensions;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.Map;
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
    internal class Entry
    {
        public ListingEntry entry;
        public Text content;

        public Entry(ListingEntry entry, Text content)
        {
            this.entry = entry;
            this.content = content;
        }

        public string name { get => content.text.Split(" ")[1]; }
        public string quantity { get => content.text.Split(" ")[0].Replace("x", "").Trim(); }
    }
    internal class TemplateInstance
    {
        // Asset bundle load
        public GameObject gameObject { get; private set; }
        private Asset _entry;
        private Asset _component;

        private Transform _templates;
        private VerticalLayoutGroup _vlg;
        private ScrollRect _scroll;
        private List<Entry> _entries = new List<Entry>();
        public TemplateInstance(GameObject gameObject)
        {
            this.gameObject = gameObject;

            _templates = gameObject.transform.Find("Mask/Content/Scroll/View/Templates");
            if(_templates == null)
            {
                Melon<Core>.Logger.Warning("Cannot find Templates in TemplateInstance");
                return;
            }
            _vlg = _templates.GetComponent<VerticalLayoutGroup>();

            GameObject scrollGo = gameObject.transform.Find("Mask/Content/Scroll").gameObject;
            _scroll = scrollGo.GetComponent<ScrollRect>();

            _entry = AssetsManager.Instance.GetAsset("Entry");
            _component = AssetsManager.Instance.GetAsset("Component");
        }

        private void DoSomeStuff(DeliveryShop shop)
        {
            foreach(Entry entry in _entries)
            {
                entry.entry.name = entry.name;
                entry.entry.QuantityInput.text = entry.quantity.ToString();
                entry.entry.QuantityInput.SendOnSubmit();
            }
        }

        public Transform AddEntry(string title, DeliveryShop shop)
        {
            GameObject entryGo = _entry.Instantiate();
            entryGo.transform.SetParent(_templates.transform, false);

            Action callback = () => DoSomeStuff(shop);
            entryGo.GetComponent<Button>().onClick.AddListener(callback);

            Transform titleGo = entryGo.transform.Find("Head/Title");
            titleGo.GetComponent<Text>().text = title;

            Transform shopGo = entryGo.transform.Find("ShopName");
            shopGo.GetComponent<Text>().text = shop.name;

            GameObject closeButton = entryGo.transform.Find("Head/CloseButton").gameObject;

            Action close = () => { GameObject.Destroy(entryGo); };
            closeButton.GetComponent<Button>().onClick.AddListener(close);

            return entryGo.transform.Find("Content");
        }

        public void AddComponent(ListingEntry entry, string test, Transform parent)
        {
            GameObject componentGo = _component.Instantiate();
            componentGo.transform.SetParent(_templates.transform, false);

            Transform titleGo = componentGo.transform.Find("Content");
            titleGo.GetComponent<Text>().text = test;

            Transform imageGo = componentGo.transform.Find("Image");
            imageGo.GetComponent<RawImage>().texture = entry.Icon.sprite.texture;

            componentGo.transform.SetParent(parent, false);
            UpdateContent();

            _entries.Add(new Entry(entry, titleGo.GetComponent<Text>()));
        }

        void UpdateContent()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_vlg.GetComponent<RectTransform>());
        }
    }

    internal class Template
    {
        public Template()
        {
            AssetsManager.Instance.LoadAsset("Template", "ui", "template");
            AssetsManager.Instance.LoadAsset("Entry", "ui", "entry");
            AssetsManager.Instance.LoadAsset("Component", "ui", "component");
        }

        public TemplateInstance Instantiate()
        {
            var template = AssetsManager.Instance.GetAsset("Template");

            TemplateInstance templateInstance = new TemplateInstance(
                AssetsManager.Instance.Instantiate("Template")
            );
            return templateInstance;
        }
    }
}
