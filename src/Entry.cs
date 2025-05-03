using HarmonyLib;
using Il2CppFluffyUnderware.Curvy.Generator;
using Il2CppFluffyUnderware.DevTools.Extensions;
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
    internal class Entry : ImmutableEntry
    {
        private InputUI _changeNameInput;
        protected override string _prefabName => "Entry";

        public Entry(EntryData data, Transform parent) : base(data, parent)
        {

        }

        public Entry(string title, DeliveryShop shop, Transform parent) : base(title, shop, parent)
        {
        }

        protected override void Init(string title, DeliveryShop shop, Transform parent)
        {
            base.Init(title, shop, parent);

            Action callback = () => ApplyEntryToShop();
            gameObject.GetComponent<Button>().onClick.AddListener(callback);

            // Text edit to change the name of the entry
            Transform titleInput = gameObject.transform.Find("Head/Header/ChangeNameInput");
            _changeNameInput = new InputUI(titleInput.GetComponent<InputField>());
            _changeNameInput.OnSubmit += ChangeTitle;

            // When the title of the entry is click, make it editable
            Action onTitleEdit = () => OnTitleEdit();
            gameObject.transform.Find("Head/Header/Title").GetComponent<Button>().onClick.AddListener(onTitleEdit);

            // Get the close button and apply the close behaviour
            Transform closeButton = gameObject.transform.Find("Head/Header/CloseButton");
            Action close = () => { Close(); };
            closeButton.GetComponent<Button>().onClick.AddListener(close);

            // Get and define the umpload behaviour
            // When click create a seed in base64 with this entity data
            Transform uploadIcon = gameObject.transform.Find("Head/Header/UploadIcon");

            Action action = () => { OnSingleExportClick(); };
            uploadIcon.GetComponent<Button>().onClick.AddListener(action);

            _multiplyInputUI.OnEndEdit += HandleMultiplicationInputBase;
            _multiplyInputUI.OnSubmit += HandleMultiplicationInput;
            // Don't clear the input after submit
            _multiplyInputUI.clearAfterSubmit = false;
        }

        private void OnTitleEdit()
        {
            _changeNameInput.InputField.text = _title;
            _changeNameInput.Activate();
        }

        private void OnSingleExportClick()
        {
            Seeder.Instance.SeedToClipboard(new List<EntryData> { ToEntryData() });
            Notification.Instance.Show("Seed copied to clipboard");
        }

        private void HandleMultiplicationInputBase(string value)
        {
            try
            {
                float multiplierValue = float.Parse(value.Replace(".", ","));

                multiplier = multiplierValue;

                TemplateManager.Instance.UpdateEntry(this);
            }
            catch (FormatException)
            {
                Notification.Instance.Show("Invalid multiplier value");
                UpdateMultiplierInput();
            }
            catch (ArgumentNullException)
            {
                Notification.Instance.Show("Multiplier value is empty");
                UpdateMultiplierInput();
            }
            catch (OverflowException)
            {
                Notification.Instance.Show("Multiplier value is too large");
                UpdateMultiplierInput();
            }
        }

        private bool HandleMultiplicationInput(string value)
        {
            HandleMultiplicationInputBase(value);

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
            Destroy();
            TemplateManager.Instance.RemoveEntry(this);
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
