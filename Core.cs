using Il2CppFluffyUnderware.Curvy.Generator;
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.UI.Phone.Delivery;
using Il2CppToolBuddy.ThirdParty.VectorGraphics;
using MelonLoader;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(DeliverySaver.Core), "DeliverySaver", "1.0.0", "Coco", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

// Phone dimensions 1200x800
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
        private string _scene;
        private bool _loaded = false;
        private bool _loadTemplate = false;
        private Template templateInstance = null;
        private GameObject templateName = null;
        private DeliveryShop _deliveryShop = null;

        public override void OnInitializeMelon()
        {
            if (!Directory.Exists(ModConfig.ModRootFile))
            {
                Directory.CreateDirectory(ModConfig.ModRootFile);
            }

            AssetsManager.Instance.resourcesPrefix = "DeliverySaver.assets.";

            TemplateManager.Instance.Init();

            AssetsManager.Instance.LoadResources("SaveButton", "ui.savebutton");
            AssetsManager.Instance.LoadResources("TemplateName", "ui.templatename");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            _scene = sceneName;

            if(_scene != "Main")
            {
                _loaded = false;
            }
        }

        public override void OnApplicationQuit()
        {
            TemplateManager.Instance.Save();
        }

        public override void OnUpdate()
        {
            if(Input.GetKeyDown(KeyCode.Escape) && templateName != null)
            {
                templateName.SetActive(false);
                GameInput.Instance.PlayerInput.ActivateInput();
            }

            if(_scene == "Main")
            {
                if (_loadTemplate)
                {
                    AddTemplatePanel(DeliveryApp.Instance);
                    _loadTemplate = false;
                }

                if (DeliveryApp.Instance && GameManager.Instance && !_loaded)
                {
                    InitTemplateName();
                    AddAppSaveButton(DeliveryApp.Instance);
                    _loaded = true;
                    _loadTemplate = true;
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
            if(TemplateManager.Instance.HasGame())
            {
                templateInstance = TemplateManager.Instance.GetCurrentTemplateGame();
            }
            else
            {
                templateInstance = TemplateManager.Instance.Load(TemplateManager.Instance.GetTemplateForSave());
            }

            templateInstance.gameObject.transform.SetParent(app.appContainer.transform, false);
            templateInstance.gameObject.transform.localPosition = new Vector3(393, -32, 0);
        }

        private void AddAppSaveButton(DeliveryApp app)
        {
            foreach (DeliveryShop deliveryShop in app.deliveryShops)
            {
                Transform panel = GetPanel(deliveryShop);

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

            GameInput.Instance.PlayerInput.DeactivateInput();
            templateName.SetActive(true);
        }

        private void OnSaveClick(string value)
        {
            if (TemplateManager.Instance.GetTemplateGameData().IsEntryRegister(value)) return;

            Entry entry = templateInstance.AddEntry(GetTemplateNameInput().text, _deliveryShop);

            foreach (ListingEntry listingEntry in _deliveryShop.listingEntries)
            {
                if (listingEntry.QuantityInput.text != "0")
                {
                    entry.AddComponent(listingEntry, entry);
                }
            }

            TemplateManager.Instance.GetTemplateGameData().RegisterEntry(entry);

            templateName.SetActive(false);
            GameInput.Instance.PlayerInput.ActivateInput();
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