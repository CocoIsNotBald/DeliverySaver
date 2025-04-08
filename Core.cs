using Il2CppFluffyUnderware.Curvy.Generator;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppToolBuddy.ThirdParty.VectorGraphics;
using MelonLoader;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(DeliverySaver.Core), "DeliverySaver", "1.0.0", "Coco", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

class SomeMono : MonoBehaviour
{
    public void Start()
    {
        // This is just a placeholder to ensure the class is not empty.
    }
}

namespace DeliverySaver
{
    public class Core : MelonMod
    {
        private string _modRootFile = Path.Combine(Application.dataPath, "..", "Mods", "DeliverySaver");
        private string _scene;
        private bool _loaded = false;
        private bool _loaded2 = false;
        private Template template;
        private TemplateInstance templateInstance = null;
        private GameObject templateName = null;
        private DeliveryShop _deliveryShop = null;

        public override void OnInitializeMelon()
        {
            if (!Directory.Exists(_modRootFile))
            {
                Directory.CreateDirectory(_modRootFile);
            }

            template = new Template();
            AssetsManager.Instance.LoadAsset("SaveButton", "ui", "saveButton");
            AssetsManager.Instance.LoadAsset("TemplateName", "ui", "templatename");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            _scene = sceneName;
        }

        //public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        //{
        //    if(sceneName == "Menu")
        //    {
        //        LoadAssets(GameObject.Find("Scene").transform);
        //    }
        //}

        public override void OnUpdate()
        {
            if(_scene == "Main")
            {
                if (DeliveryApp.Instance && !_loaded)
                {
                    InitTemplateName();
                    AddAppSaveButton(DeliveryApp.Instance);
                    AddTemplatePanel(DeliveryApp.Instance);
                    _loaded = true;
                }
            }
        }

        private void InitTemplateName()
        {
            if (templateName == null)
            {
                templateName = AssetsManager.Instance.Instantiate("TemplateName");
                templateName.transform.SetParent(DeliveryApp.Instance.appContainer.transform, false);
                templateName.SetActive(false);

                Action<string> action = (string value) => OnSaveClick(value);
                GetTemplateNameInput().onSubmit.AddListener(action);
            }
        }

        private void AddTemplatePanel(DeliveryApp app)
        {
            templateInstance = template.Instantiate();

            templateInstance.gameObject.transform.SetParent(app.appContainer.transform, false);
            templateInstance.gameObject.transform.localPosition = new Vector3(393, -32, 0);
        }

        private void AddAppSaveButton(DeliveryApp app)
        {
            foreach (DeliveryShop deliveryShop in app.deliveryShops)
            {
                LoggerInstance.Msg($"Loading button for delivery shop {deliveryShop.name}");

                Transform panel = GetPanel(deliveryShop);

                LoggerInstance.Msg(panel.ToString());
                if (panel)
                {
                    GameObject go = AssetsManager.Instance.Instantiate("SaveButton");
                    Button saveButton = go.GetComponent<Button>();

                    go.transform.SetParent(panel, false);
                    go.transform.localPosition = new Vector3(122, -13, 0);

                    Action callback = () => 
                    {
                        _deliveryShop = deliveryShop;
                        OnNameValidated();
                    };
                    saveButton.onClick.AddListener(callback);

                    LoggerInstance.Msg($"Save button created for {deliveryShop.name}");
                }
                else
                {
                    LoggerInstance.Warning($"Cannot get panel for deliveryShop '{deliveryShop.name}'");
                }
            }
        }

        private InputField GetTemplateNameInput()
        {
            return templateName.transform.Find("InputName").GetComponent<InputField>();
        }

        private void OnNameValidated()
        {
            if(!HasComponent())
            {
                return;
            }

            templateName.SetActive(true);
        }

        private void OnSaveClick(string value)
        {
            Transform entry = templateInstance.AddEntry(GetTemplateNameInput().text, _deliveryShop);

            foreach (ListingEntry listingEntry in _deliveryShop.listingEntries)
            {
                if (listingEntry.QuantityInput.text != "0")
                {
                    string content = listingEntry.QuantityInput.text + "x " + listingEntry.ItemNameLabel.text;
                    templateInstance.AddComponent(listingEntry, content, entry);
                }
            }

            templateName.SetActive(false);
            GetTemplateNameInput().text = "";
        }

        private bool HasComponent()
        {
            foreach (ListingEntry listingEntry in _deliveryShop.listingEntries)
            {
                if (listingEntry.QuantityInput.text != "0")
                {
                    return true;
                }
            }
            return false;
        }

        private Transform GetPanel(DeliveryShop deliveryShop)
        {
            int childCount = deliveryShop.ContentsContainer.childCount;
            for (int index = 0; index < childCount; index++)
            {
                GameObject go = deliveryShop.ContentsContainer.GetChild(index).gameObject;
                if (go.name == "Panel")
                {
                    return go.transform;
                }
            }

            return null;
        }
    }
}