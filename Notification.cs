using Il2CppScheduleOne.UI.Phone.Delivery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DeliverySaver
{
    internal class Notification
    {
        private static Notification _instance = null;
        private GameObject _notification;

        public static Notification Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Notification();
                }
                return _instance;
            }
        }

        private Notification()
        {

        }

        public GameObject Instantiate()
        {
            _notification = AssetsManager.Instance.Instantiate("Notification");
            return _notification;
        }

        public void Show(string message)
        {
            Text text = _notification.transform.Find("Message").GetComponent<Text>();

            text.text = message;
            _notification.GetComponent<Animator>().Play("NotificationAnim");
        }
    }
}
