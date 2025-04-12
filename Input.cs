using Il2CppScheduleOne;
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

        public Action<string> OnEndEdit = (string _) => { };
        public Action<string> OnValueChanged = (string _) => { };
        public Func<string, bool> OnSubmit = (string _) => true;

        public InputField InputField => _inputField;

        public InputUI(InputField field)
        {
            _inputField = field;

            Action<string> onEndEdit = (string value) =>
            {
                OnEndEdit(value);

                if (_inputField.wasCanceled)
                {
                    Deactivate(false);
                }
            };


            Action<string> onSubmit = (string value) =>
            {
                if (OnSubmit(value))
                {
                    Deactivate();
                }
            };

            _inputField.onEndEdit.AddListener(onEndEdit);
            _inputField.onSubmit.AddListener(onSubmit);
        }

        public InputUI(GameObject parent, string inputName)
        {
            _parent = parent;
            _inputField = _parent.transform.Find(inputName).GetComponent<InputField>();

            Action<string> onEndEdit = (string value) =>
            {
                OnEndEdit(value);

                if(_inputField.wasCanceled)
                {
                    Deactivate(false);
                }
            };


            Action<string> onSubmit = (string value) =>
            {
                if(OnSubmit(value))
                {
                    Deactivate();
                }
            };

            _inputField.onEndEdit.AddListener(onEndEdit);
            _inputField.onSubmit.AddListener(onSubmit);
        }

        public void Deactivate(bool changeInputOnSubmit = true, string setInputOnSubmitTo = "")
        {
            if (_parent != null)
                _parent.SetActive(false);
            GameInput.Instance.PlayerInput.ActivateInput();
            if(changeInputOnSubmit)
                _inputField.text = setInputOnSubmitTo;
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
