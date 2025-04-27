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

    abstract class Asset
    {
        public string name { get; }
        public string path { get; private set; }
        public AssetAccessType assetAccessType { get; }
        public bool valid { get; private set; } = true;

        public Asset(AssetAccessType assetAccessType, string name, string path)
        {
            this.assetAccessType = assetAccessType;
            this.name = name;

            switch (assetAccessType)
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
        }

        private void LoadFromFile(string path)
        {
            this.path = Path.Combine(ModConfig.AssetPath, path);

            if (!File.Exists(this.path))
            {
                Melon<Core>.Logger.Warning($"Cannot find asset {this.path}");
                valid = false;
                return;
            }

            LoadFromFileBase(this.path);
        }

        private void LoadFromResources(string path)
        {
            this.path = $"{AssetsManager.Instance.resourcesPrefix}{path}";

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(this.path))
            {
                if (stream == null)
                {
                    Melon<Core>.Logger.Warning($"Cannot find asset {this.path}");
                    valid = false;
                    return;
                }

                LoadFromResourcesBase(stream);
            }
        }

        protected abstract void LoadFromFileBase(string path);
        protected abstract void LoadFromResourcesBase(Stream stream);
        public abstract void Unload();
    }

    class AssetBundle : Asset
    {
        public Il2CppAssetBundle assetBundle { get; private set; }

        public AssetBundle(AssetAccessType assetAccessType, string name, string path) : base(assetAccessType, name, path)
        {
            if (assetBundle == null)
            {
                Melon<Core>.Logger.Warning($"Cannot load asset {this.path}");
                return;
            }
        }

        public GameObject Instantiate()
        {
            if (valid)
            {
                GameObject prefab = assetBundle.LoadAsset<GameObject>(name);

                if(prefab == null)
                {
                    throw new Exception($"Asset bundle prefab {name} is null on instantiation. Please verify that {name} match the exported unity gameobject name in the editor");
                }

                GameObject data = GameObject.Instantiate(prefab);

                return data;
            }

            return null;
        }

        public override void Unload()
        {
            if (valid)
            {
                assetBundle.Unload(true);
            }
        }

        protected override void LoadFromFileBase(string path)
        {
            assetBundle = Il2CppAssetBundleManager.LoadFromFile(path);
        }

        protected override void LoadFromResourcesBase(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                var buffer = memoryStream.ToArray();
                var il2cppStream = new Il2CppSystem.IO.MemoryStream(buffer);
                assetBundle = Il2CppAssetBundleManager.LoadFromStream(il2cppStream);
                il2cppStream.Close();
            }
        }
    }

    class AssetFile : Asset
    {
        public string content { get; private set; }

        public AssetFile(AssetAccessType assetAccessType, string name, string path) : base(assetAccessType, name, path)
        {
            if (string.IsNullOrEmpty(content))
            {
                Melon<Core>.Logger.Warning($"Cannot load asset {this.path}");
                return;
            }
        }

        public override void Unload()
        {
            content = "";
        }

        protected override void LoadFromFileBase(string path)
        {
            content = File.ReadAllText(path);
        }

        protected override void LoadFromResourcesBase(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                content = reader.ReadToEnd();
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

        public void LoadAssetBundleFromPath(string name, string path)
        {
            assets.Add(new AssetBundle(AssetAccessType.FileSystem, name, path));
        }

        public void LoadAssetBundleFromResources(string name, string path)
        {
            assets.Add(new AssetBundle(AssetAccessType.Resources, name, path));
        }

        public void LoadFileFromPath(string name, string path)
        {
            assets.Add(new AssetFile(AssetAccessType.FileSystem, name, path));
        }
        public void LoadFileFromResources(string name, string path)
        {
            assets.Add(new AssetFile(AssetAccessType.Resources, name, path));
        }

        public AssetBundle GetAssetBundle(string name)
        {
            foreach (Asset asset in assets)
            {
                if (asset.name == name && asset is AssetBundle)
                {
                    return (AssetBundle)asset;
                }
            }
            Melon<Core>.Logger.Warning($"Get: Cannot find asset {name}");
            return null;
        }

        public AssetFile GetAssetFile(string name)
        {
            foreach (Asset asset in assets)
            {
                if (asset.name == name && asset is AssetFile)
                {
                    return (AssetFile)asset;
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
                if (asset.name == name && asset is AssetBundle)
                {
                    return ((AssetBundle)asset).Instantiate();
                }
            }
            Melon<Core>.Logger.Warning($"Instantiate: Cannot find asset {name}");
            return null;
        }
    }
}
