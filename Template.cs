using Il2CppFluffyUnderware.DevTools.Extensions;
using Il2CppNewtonsoft.Json;
using Il2CppNewtonsoft.Json.Utilities;
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

namespace DeliverySaver
{
    internal class ComponentData
    {
        public string name;
        public string quantity;

        [Newtonsoft.Json.JsonConstructor]
        public ComponentData(string name, string quantity)
        {
            this.name = name;
            this.quantity = quantity;
        }

        public ComponentData(Component component)
        {
            name = component.name;
            quantity = component.quantity.ToString();
        }
    }
    internal class EntryData
    {
        public string name;
        public string shopName;
        public float multiplier;

        public List<ComponentData> components;

        [Newtonsoft.Json.JsonConstructor]
        public EntryData(string name, string shopName, float multiplier, List<ComponentData> components)
        {
            this.name = name;
            this.shopName = shopName;
            this.components = components;
            this.multiplier = multiplier;
        }

        public EntryData(Entry entry)
        {
            name = entry.name;
            shopName = entry.shopName;
            multiplier = entry.multiply;
            components = entry.components.Select(c => new ComponentData(c)).ToList(); // Fix: Convert each Component to ComponentJson
        }
    }

    internal class Entry
    {
        public List<Component> components = new List<Component>();
        public string name { get; }
        public GameObject root;
        public float multiply = 1;
        public string shopName { get => root.transform.Find("Head/ShopName").GetComponent<Text>().text; }

        private Asset _component;
        private InputField _multiplierIF;
        private GameObject _tooLarge;
        private GameObject _insufficientBalance;
        private Text _price;
        private Action _rebuilder;

        public Entry(string title, DeliveryShop shop, GameObject root, Action rebuilder)
        {
            this.name = title;
            this.root = root;

            Action callback = () => ApplyTemplate(shop);
            root.GetComponent<Button>().onClick.AddListener(callback);

            Transform titleGo = root.transform.Find("Head/Header/Title");
            titleGo.GetComponent<Text>().text = title;

            Transform shopGo = root.transform.Find("Head/ShopName");
            shopGo.GetComponent<Text>().text = shop.name;

            Transform headerImage = root.transform.Find("Head/Background");
            headerImage.GetComponent<Image>().color = shop.HeaderImage.color;

            Transform closeButton = root.transform.Find("Head/Header/CloseButton");

            Action close = () => { Close(); };
            closeButton.GetComponent<Button>().onClick.AddListener(close);

            Transform multiplierInput = root.transform.Find("Footer/Multiplier/MultiplierInput");
            InputField multiplierIF = multiplierInput.GetComponent<InputField>();
            multiplierIF.text = multiply.ToString();

            Action<string> multiplierAction = (string value) => HandleMulitplicationInput(value);
            multiplierIF.onSubmit.AddListener(multiplierAction);
            _multiplierIF = multiplierIF;

            _tooLarge = root.transform.Find("Footer/TooLarge").gameObject;
            _insufficientBalance = root.transform.Find("Footer2/InsufficientBalance").gameObject;
            _price = root.transform.Find("Footer2/TotalCost").GetComponent<Text>();
            _rebuilder = rebuilder;

            _component = AssetsManager.Instance.GetAsset("Component");
            _rebuilder();
        }

        private void Close()
        {
            GameObject.Destroy(root);
            TemplateManager.Instance.RemoveEntry(this);
        }

        private float totalCost { get => components.Sum(c => c.price * c.MultipliedQuantity()) + 200; }

        private void HandleMulitplicationInput(string value)
        {
            multiply = float.Parse(value.Replace(".", ","));

            float contentCount = 0;

            foreach (Component component in components)
            {
                component.UpdateContent();
                contentCount += component.MultipliedQuantity();
            }
      
            if(contentCount / 20 > 16)
            {
                _tooLarge.SetActive(true);
            }

            else
            {
                _tooLarge.SetActive(false);
            }

            _price.text = totalCostLabel;
            TemplateManager.Instance.UpdateEntry(this);

            CheckInsufficientBalance();
        }

        public void AddComponent(ListingEntry entry, Entry parent)
        {
            string name = entry.ItemNameLabel.text;
            string quantity = entry.QuantityInput.text;
            string price = entry.ItemPriceLabel.text;

            GameObject componentGo = _component.Instantiate();
            componentGo.transform.SetParent(parent.root.transform.Find("Content"), false);

            Transform titleGo = componentGo.transform.Find("Content");
            titleGo.GetComponent<Text>().text = $"{quantity}x {name}";

            Transform imageGo = componentGo.transform.Find("Image");
            imageGo.GetComponent<RawImage>().texture = entry.Icon.sprite.texture;

            components.Add(new Component(entry, price, quantity, name, titleGo.GetComponent<Text>(), this));
            _price.text = totalCostLabel;

            CheckInsufficientBalance();
            _rebuilder();
        }

        public void UpdateMultiplierText(float value)
        {
            _multiplierIF.text = value.ToString();
            _multiplierIF.SendOnSubmit();
        }

        private void CheckInsufficientBalance()
        {
            if (MoneyManager.Instance.onlineBalance < totalCost)
            {
                _insufficientBalance.SetActive(true);
            }
            else
            {
                _insufficientBalance.SetActive(false);
            }
        }

        private string totalCostLabel { get => $"Total Cost: ${totalCost.ToString("n0", CultureInfo.InvariantCulture)}"; }

