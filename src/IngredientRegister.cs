global using ItemID = uint;

using Il2CppScheduleOne.UI.Phone.Delivery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliverySaver
{

    internal class IngredientRegister
    {
        private ItemID _counter;
        private static IngredientRegister _instance;
        Dictionary<string, ItemID> _ingredientAttributor = new Dictionary<string, ItemID>();

        public static IngredientRegister Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new IngredientRegister();
                }

                return _instance;
            }
        }

        public void Synchronize()
        {
            if (_ingredientAttributor.Count == 0)
            {
                foreach (DeliveryShop shop in DeliveryApp.Instance.deliveryShops)
                {
                    foreach (ListingEntry entry in shop.listingEntries)
                    {
                        if (!_ingredientAttributor.ContainsKey(entry.MatchingListing.Item.ID))
                        {
                            _ingredientAttributor.Add(entry.MatchingListing.Item.ID, _counter);
                            _counter++;
                        }
                    }
                }
            }
        }

        public ItemID GetItemID(string ingredientName)
        {
            return _ingredientAttributor[ingredientName];
        }

        public string GetItemName(ItemID id)
        {
            return _ingredientAttributor.First(e => e.Value == id).Key;
        }
    }
}
