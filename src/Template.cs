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

namespace DeliverySaver
{
    public class ComponentData : IEquatable<ComponentData>
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

        public bool Equals(ComponentData other)
        {
            return
                name == other.name &&
                quantity == other.quantity;
        }
    }
    public class EntryData : IEquatable<EntryData>
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

        public bool Equals(EntryData other)
        {
            return
                name == other.name &&
                shopName == other.shopName &&
                multiplier == other.multiplier &&
                components.SequenceEqual(other.components);
        }
    }

    public class Entry
    {
        public List<Component> components = new List<Component>();
        public string name { get; private set; }
        public GameObject root;
        public float multiply = 1;
        public string shopName { get => root.transform.Find("Head/ShopName").GetComponent<Text>().text; }

        private AssetBundle _component;
        private InputField _multiplierIF;
        private GameObject _tooLarge;
        private GameObject _insufficientBalance;
        private Text _price;
        private Action _rebuilder;

        public Entry(string title, DeliveryShop shop, GameObject root, Action rebuilder)
        {
            name = title;
            this.root = root;

            Action callback = () => ApplyTemplate(shop);
            root.GetComponent<Button>().onClick.AddListener(callback);

            Transform titleGo = root.transform.Find("Head/Header/Title");
            Text titleText = titleGo.GetComponent<Text>();
            titleText.text = title;

            Transform titleInput = root.transform.Find("Head/Header/ChangeNameInput");

            InputUI inputTitleUi = new InputUI(titleInput.GetComponent<InputField>());
            inputTitleUi.OnSubmit += ChangeTitle(titleText);

            titleGo.GetComponent<Button>().onClick.AddListener(OnTitleEdit(inputTitleUi));

            Transform shopGo = root.transform.Find("Head/ShopName");
            shopGo.GetComponent<Text>().text = shop.name;

            Transform headerImage = root.transform.Find("Head/Background");
            headerImage.GetComponent<Image>().color = shop.HeaderImage.color;

            Transform uploadIcon = root.transform.Find("Head/Header/UploadIcon");
            
            Action action = () => { OnSingleExportClick(); };
            uploadIcon.GetComponent<Button>().onClick.AddListener(action);

            Transform closeButton = root.transform.Find("Head/Header/CloseButton");

            Action close = () => { Close(); };
            closeButton.GetComponent<Button>().onClick.AddListener(close);

            Transform multiplierInput = root.transform.Find("Footer/Multiplier/MultiplierInput");
            InputField multiplierIF = multiplierInput.GetComponent<InputField>();
            multiplierIF.text = multiply.ToString();

            Action<string> multiplierAction = (value) => HandleMulitplicationInput(value);
            multiplierIF.onSubmit.AddListener(multiplierAction);
            _multiplierIF = multiplierIF;

            _tooLarge = root.transform.Find("Footer/TooLarge").gameObject;
            _insufficientBalance = root.transform.Find("Footer2/InsufficientBalance").gameObject;
            _price = root.transform.Find("Footer2/TotalCost").GetComponent<Text>();
            _rebuilder = rebuilder;

            _component = AssetsManager.Instance.GetAssetBundle("Component");
            _rebuilder();
        }

        private Action OnTitleEdit(InputUI inputUI)
        {
            Action onTitleEdit = () => {
                inputUI.InputField.text = name;
                inputUI.Activate();
            };

            return onTitleEdit;
        }

        private Func<string, bool> ChangeTitle(Text titleText)
        {
            Func<string, bool> action = (value) => 
            {
                titleText.text = value;
                string oldName = name;
                name = value;
                TemplateManager.Instance.GetTemplateGameData().UpdateTitle(oldName, this);
                return true;
            };

            return action;
        }

        private void OnSingleExportClick()
        {
            Notification.Instance.Show("Entry seed copied to clipboard");

            EntryData entryData = TemplateManager.Instance.GetTemplateGameData().entryData[name];
            Seeder.Instance.SeedToClipboard(new List<EntryData> { entryData });
        }

        private void Close()
        {
            UnityEngine.Object.Destroy(root);
            TemplateManager.Instance.GetTemplateGameData().RemoveEntry(this);
        }

        private float totalCost { get => components.Sum(c => c.price * c.MultipliedQuantity()) + 200; }

        private void HandleMulitplicationInput(string value)
        {
            multiply = float.Parse(value.Replace(".", ","));

            foreach (Component component in components)
            {
                component.UpdateContent();
                int contentCount = component.MultipliedQuantity();
            }

            _price.text = totalCostLabel;
            TemplateManager.Instance.GetTemplateGameData().UpdateEntry(this);

            CheckInsufficientBalance();
        }

        public void AddComponent(ListingEntry entry)
        {
            AddComponentWithQuantity(entry, entry.QuantityInput.text);
        }

        private void CheckStack()
        {
            int stack = 0;

            foreach (Component component in components)
            {
                int contentCount = component.MultipliedQuantity();
                stack += (int)Math.Ceiling((float)contentCount / component.stackLimit);
            }

            if (stack > 16)
            {
                _tooLarge.SetActive(true);
            }
            else
            {
                _tooLarge.SetActive(false);
            }
        }

        public void AddComponentWithQuantity(ListingEntry entry, string quantity)
        {
            string name = entry.ItemNameLabel.text;
            string price = entry.ItemPriceLabel.text;

            GameObject componentGo = _component.Instantiate();
            componentGo.transform.SetParent(root.transform.Find("Content"), false);

            Transform titleGo = componentGo.transform.Find("Content");
            titleGo.GetComponent<Text>().text = $"{quantity}x {name}";

            Transform imageGo = componentGo.transform.Find("Image");
            imageGo.GetComponent<RawImage>().texture = entry.Icon.sprite.texture;

            int stackLimit = entry.MatchingListing.Item.StackLimit;

            components.Add(new Component(entry, price, quantity, name, titleGo.GetComponent<Text>(), this, stackLimit));
            _price.text = totalCostLabel;

            CheckInsufficientBalance();
            _rebuilder();

            CheckStack();
        }

        public void UpdateMultiplierText(float value)
        {
            _multiplierIF.text = value.ToString();
            _multiplierIF.SendOnSubmit();
            
            CheckStack();
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

    public class Component
    {
        public ListingEntry entry;
        public Text content;

        private Entry _parent;
        private int _baseQuantity;
        private string _name;
        private int _price;
        private int _stackLimit;

        public Component(ListingEntry entry, string price, string quantity, string name, Text content, Entry parent, int stackLimit)
        {
            this.entry = entry;
            this.content = content;
            
            _stackLimit = stackLimit;
            _name = name;
            _parent = parent;

            _price = int.Parse(price.Replace("$",""));
            _baseQuantity = int.Parse(quantity);
        }

        public int stackLimit { get => _stackLimit; }
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

    public class Template
    {
        public GameObject gameObject { get; private set; }
        private AssetBundle _entry;

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

            Action callback = () => { OnExportTemplate(); };

            gameObject.transform.Find("Mask/Content/ExportSeed").GetComponent<Button>().onClick.AddListener(callback); ;

            GameObject scrollGo = gameObject.transform.Find("Mask/Content/Scroll").gameObject;

            _entry = AssetsManager.Instance.GetAssetBundle("Entry");

            if(entries == null)
            {
                return;
            }

            foreach (EntryData data in entries)
            {
                AddEntryData(data);
            }
        }

        private void OnExportTemplate()
        {
            Notification.Instance.Show("Template seed copied to clipboard");

            List<EntryData> entryDatas = TemplateManager.Instance.GetTemplateGameData().entryData.Values.ToList();
            Seeder.Instance.SeedToClipboard(entryDatas);
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

        public void AddEntryData(EntryData entryData)
        {
            DeliveryShop shop = DeliveryApp.Instance.GetShop(entryData.shopName);
            Entry entry = AddEntry(entryData.name, shop);

            List<ComponentData> components = entryData.components;

            foreach (ListingEntry listingEntry in shop.listingEntries)
            {
                if (components.Count > 0)
                {
                    if (listingEntry.ItemNameLabel.text == components[0].name)
                    {
                        entry.AddComponentWithQuantity(listingEntry, components[0].quantity);
                        components.RemoveAt(0);
                    }
                }
                else
                {
                    break;
                }
            }

            entry.UpdateMultiplierText(entryData.multiplier);
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

    public class TemplateGameData
    {
        private SortedDictionary<string, EntryData> _entryData = new SortedDictionary<string, EntryData>();
        public string gameName { get; private set; }
        public string gameSeed { get; private set; }
        public string filename;
        public string gameFullName { get => $"{gameName}_{gameSeed}"; }

        public SortedDictionary<string, EntryData> entryData { get => _entryData; }

        public TemplateGameData(List<EntryData> entryData = default)
        {
            gameName = GameManager.Instance.OrganisationName;
            gameSeed = GameManager.Instance.seed.ToString();

            filename = $"template_{gameFullName}.json";

            if (entryData == null)
            {
                return;
            }

            Populate(entryData);
        }

        public bool IsEntryRegister(string name)
        {
            return _entryData.ContainsKey(name);
        }

        public bool IsEntryRegister(Entry entry)
        {
            return _entryData.ContainsKey(entry.name);
        }

        public void RegisterEntry(Entry entry)
        {
            _entryData.Add(entry.name, new EntryData(entry));
        }

        public void UpdateEntry(Entry entry)
        {
            _entryData[entry.name] = new EntryData(entry);
        }
        
        public void UpdateTitle(string oldTitle, Entry entry)
        {
            _entryData[oldTitle].name = entry.name;
        }

        public void RemoveEntry(Entry entry)
        {
            _entryData.Remove(entry.name);
        }

        public void Populate(List<EntryData> entryData)
        {
            foreach (EntryData data in entryData)
            {
                if (HasExactEntry(data))
                {
                    if(entryData.Count == 1)
                    {
                        throw new EntryAlreadyExistsException("Entry already exists");
                    }
                    else
                    {
                        Notification.Instance.Show($"Entry {data.name} already exists");
                    }
                    continue;
                }

                data.name = FindNameForEntry(data);

                _entryData.Add(data.name, data);

                if(TemplateManager.Instance.template != null)
                {
                    TemplateManager.Instance.template.AddEntryData(data);
                }
            }
        }

        private bool HasExactEntry(EntryData entry)
        {
            if(_entryData.ContainsKey(entry.name))
            {
                return entry.Equals(_entryData[entry.name]);
            }

            return false;
        }

        private string FindNameForEntry(EntryData data, int count = 0)
        {
            if (_entryData.ContainsKey(data.name))
            {
                data.name = data.name.Replace($" ({count})", "");
                count += 1;
                string endString = $"{data.name} ({count})";
                data.name = endString;
                return FindNameForEntry(data, count);
            }

            return data.name;
        }
    }

    public class EntryAlreadyExistsException : Exception
    {
        public EntryAlreadyExistsException(string message) : base(message)
        {
        }
    }

    public class TemplateManager
    {
        public List<TemplateGameData> templates = new List<TemplateGameData>();
        private Template _template = null;

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

        private string GetGameFullName()
        {
            string gameName = GameManager.Instance.OrganisationName;
            string gameSeed = GameManager.Instance.seed.ToString();
            string gameFullName = $"{gameName}_{gameSeed}";
            return gameFullName;
        }

        public bool HasGame()
        {
            return templates.FirstOrDefault((t) => t.gameFullName == GetGameFullName()) != null;
        }

        public TemplateGameData GetTemplateGameData()
        {
            TemplateGameData gameData = templates.FirstOrDefault(t => t.filename == GetTemplateForSave());

            if (gameData == null)
            {
                gameData = new TemplateGameData();
                templates.Add(gameData);
            }

            return gameData;
        }

        public string GetTemplateForSave()
        {
            return $"template_{GameManager.Instance.OrganisationName}_{GameManager.Instance.seed}.json";
        }

        public void Save()
        {
            foreach (TemplateGameData template in templates)
            {
                SaveTemplate(template);
            }
        }

        private void SaveTemplate(TemplateGameData template)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(template.entryData.Values.ToList());
            string path = Path.Combine(ModConfig.ModRootFile, template.filename);

            File.WriteAllText(Path.Combine(ModConfig.ModRootFile, template.filename), json);
        }

        public TemplateGameData GetCurrentTemplateGame()
        {
            return templates.FirstOrDefault(t => t.gameFullName == GetGameFullName());
        }

        public Template Load(string file)
        {
            string path = Path.Combine(ModConfig.ModRootFile, file);

            if (!File.Exists(path))
            {
                return Instantiate();
            }

            string data = File.ReadAllText(path);

            List<EntryData> entries = Newtonsoft.Json.JsonConvert.DeserializeObject<List<EntryData>>(data);
            templates.Add(new TemplateGameData(entries));

            return Instantiate(templates.Last().entryData.Values.ToList());
        }

        public void Populate(List<EntryData> entries)
        {
            GetCurrentTemplateGame().Populate(entries);
        }

        public void Init()
        {
            AssetsManager.Instance.LoadAssetBundleFromResources("Template", "ui.template");
            AssetsManager.Instance.LoadAssetBundleFromResources("Entry", "ui.entry");
            AssetsManager.Instance.LoadAssetBundleFromResources("Component", "ui.component");
        }

        public Template Instantiate(List<EntryData> entries = default)
        {
            if(_template != null && _template.gameObject != null)
            {
                return _template;
            }

            Template templateInstance = new Template(entries);
            _template = templateInstance;
            return templateInstance;
        }
    }
}
