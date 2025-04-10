using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;

namespace DeliverySaver
{
    class Asset
    {
        public string name { get; }
        public string path { get; }
        public Il2CppAssetBundle assetBundle { get; }
        bool valid { get; } = false;

        public Asset(string name, params string[] path)
        {
            this.name = name;
            this.path = ModConfig.AssetPath;

            foreach (string p in path)
            {
                this.path = Path.Combine(this.path, p);
            }

            if (!File.Exists(this.path))
            {
                Melon<Core>.Logger.Warning($"Cannot find asset {this.path}");
                return;
            }

            assetBundle = Il2CppAssetBundleManager.LoadFromFile(this.path);
            
            if (assetBundle == null)
            {
                Melon<Core>.Logger.Warning($"Cannot load asset {this.path}");
                return;
            }

            valid = true;
        }

        public GameObject Instantiate()
        {
            if(valid)
            {
                GameObject data = GameObject.Instantiate(assetBundle.LoadAsset<GameObject>(name));
                
                return data;
            }

            return null;
        }

        public void Unload()
        {
            if (valid)
            {
                assetBundle.Unload(true);
            }
        }
    }
    internal class AssetsManager
    {
        List<Asset> assets = new List<Asset>();

        private static AssetsManager _instance = null;

        public static AssetsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AssetsManager();
                }
                return _instance;
            }
        }

        AssetsManager() { }

        public void LoadAsset(string name, params string[] path)
        {
            assets.Add(new Asset(name, path));
        }

        public Asset GetAsset(string name)
        {
            foreach (Asset asset in assets)
            {
                if (asset.name == name)
                {
                    return asset;
                }
            }
            Melon<Core>.Logger.Warning($"Get: Cannot find asset {name}");
            return null;
        }

        public void RemoveAsset(string name)
        {
            foreach (Asset asset in assets)
            {
                if (asset.name == name)
                {
                    asset.Unload();
                    assets.Remove(asset);
                    return;
                }
            }
            Melon<Core>.Logger.Warning($"Remove: Cannot find asset {name}");
        }

        public GameObject Instantiate(string name)
        {
            foreach (Asset asset in assets)
            {
                if (asset.name == name)
                {
                    return asset.Instantiate();
                }
            }
            Melon<Core>.Logger.Warning($"Instantiate: Cannot find asset {name}");
            return null;
        }
    }
}
