using HarmonyLib;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.UI.Phone.Delivery;
using MelonLoader;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Serialization;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Il2CppMono.Security.X509.X520;

namespace DeliverySaver
{
    internal class EntryDataComparer : IEqualityComparer<EntryData>
    {
        public bool Equals(EntryData x, EntryData y)
        {
            return x.Equals(y);
        }

        public int GetHashCode([DisallowNull] EntryData obj)
        {
            return obj.GetHashCode();
        }
    }

    internal class EntryData : IEquatable<EntryData>
    {
        public string title;
        public float multiplier;
        private string _shopName;
        private List<IngredientData> _ingredients;

        public string shopName { get => _shopName; }
        public List<IngredientData> ingredients { get => _ingredients; }

        public EntryData(string title, float multiplier, string shopName, List<IngredientData> ingredients) 
        {
            this.title = title;
            this.multiplier = multiplier;
            _shopName = shopName;
            _ingredients = ingredients;
        }

        public static EntryData FromDeliveryShop(string title, float multiplier,  DeliveryShop shop)
        {
            List<IngredientData> ingredientDatas = new List<IngredientData>();

            foreach (ListingEntry component in shop.listingEntries)
            {
                if (component.QuantityInput.text != "0")
                {
                    ingredientDatas.Add(IngredientData.FromListingEntry(component));
                }
            }

            return new EntryData(title, multiplier, shop.name, ingredientDatas);
        }

        public bool Equals(EntryData other)
        {
            return title == other.title;
        }

        public override int GetHashCode()
        {
            return this.title.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if(obj is EntryData)
            {
                return Equals((EntryData)obj);
            }

            return base.Equals(obj);
        }
    }

    // Entry will be save and clear if the scene is not Main (Main scene = Game)
    internal class Entry
    {
        private string _title;
        private DeliveryShop _shop;
        private AssetBundle _entry;
        private Text _price;
        private float _multiplier;

        private Text _textTitle;
        private InputUI _changeNameInput;
        private GameObject _gameObject;
        private List<Ingredient> _ingredients = new List<Ingredient>();
        private GameObject _tooLarge;
        private GameObject _insufficientBalance;
        private string _shopName;
        private InputUI _multiplyInputUI;
        private Template _parent;
        private Image _headerImage;
        private Text _shopGo;

        public string title => _title;
        public string shopName => _shopName;

        public float multiplier
        {
            get { return _multiplier; }
            set
            {
                _multiplier = value;
                _multiplyInputUI.InputField.text = _multiplier.ToString().Replace(",", ".");
                UpdateIngredientWithMultiplier();
            }
        }

        public GameObject gameObject { get => _gameObject; }

        public Entry(EntryData data, Template parent)
        {
            DeliveryShop shop = DeliveryApp.Instance.GetShop(data.shopName);
            Init(data.title, shop, parent.templateContent);

            AddIngredientFromEntryData(data);

            multiplier = data.multiplier;
        }

        public Entry(string title, DeliveryShop shop, Template parent)
        {
            _parent = parent;

            Init(title, shop, parent.templateContent);

            foreach (ListingEntry component in _shop.listingEntries)
            {
                if (component.QuantityInput.text != "0")
                {
                    AddIngredient(component);
                }
            }

            multiplier = 1.0f;
        }

        private void Init(string title, DeliveryShop shop, Transform root)
        {
            _gameObject = AssetsManager.Instance.Instantiate("Entry");
            _gameObject.transform.SetParent(root, false);

            // Get the text component from the "Title" gameobject and keep it as a private variable to be reusable 
            _textTitle = _gameObject.transform.Find("Head/Header/Title").GetComponent<Text>();

            // Get the image component from the "Background" gameobject and keep it as a private variable to be reusable
            _headerImage = _gameObject.transform.Find("Head/Background").GetComponent<Image>();

            // Get the text component from the "ShopName" gameobject and keep it as a private variable to be reusable
            _shopGo = _gameObject.transform.Find("Head/ShopName").GetComponent<Text>();

            Set(title, shop);

            Action callback = () => ApplyEntryToShop();
            _gameObject.GetComponent<Button>().onClick.AddListener(callback);

            // Text edit to change the name of the entry
            Transform titleInput = _gameObject.transform.Find("Head/Header/ChangeNameInput");
            _changeNameInput = new InputUI(titleInput.GetComponent<InputField>());
            _changeNameInput.OnSubmit += ChangeTitle;

            // When the title of the entry is click, make it editable
            Action onTitleEdit = () => OnTitleEdit();
            _gameObject.transform.Find("Head/Header/Title").GetComponent<Button>().onClick.AddListener(onTitleEdit);

            // Get the close button and apply the close behaviour
            Transform closeButton = _gameObject.transform.Find("Head/Header/CloseButton");
            Action close = () => { Close(); };
            closeButton.GetComponent<Button>().onClick.AddListener(close);

            // Get and define the umpload behaviour
            // When click create a seed in base64 with this entity data
            Transform uploadIcon = _gameObject.transform.Find("Head/Header/UploadIcon");

            Action action = () => { OnSingleExportClick(); };
            uploadIcon.GetComponent<Button>().onClick.AddListener(action);

            // Get the multiplier input and create a input ui for ease of use
            // Also replace the "," decimal operator from the ToString() function to a dot
            Transform multiplierInput = _gameObject.transform.Find("Footer/Multiplier/MultiplierInput");
            _multiplyInputUI = new InputUI(multiplierInput.GetComponent<InputField>());

            _multiplyInputUI.OnSubmit += HandleMultiplicationInput;
            _multiplyInputUI.clearAfterSubmit = false;

            // Get the order is too large label from the entry game object
            _tooLarge = _gameObject.transform.Find("Footer/TooLarge").gameObject;

            // Get the insufficient balance label from the entry game object
            _insufficientBalance = _gameObject.transform.Find("Footer2/InsufficientBalance").gameObject;

            // Get the total cost label from the entry game object
            _price = _gameObject.transform.Find("Footer2/TotalCost").GetComponent<Text>();
        }

