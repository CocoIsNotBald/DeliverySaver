using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;
using System.Reflection;
using Unity.Collections;
using Il2CppInterop.Runtime.Runtime;

namespace DeliverySaver
{
    enum AssetAccessType
    {
        FileSystem,
        Resources,
    }

    class Asset
    {
        public string name { get; }
        public string path { get; private set; }
        public AssetAccessType assetAccessType { get; }
        public Il2CppAssetBundle assetBundle { get; private set; }
        bool valid { get; } = false;

        public Asset(AssetAccessType assetAccessType, string name, string path)
        {
            this.assetAccessType = assetAccessType;
            this.name = name;
            
            switch(assetAccessType)
            {
                case AssetAccessType.FileSystem:
                    LoadFromFile(path);
                    break;
                case AssetAccessType.Resources:
                    LoadFromResources(path);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(assetAccessType), assetAccessType, null);
            }
            
            if (assetBundle == null)
            {
                Melon<Core>.Logger.Warning($"Cannot load asset {this.path}");
                return;
            }

            valid = true;
        }

        private void LoadFromFile(string path)
        {
           

            this.path = Path.Combine(ModConfig.AssetPath, path);

            if (!File.Exists(this.path))
            {
                Melon<Core>.Logger.Warning($"ACannot find asset {this.path}");
                return;
            }

            assetBundle = Il2CppAssetBundleManager.LoadFromFile(this.path);
        }

        private void LoadFromResources(string path)
        {
            this.path = $"{AssetsManager.Instance.resourcesPrefix}{path}";

            using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(this.path))
            {
                if (stream == null)
                {
                    Melon<Core>.Logger.Warning($"Cannot find asset {this.path}");
                    return;
                }

                using (var memoryStream = new System.IO.MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    var buffer = memoryStream.ToArray();
                    var il2cppStream = new Il2CppSystem.IO.MemoryStream(buffer);
                    assetBundle = Il2CppAssetBundleManager.LoadFromStream(il2cppStream);
                }
            }
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
        public string resourcesPrefix = "";

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

        public void LoadFile(string name, string path)
        {
            assets.Add(new Asset(AssetAccessType.FileSystem,name, path));
        }

        public void LoadResources(string name, string path)
        {
            assets.Add(new Asset(AssetAccessType.Resources, name, path));
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
