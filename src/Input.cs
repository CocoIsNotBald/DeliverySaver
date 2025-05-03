using Il2CppScheduleOne;
using Il2CppScheduleOne.UI.Phone.Delivery;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DeliverySaver
{
    internal class InputUI
    {
        private GameObject _parent;
        private InputField _inputField;

        public Action<string> OnEndEdit = (_) => { };
        public Action<string> OnValueChanged = (_) => { };

        public Func<string, bool> OnSubmit = (_) => true;
        public bool clearAfterSubmit = true;

        public InputField InputField => _inputField;
        public GameObject gameObject => _parent;

        public InputUI(InputField field)
        {
            InitInput(field);
        }

        public InputUI(string assetName, string inputName)
        {
            GameObject inputGO = AssetsManager.Instance.Instantiate(assetName);
            inputGO.transform.SetParent(DeliveryApp.Instance.appContainer.transform, false);
            inputGO.SetActive(false);

            _parent = inputGO;

            InitInput(_parent.transform.Find(inputName).GetComponent<InputField>());
        }

        private void InitInput(InputField field)
        {
            _inputField = field;

            Action<string> onValueChanged = (value) =>
            {
                OnValueChanged(value);
            };

            Action<string> onEndEdit = (value) =>
            {
                OnEndEdit(value);

                if (_inputField.wasCanceled)
                {
                    Deactivate();
                    InputField.text = value;
                }
            };


            Action<string> onSubmit = (value) =>
            {
                if (OnSubmit(value))
                {
                    Deactivate();
                }
            };

            _inputField.onEndEdit.AddListener(onEndEdit);
            _inputField.onSubmit.AddListener(onSubmit);
            _inputField.onValueChanged.AddListener(onValueChanged);
            _inputField.onValueChange.AddListener(OnValueChanged);
        }

        public void Deactivate()
        {
            if (_parent != null)
                _parent.SetActive(false);
            GameInput.Instance.PlayerInput.ActivateInput();
            if(clearAfterSubmit)
                _inputField.text = "";
        }

        public Action ActivateAsAction()
        {
            Action callback = () => Activate();

            return callback;
        }

        public void Activate()
        {
            if(_parent != null)
                _parent.SetActive(true);
            GameInput.Instance.PlayerInput.DeactivateInput();
            _inputField.Select();
        }
    }
}
