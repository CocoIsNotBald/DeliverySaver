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
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(json);

            return Convert.ToBase64String(plainTextBytes);
        }

        public void SeedToClipboard(object obj)
        {
            GUIUtility.systemCopyBuffer = Seed(obj);
        }

        public T Decode<T>(string encoded)
        {
            byte[] base64encoded = Convert.FromBase64String(encoded);
            string json = Encoding.UTF8.GetString(base64encoded);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }
    }
}
