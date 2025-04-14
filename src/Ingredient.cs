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
    internal class IngredientData
    {
        public int baseQuantity;
        public ItemID id;

        public IngredientData(ItemID id, int baseQuantity) 
        {
            this.id = id;
            this.baseQuantity = baseQuantity;
        }
    }

    internal class Ingredient
    {
        private ListingEntry _entry;
        private string _name;
        private int _price;
        private int _stackLimit;
        private Text _content;
        private ItemID _id;
        private int _baseQuantity;
        private Entry _parent;

        public int baseQuantity
        {
            set
            {
                _baseQuantity = value;
                RebuildContent(_parent.multiplier);
            }
            get => _baseQuantity;
        }
        public int price { get => _price; }
        public int stackLimit { get => _stackLimit; }
        public string name { get => _name; }
        public ItemID id { get => _id; }
        public Ingredient(ListingEntry entry, Entry parent)
        {
            // Set class properties
            _entry = entry;
            _id = IngredientRegister.Instance.GetItemID(entry.MatchingListing.Item.ID);

            _stackLimit = entry.MatchingListing.Item.StackLimit;
            _name = entry.ItemNameLabel.text;

            _price = int.Parse(entry.ItemPriceLabel.text.Replace("$", ""));
            _baseQuantity = int.Parse(entry.QuantityInput.text);

            _parent = parent;

            // Set the component to be inside of the entry
            GameObject componentGO = AssetsManager.Instance.Instantiate("Component");
            componentGO.transform.SetParent(parent.gameObject.transform.Find("Content"), false);

            // Set the image of the component
            Transform imageGO = componentGO.transform.Find("Image");
            imageGO.GetComponent<RawImage>().texture = entry.Icon.mainTexture;

            // Set the title of the component
            Transform titleGO = componentGO.transform.Find("Content");

            _content = titleGO.GetComponent<Text>();
            _content.text = $"{entry.QuantityInput.text}x {_name}";
        }

        public int QuantityMultipliedBy(float multiplier)
        {
            return (int)(_baseQuantity * multiplier);
        }

        public void RebuildContent(float multiplier)
        {
            int totalQuantity = QuantityMultipliedBy(multiplier);
            _content.text = $"{totalQuantity}x {_name}";
        }
    }
}
