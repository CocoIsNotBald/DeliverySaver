using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.UI.Phone.Delivery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using System.Globalization;
using Il2CppFluffyUnderware.DevTools.Extensions;

namespace DeliverySaver
{
    internal class ImmutableEntry
    {
        protected string _title;
        protected DeliveryShop _shop;
        protected Text _price;
        protected float _multiplier;

        protected Text _textTitle;
        protected GameObject _gameObject;
        protected List<Ingredient> _ingredients = new List<Ingredient>();
        protected GameObject _tooLarge;
        protected GameObject _insufficientBalance;
        protected string _shopName;
        protected Transform _parent;
        protected Image _headerImage;
        protected Text _shopGo;
        protected InputUI _multiplyInputUI;

        protected virtual string _prefabName => "ImmutableEntry";

        public string title => _title;
        public string shopName => _shopName;

        public virtual float multiplier
        {
            get { return _multiplier; }
            set
            {
                _multiplier = value;
                // Replace the "," decimal operator from the ToString() function to a dot
                UpdateMultiplierInput();
                UpdateIngredientWithMultiplier();
            }
        }

        protected void UpdateMultiplierInput()
        {
            _multiplyInputUI.InputField.text = _multiplier.ToString().Replace(",", ".");
        }

        public GameObject gameObject { get => _gameObject; }

        public ImmutableEntry(EntryData data, Transform parent)
        {
            DeliveryShop shop = DeliveryApp.Instance.GetShop(data.shopName);
            Init(data.title, shop, parent);

            AddIngredientFromEntryData(data);

            multiplier = data.multiplier;
        }

        public ImmutableEntry(string title, DeliveryShop shop, Transform parent)
        {
            Init(title, shop, parent);

            foreach (ListingEntry component in _shop.listingEntries)
            {
                if (component.QuantityInput.text != "0")
                {
                    AddIngredient(component);
                }
            }

            multiplier = 1.0f;
        }

        protected virtual void Init(string title, DeliveryShop shop, Transform parent)
        {
            _parent = parent;
            _gameObject = AssetsManager.Instance.Instantiate(_prefabName);
            _gameObject.transform.SetParent(_parent, false);

            // Get the text component from the "Title" gameobject and keep it as a private variable to be reusable 
            _textTitle = _gameObject.transform.Find("Head/Header/Title").GetComponent<Text>();

            // Get the image component from the "Background" gameobject and keep it as a private variable to be reusable
            _headerImage = _gameObject.transform.Find("Head/Background").GetComponent<Image>();

            // Get the text component from the "ShopName" gameobject and keep it as a private variable to be reusable
            _shopGo = _gameObject.transform.Find("Head/ShopName").GetComponent<Text>();

            Set(title, shop);

            // Get the order is too large label from the entry game object
            _tooLarge = _gameObject.transform.Find("Footer/TooLarge").gameObject;

            // Get the insufficient balance label from the entry game object
            _insufficientBalance = _gameObject.transform.Find("Footer2/InsufficientBalance").gameObject;

            // Get the total cost label from the entry game object
            _price = _gameObject.transform.Find("Footer2/TotalCost").GetComponent<Text>();

            // Get the multiplier input and create a input ui for ease of use
            Transform multiplierInput = gameObject.transform.Find("Footer/Multiplier/MultiplierInput");
            _multiplyInputUI = new InputUI(multiplierInput.GetComponent<InputField>());
        }

        protected void AddIngredientFromEntryData(EntryData data)
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

        protected void Set(string title, DeliveryShop shop)
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

        public EntryData ToEntryData()
        {
            List<IngredientData> ingredientDatas = new List<IngredientData>();

            foreach (Ingredient ingredient in _ingredients)
            {
                ingredientDatas.Add(new IngredientData(ingredient.id, ingredient.baseQuantity));
            }

            return new EntryData(_title, _multiplier, _shopName, ingredientDatas);
        }

        protected void UpdateIngredientWithMultiplier()
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

        protected void AddIngredientWithQuantity(ListingEntry entry, int quantity)
        {
            _ingredients.Add(new Ingredient(entry, this));
            _ingredients.Last().baseQuantity = quantity;
        }

        protected void AddIngredient(ListingEntry component)
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

        public void Destroy()
        {
            _ingredients.Clear();
            gameObject.Destroy();
        }
    }
}
