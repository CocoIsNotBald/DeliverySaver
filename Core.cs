using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI.Phone.Delivery;
using MelonLoader;
using UnityEngine;
using UnityEngine.Playables;
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

// Delivery app notification position : 0 200 0 size : 500x75
namespace DeliverySaver
{
    public class Core : MelonMod
    {
        private string _scene;
        private bool _loaded = false;
        private bool _loadTemplate = false;
        private DeliveryShop _deliveryShop = null;
        private GameObject templateContainer = null;

        InputUI templateSeedInput = null;
        InputUI templateNameInput = null;
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
            AssetsManager.Instance.LoadResources("Notification", "ui.notification");
            AssetsManager.Instance.LoadResources("TemplateSeed", "ui.templateseed");
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
            

            if(_scene == "Main")
            {
                if (_loadTemplate)
                {
                    AddTemplatePanel(DeliveryApp.Instance);
                    AddNotificationPanel(DeliveryApp.Instance);
                    _loadTemplate = false;
                }
                    
                if (DeliveryApp.Instance && GameManager.Instance && !_loaded)
                {
                    templateContainer = new GameObject("TemplateContainer");
                    templateContainer.transform.SetParent(DeliveryApp.Instance.appContainer.transform, false);

                    InitTemplateName();
                    InitTemplateSeed();
                    AddAppSaveButton(DeliveryApp.Instance);
                    _loaded = true;
                    _loadTemplate = true;
                }
            }
        }

        private void InitTemplateSeed()
        {
            GameObject templateSeed = AssetsManager.Instance.Instantiate("TemplateSeed");
            templateSeed.transform.SetParent(DeliveryApp.Instance.appContainer.transform, false);
            templateSeed.SetActive(false);

            templateSeedInput = new InputUI(templateSeed, "InputName");

            templateSeedInput.OnSubmit += OnSeedEnter;
        }


        private bool OnSeedEnter(string seed)
        {
            try
            {
                byte[] base64encoded = System.Convert.FromBase64String(seed);
                string json = System.Text.Encoding.UTF8.GetString(base64encoded);

                List<EntryData> entry = Newtonsoft.Json.JsonConvert.DeserializeObject<List<EntryData>>(json);

                // List<EntryData> entry = Seeder.Instance.Decode<List<EntryData>>(seed);

                if (entry == null)
                {
                    throw new Exception();
                }

                TemplateManager.Instance.Populate(entry);
                return true;
            }
            catch (EntryAlreadyExistsException e)
            {
                Notification.Instance.Show(e.Message);
                return false;
            }
            catch (Exception e)
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
        }

        private void AddNotificationPanel(DeliveryApp app)
        {
            GameObject go = Notification.Instance.Instantiate();

            go.transform.SetParent(app.appContainer.transform, false);
            go.transform.localPosition = new Vector3(0, 200, 0);
        }

        private void AddTemplatePanel(DeliveryApp app)
        {
            if(TemplateManager.Instance.HasGame())
            {
                List<EntryData> data = TemplateManager.Instance.GetCurrentTemplateGame().entryData.Values.ToList();
                TemplateManager.Instance.Instantiate(data);
            }
            else
            {
                TemplateManager.Instance.Load(TemplateManager.Instance.GetTemplateForSave());
            }

            TemplateManager.Instance.template.gameObject.transform.SetParent(templateContainer.transform, false);
            TemplateManager.Instance.template.gameObject.transform.localPosition = new Vector3(393, -32, 0);

            Transform importSeed = TemplateManager.Instance.template.gameObject.transform.Find("Mask/Content/ImportSeed");

            importSeed.GetComponent<Button>().onClick.AddListener(templateSeedInput.ActivateAsAction());
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

                    Action setShop = () => { OnShopSelected(deliveryShop); };
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

            if (TemplateManager.Instance.GetTemplateGameData().IsEntryRegister(name))
            {
                Notification.Instance.Show("Name already taken");
                return false;
            }

            Entry entry = TemplateManager.Instance.template.AddEntry(templateNameInput.InputField.text, _deliveryShop);

            foreach (ListingEntry listingEntry in _deliveryShop.listingEntries)
            {
                if (listingEntry.QuantityInput.text != "0")
                {
                    entry.AddComponent(listingEntry);
                }
            }

            TemplateManager.Instance.GetTemplateGameData().RegisterEntry(entry);

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