        private void ApplyTemplate(DeliveryShop shop)
        {
            // Reset quanity of every entry in the deliveryShop to 0
            foreach (ListingEntry entry in shop.listingEntries)
            {
                entry.QuantityInput.text = "0";
                entry.QuantityInput.SendOnSubmit();
            }

            foreach (Component component in components)
            {
                component.Update();
            }

            shop.SetIsExpanded(true);
        }
    }

    internal class Component
    {
        public ListingEntry entry;
        public Text content;

        private Entry _parent;
        private int _baseQuantity;
        private string _name;
        private int _price;

        public Component(ListingEntry entry, string price, string quantity, string name, Text content, Entry parent)
        {
            this.entry = entry;
            this.content = content;
            this._name = name;
            this._parent = parent;

            _price = int.Parse(price.Replace("$",""));
            _baseQuantity = int.Parse(quantity);
        }

        public int quantity { get => _baseQuantity; }
        public string name { get => _name; }
        public int price { get => _price; }

        public int MultipliedQuantity()
        {
            return quantity * (int)_parent.multiply;
        }

        public void Update()
        {
            entry.QuantityInput.text = MultipliedQuantity().ToString();
            entry.QuantityInput.SendOnSubmit();
        }

        public void UpdateContent()
        {
            content.text = $"{MultipliedQuantity().ToString()}x {name}";
        }
    }

    internal class Template
    {
        public GameObject gameObject { get; private set; }
        private Asset _entry;

        private Transform _templates;
        private VerticalLayoutGroup _vls;

        private List<Entry> _entries = new List<Entry>();

        public Template(List<EntryData> entries = default)
        {
            gameObject = AssetsManager.Instance.Instantiate("Template");

            _templates = gameObject.transform.Find("Mask/Content/Scroll/View/Templates");
            _vls = _templates.GetComponent<VerticalLayoutGroup>();

            if (_templates == null)
            {
                Melon<Core>.Logger.Warning("Cannot find Templates in TemplateInstance");
                return;
            }

            GameObject scrollGo = gameObject.transform.Find("Mask/Content/Scroll").gameObject;

            _entry = AssetsManager.Instance.GetAsset("Entry");

            foreach (EntryData entry in entries)
            {
                DeliveryShop shop = DeliveryApp.Instance.GetShop(entry.shopName);
                Entry newEntry = AddEntry(entry.name, shop);

                List<ComponentData> components = entry.components.ToList();

                foreach (ListingEntry listingEntry in shop.listingEntries)
                {
                    if (components.Count > 0)
                    {
                        if (listingEntry.ItemNameLabel.text == components[0].name)
                        {
                            listingEntry.QuantityInput.text = components[0].quantity;
                            newEntry.AddComponent(listingEntry, newEntry);
                            listingEntry.QuantityInput.text = "0";
                            components.RemoveAt(0);
                        }
                    }
                    else
                    {
                        break;
                    }   
                }

                newEntry.UpdateMultiplierText(entry.multiplier);
            }
        }

        private void UpdateTemplates()
        {
            // For whatever the reason. calling this method 3 times is required to update the layout
            // otherwise the layout will be fucked up if you add a big entry
            for (int i = 0; i < 3; i++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_vls.GetComponent<RectTransform>());
            }
        }

        public Entry AddEntry(string title, DeliveryShop shop)
        {
            GameObject entryGo = _entry.Instantiate();
            entryGo.transform.SetParent(_templates.transform, false);

            Entry entry = new Entry(title, shop, entryGo, UpdateTemplates);

            _entries.Add(entry);

            return entry;
        }
    }

    internal class TemplateManager
    {
        private Dictionary<string, EntryData> entryData = new Dictionary<string, EntryData>();
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
        
        public bool IsEntryRegister(string name)
        {
            return entryData.ContainsKey(name);
        }

        public bool IsEntryRegister(Entry entry)
        {
            return entryData.ContainsKey(entry.name);
        }

        public void RegisterEntry(Entry entry)
        {
            entryData.Add(entry.name, new EntryData(entry));
        }

        public void UpdateEntry(Entry entry)
        {
            entryData[entry.name] = new EntryData(entry);
        }

        public void RemoveEntry(Entry entry)
        {
            entryData.Remove(entry.name);
        }

        public void Save()
        {
            string path = Path.Combine(ModConfig.ModRootFile, "templates");
            Directory.CreateDirectory(path);
            List<EntryData> entries = entryData.Values.ToList();

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(entries);
            File.WriteAllText(Path.Combine(path, "template.json"), json);
        }

        public Template Load(string file)
        {
            string path = Path.Combine(ModConfig.ModRootFile, "templates", file);
            string data = File.ReadAllText(path);

            if(!File.Exists(path))
            {
                Melon<Core>.Logger.Warning($"Cannot find template file {path}");
                return Instantiate();
            }

            List<EntryData> entries = Newtonsoft.Json.JsonConvert.DeserializeObject<List<EntryData>>(data);
            entryData.Clear();

            foreach (EntryData entry in entries)
            {
                entryData.Add(entry.name, entry);
            }

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(entryData);
            return Instantiate(entries);
        }

        public void Init()
        {
            AssetsManager.Instance.LoadAsset("Template", "ui", "template");
            AssetsManager.Instance.LoadAsset("Entry", "ui", "entry");
            AssetsManager.Instance.LoadAsset("Component", "ui", "component");
        }
        private Template Instantiate(List<EntryData> entries = default)
        {
            var template = AssetsManager.Instance.GetAsset("Template");

            Template templateInstance = new Template(entries);
            return templateInstance;
        }
    }
}
