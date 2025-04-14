using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.UI.Phone.Delivery;
using MelonLoader;
using System.Globalization;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Il2CppMono.Security.X509.X520;

namespace DeliverySaver
{
    internal class EntryData
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

        public string title => _title;
        public string shopName => _shopName;

        public float multiplier
        {
            get { return _multiplier; }
            set
            {
                _multiplier = value;
                UpdateIngredientWithMultiplier();
            }
        }

        public GameObject gameObject { get => _gameObject; }

        public Entry(EntryData data, Template parent)
        {
            DeliveryShop shop = DeliveryApp.Instance.GetShop(data.shopName);
            Init(data.title, shop, parent.templateContent);

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

            _multiplyInputUI.InputField.text = data.multiplier.ToString();
            this.multiplier = data.multiplier;
        }

        public Entry(string title, DeliveryShop shop, Template parent)
        {
            Init(title, shop, parent.templateContent);

            foreach (ListingEntry component in _shop.listingEntries)
            {
                if (component.QuantityInput.text != "0")
                {
                    AddIngredient(component);
                }
            }

            _multiplyInputUI.InputField.text = "1";
            this.multiplier = 1.0f;
        }

        private void Init(string title, DeliveryShop shop, Transform root)
        {
            _title = title;
            _gameObject = AssetsManager.Instance.Instantiate("Entry");
            _gameObject.transform.SetParent(root, false);
            _shop = shop;
            _shopName = shop.name;

            Action callback = () => ApplyEntryToShop();
            _gameObject.GetComponent<Button>().onClick.AddListener(callback);

            // Set the title to be display on the entry
            Transform titleGo = _gameObject.transform.Find("Head/Header/Title");
            _textTitle = titleGo.GetComponent<Text>();
            _textTitle.text = title;

            // Set the color of the header with the corresponding shop color
            Transform headerImage = _gameObject.transform.Find("Head/Background");
            headerImage.GetComponent<Image>().color = shop.HeaderImage.color;

            // Set the shop name to be display on the entry
            Transform shopGo = _gameObject.transform.Find("Head/ShopName");
            shopGo.GetComponent<Text>().text = shop.name;

            // Text edit to change the name of the entry
            Transform titleInput = _gameObject.transform.Find("Head/Header/ChangeNameInput");
            _changeNameInput = new InputUI(titleInput.GetComponent<InputField>());
            _changeNameInput.OnSubmit += ChangeTitle;

            // When the title of the entry is click, make it editable
            Action onTitleEdit = () => OnTitleEdit();
            titleGo.GetComponent<Button>().onClick.AddListener(onTitleEdit);

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
            _multiplyInputUI.InputField.text = multiplier.ToString().Replace(",", ".");
            _multiplyInputUI.clearAfterSubmit = false;

            // Get the order is too large label from the entry game object
            _tooLarge = _gameObject.transform.Find("Footer/TooLarge").gameObject;

            // Get the insufficient balance label from the entry game object
            _insufficientBalance = _gameObject.transform.Find("Footer2/InsufficientBalance").gameObject;

            // Get the total cost label from the entry game object
            _price = _gameObject.transform.Find("Footer2/TotalCost").GetComponent<Text>();
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
                if (ingredients.Count > 0 && ingredients[0].name == entry.MatchingListing.Item.ID)
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
