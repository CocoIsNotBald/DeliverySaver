using MelonLoader;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DeliverySaver
{
    internal class Seeder
    {
        private static Seeder _instance;
        private string signature = AssetsManager.Instance.GetAssetFile("Signature").content;

        public static Seeder Instance 
        { 
            get
            {
                if (_instance == null)
                {
                    _instance = new Seeder();
                }
                return _instance;
            }
        }

        public string Seed(object obj)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(signature + json);

            return Convert.ToBase64String(plainTextBytes, Base64FormattingOptions.None);
        }

        public void SeedToClipboard(object obj)
        {
            GUIUtility.systemCopyBuffer = Seed(obj);
        }

        public T Decode<T>(string encoded) where T : class
        {
            if (encoded == "")
            {
                throw new ArgumentNullException();
            }

            byte[] base64encoded = Convert.FromBase64String(encoded);
            string content = Encoding.UTF8.GetString(base64encoded);


            if (!content.StartsWith(signature))
            {
                throw new Exception("Invalid seed");
            }

            string json = content.Substring(signature.Length);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }
    }
}