        private void AddIngredientFromEntryData(EntryData data)
        {
            DeliveryShop shop = DeliveryApp.Instance.GetShop(data.shopName);
            List<IngredientData> ingredients = new List<IngredientData>(data.ingredients);

            foreach (ListingEntry entry in shop.listingEntries)
            {
                if (ingredients.Count == 0)
                {
                    break;
                }

                if (entry.MatchingListing.Item.ID == IngredientRegister.Instance.GetItemName(ingredients[0].id))
                {
                    AddIngredientWithQuantity(entry, ingredients[0].baseQuantity);
                    ingredients.RemoveAt(0);
                }
            }
        }

        private void Set(string title, DeliveryShop shop)
        {
            _title = title;
            _shop = shop;
            _shopName = shop.name;

            // Set the title to be display on the entry
            _textTitle.text = title;

            // Set the color of the header with the corresponding shop color
            _headerImage.GetComponent<Image>().color = shop.HeaderImage.color;

            // Set the shop name to be display on the entry
            _shopGo.text = shop.name;
        }

        private void OnSingleExportClick()
        {
            Seeder.Instance.SeedToClipboard(new List<EntryData> { ToEntryData() });
            Notification.Instance.Show("Seed copied to clipboard");
        }

        public EntryData ToEntryData()
        {
            List<IngredientData> ingredientDatas = new List<IngredientData>();

            foreach(Ingredient ingredient in _ingredients)
            {
                ingredientDatas.Add(new IngredientData(ingredient.id, ingredient.baseQuantity));
            }

            return new EntryData(_title, _multiplier, _shopName, ingredientDatas);
        }

        private void UpdateIngredientWithMultiplier()
        {
            int totalCost = 0;
            int totalStack = 0;

            foreach (Ingredient ingredient in _ingredients)
            {
                int quantity = ingredient.QuantityMultipliedBy(_multiplier);
                ingredient.RebuildContent(_multiplier);
                totalCost += ingredient.price * quantity;
                totalStack += (int)Math.Ceiling((double)quantity / ingredient.stackLimit);
            }

            if (totalStack > 16)
            {
                _tooLarge.SetActive(true);
            }
            else
            {
                _tooLarge.SetActive(false);
            }

            if (totalCost > MoneyManager.Instance.onlineBalance)
            {
                _insufficientBalance.SetActive(true);
            }
            else
            {
                _insufficientBalance.SetActive(false);
            }

            _price.text = $"Total cost: ${(200 + totalCost).ToString("n0", CultureInfo.InvariantCulture)}";
        }

        private bool HandleMultiplicationInput(string value)
        {
            float multiplierValue = float.Parse(value.Replace(".", ","));

            multiplier = multiplierValue;

            TemplateManager.Instance.UpdateEntry(this);

            return true;
        }

        private void ApplyEntryToShop()
        {
            List<Ingredient> ingredients = new(_ingredients);

            // Iterate over every entry in the targeted shop
            foreach (ListingEntry entry in _shop.listingEntries)
            {
                // Apply the quantity of the ingredient to the shop ingredient if both name match
                // And then clear one entry from the copied ingredients list
                if (ingredients.Count > 0 && ingredients[0].name == entry.ItemNameLabel.text)
                {
                    entry.QuantityInput.text = ingredients[0].QuantityMultipliedBy(_multiplier).ToString();
                    ingredients.RemoveAt(0);
                }
                else
                {
                    entry.QuantityInput.text = "0";
                }
                entry.QuantityInput.SendOnSubmit();
            }

            _shop.SetIsExpanded(true);
        }

        private void Close()
        {
            GameObject.Destroy(_gameObject);
            TemplateManager.Instance.RemoveEntry(this);
        }

        private void OnTitleEdit()
        {
            _changeNameInput.InputField.text = _title;
            _changeNameInput.Activate();
        }

        private void AddIngredientWithQuantity(ListingEntry entry, int quantity)
        {
            _ingredients.Add(new Ingredient(entry, this));
            _ingredients.Last().baseQuantity = quantity;
        }

        private void AddIngredient(ListingEntry component)
        {
            _ingredients.Add(new Ingredient(component, this));
        }

        public void SetDataFromEntryData(EntryData data)
        {
            DeliveryShop shop = DeliveryApp.Instance.GetShop(data.shopName);

            Set(data.title, shop);

            List<IngredientData> ingredients = new List<IngredientData>(data.ingredients);

            foreach (Ingredient ingredient in _ingredients) 
            {
                ingredient.Destroy();
            }

            _ingredients.Clear();

            AddIngredientFromEntryData(data);

            multiplier = data.multiplier;

            TemplateManager.Instance.template.RebuildEveryLayout();
        }

        private bool ChangeTitle(string value)
        {
            string oldTitle = _title;
            _title = value;
            _textTitle.text = value;
            TemplateManager.Instance.UpdateEntryTitle(oldTitle, this);
            return true;
        }
    }
}
