using Il2CppFluffyUnderware.Curvy.Generator;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.UI.Phone.Delivery;
using MelonLoader;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(DeliverySaver.Core), "DeliverySaver", "1.0.2", "Coco", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

// Phone dimensions 1200x800
// Delivery app notification position : 0 200 0 size : 500x75
namespace DeliverySaver
{
    public class Core : MelonMod
    {
        private string _scene;
        private bool _loaded = false;
        private DeliveryShop _deliveryShop = null;
        private bool _errorMode = false;

        InputUI templateSeedInput = null;
        InputUI templateNameInput = null;
        public override void OnInitializeMelon()
        {
            try
            {
                if (!Directory.Exists(ModConfig.ModRootFile))
                {
                    Directory.CreateDirectory(ModConfig.ModRootFile);
                }

                AssetsManager.Instance.resourcesPrefix = "DeliverySaver.assets.";

                TemplateManager.Instance.Init();

                AssetsManager.Instance.LoadFileFromResources("Signature", "signature.txt");
                AssetsManager.Instance.LoadAssetBundleFromResources("SaveButton", "ui.savebutton");
                AssetsManager.Instance.LoadAssetBundleFromResources("TemplateName", "ui.templatename");
                AssetsManager.Instance.LoadAssetBundleFromResources("Notification", "ui.notification");
                AssetsManager.Instance.LoadAssetBundleFromResources("TemplateSeed", "ui.templateseed");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("There is a issue pls report it to the developer");
                LoggerInstance.Error(ex);
            }
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
            try
            {
                TemplateManager.Instance.Save();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("There is a issue pls report it to the developer");
                LoggerInstance.Error(ex);
            }
        }

        public override void OnUpdate()
        {
            if (_scene == "Main" && !_errorMode && !_loaded)
            {
                try
                {
                    if (
                        DeliveryApp.Instance &&
                        GameManager.Instance &&
                        MoneyManager.Instance &&
                        GameManager.Instance.seed != 0 &&
                        DeliveryApp.Instance.appIconButton != null &&
                        SaveManager.Instance
                    )
                    {
                        InitSaveListener();
                        IngredientRegister.Instance.Synchronize();
                        TemplateManager.Instance.CreateTemplateGameObject();
                        InitTemplateName();
                        InitTemplateSeed();
                        AddAppSaveButton(DeliveryApp.Instance);
                        AddNotificationPanel(DeliveryApp.Instance);
                        TemplateManager.Instance.Load();
                        _loaded = true;
                    }
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error("There is a issue pls report it to the developer");
                    LoggerInstance.Error(ex);
                    _errorMode = true;
                }
            }
        }

        private void InitSaveListener()
        {
            Action callback = () => TemplateManager.Instance.Save();
            SaveManager.Instance.onSaveStart.AddListener(callback);
        }

        private void InitTemplateSeed()
        {
            GameObject templateSeed = AssetsManager.Instance.Instantiate("TemplateSeed");
            templateSeed.transform.SetParent(DeliveryApp.Instance.appContainer.transform, false);
            templateSeed.SetActive(false);

            templateSeedInput = new InputUI(templateSeed, "InputName");
            templateSeedInput.OnSubmit += OnSeedEnter;

            // Bind the button close (X) to deactivate the input and relieve player input
            Action close = () => templateSeedInput.Deactivate();
            templateSeed.transform.Find("Close").GetComponent<Button>().onClick.AddListener(close);
   
            // When the import button is clicked, activate the prompt for entering a seed
            Transform importSeed = TemplateManager.Instance.template.gameObject.transform.Find("Mask/Content/ImportSeed");
            importSeed.GetComponent<Button>().onClick.AddListener(templateSeedInput.ActivateAsAction());

            // When the V button is click, submit the input
            Action action = () => templateSeedInput.InputField.SendOnSubmit();
            templateSeed.transform.Find("Validate").GetComponent<Button>().onClick.AddListener(action);
        }

        private bool OnSeedEnter(string seed)
        {
            try
            {
                List<EntryData> entry = Seeder.Instance.Decode<List<EntryData>>(seed);

                if (entry == null)
                {
                    throw new EntryIsEmpty();
                }

                bool showEntryMessageData = entry.Count == 1 ? true : false;

                foreach (EntryData entryData in entry)
                {
                    TemplateManager.Instance.AddEntryData(entryData, showEntryMessageData);
                }

                TemplateManager.Instance.template.Open();
                return true;
            }
            catch (EntryIsEmpty)
            {
                Notification.Instance.Show("Cannot add a empty entry");
                return false;
            }
            catch (EntryAlreadyExistsException)
            {
                Notification.Instance.Show("Entry already registered");
                return false;
            }
            catch (Exception)
            {
                Notification.Instance.Show("Invalid seed");
                return false;
            }
        }

        private void InitTemplateName()
        {
            GameObject templateName = AssetsManager.Instance.Instantiate("TemplateName");
            templateName.transform.SetParent(DeliveryApp.Instance.appContainer.transform, false);
            templateName.SetActive(false);

            templateNameInput = new InputUI(templateName, "InputName");
            templateNameInput.OnSubmit += OnEntryNameValidated;

            Action close = () => templateNameInput.Deactivate();
            templateName.transform.Find("Close").GetComponent<Button>().onClick.AddListener(close);

            // When the V button is click, submit the input
            Action action = () => templateNameInput.InputField.SendOnSubmit();
            templateName.transform.Find("Validate").GetComponent<Button>().onClick.AddListener(action);
        }

        private void AddNotificationPanel(DeliveryApp app)
        {
            GameObject go = Notification.Instance.Instantiate();

            go.transform.SetParent(app.appContainer.transform, false);
            go.transform.localPosition = new Vector3(0, 200, 0);
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

                    Action setShop = () => OnShopSelected(deliveryShop);
                    saveButton.onClick.AddListener(setShop);
                }
                else
                {
                    LoggerInstance.Warning($"Cannot get panel for deliveryShop '{deliveryShop.name}'");
                }
            }
        }

        private void OnShopSelected(DeliveryShop deliveryShop)
        {
            if (!HasComponent(deliveryShop)) return;

            _deliveryShop = deliveryShop;
            templateNameInput.Activate();
        }

        private bool OnEntryNameValidated(string name)
        {
            if (string.IsNullOrEmpty(templateNameInput.InputField.text))
            {
                Notification.Instance.Show("Name cannot be empty");
                return false;
            }

            if (TemplateManager.Instance.IsEntryRegister(name))
            {
                Notification.Instance.Show("Name already taken");
                return false;
            }

            TemplateManager.Instance.AddEntry(templateNameInput.InputField.text, _deliveryShop);
            TemplateManager.Instance.template.Open();

            return true;
        }

        private bool HasComponent(DeliveryShop shop)
        {
            foreach (ListingEntry listingEntry in shop.listingEntries)
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