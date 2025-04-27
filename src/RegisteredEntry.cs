using Il2CppScheduleOne.UI.Phone.Delivery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using MelonLoader;

namespace DeliverySaver.src
{
    internal class RegisteredEntry
    {
        private GameObject _registeredEntry;
        private Text _registeredEntryText;

        private GameObject _registeredAllEntry;
        private Text _registeredAllEntryText;

        private EntryData _entryData;
        private List<EntryData> _entriesData = new List<EntryData>();

        public RegisteredEntry(string registeredEntryAssetName, string registeredMultipleAssetName)
        {
            // Setup for a single registered entry
            _registeredEntry = AssetsManager.Instance.Instantiate(registeredEntryAssetName);
            _registeredEntry.transform.SetParent(DeliveryApp.Instance.appContainer.transform, false);
            _registeredEntry.SetActive(false);

            _registeredEntryText = _registeredEntry.transform.Find("Message").GetComponent<Text>();
            Button overwrite = _registeredEntry.transform.Find("Buttons/Overwrite").GetComponent<Button>();
            Button cancel = _registeredEntry.transform.Find("Buttons/Cancel").GetComponent<Button>();

            overwrite.onClick.AddListener((Action)OnOverwrite);
            cancel.onClick.AddListener((Action)OnCancel);

            // Setup for multiple registered entry
            GameObject registeredAllEntryGO = AssetsManager.Instance.Instantiate(registeredMultipleAssetName);
            registeredAllEntryGO.transform.SetParent(DeliveryApp.Instance.appContainer.transform, false);
            registeredAllEntryGO.SetActive(false);

            _registeredAllEntry = registeredAllEntryGO;
            _registeredAllEntryText = _registeredAllEntry.transform.Find("Message").GetComponent<Text>();

            Button cancelMultiple = _registeredAllEntry.transform.Find("Buttons/Cancel").GetComponent<Button>();
            cancelMultiple.onClick.AddListener((Action)OnCancelEntries);

            Button cancelAllMultiple = _registeredAllEntry.transform.Find("Buttons/CancelAll").GetComponent<Button>();
            cancelAllMultiple.onClick.AddListener((Action)OnCancelAllEntries);

            Button overwriteMultiple = _registeredAllEntry.transform.Find("Buttons/Overwrite").GetComponent<Button>();
            overwriteMultiple.onClick.AddListener((Action)OnOverwriteMultiple);

            Button overwriteAllMultiple = _registeredAllEntry.transform.Find("Buttons/OverwriteAll").GetComponent<Button>();
            overwriteAllMultiple.onClick.AddListener((Action)OnOverwriteAllMultiple);
        }

        private void OnOverwriteAllMultiple()
        {
            foreach(EntryData entryData in _entriesData)
            {
                TemplateManager.Instance.SetEntryData(entryData);
            }

            TemplateManager.Instance.template.Open();
            _entriesData.Clear();
            _registeredAllEntry.SetActive(false);
        }

        private void OnOverwriteMultiple()
        {
            TemplateManager.Instance.SetEntryData(_entriesData[0]);
            _entriesData.RemoveAt(0);

            if (_entriesData.Count == 0)
            {
                TemplateManager.Instance.template.Open();
                _registeredAllEntry.SetActive(false);
                return;
            }

            _registeredAllEntryText.text = CreateMessage(_entriesData[0]);
        }

        private void OnCancelAllEntries()
        {
            _registeredAllEntry.SetActive(false);
            _entriesData.Clear();
        }

        private void OnCancelEntries()
        {
            _entriesData.RemoveAt(0);

            if (_entriesData.Count == 0)
            {
                TemplateManager.Instance.template.Open();
                _registeredAllEntry.SetActive(false);
                return;
            }

            _registeredAllEntryText.text = CreateMessage(_entriesData[0]);
        }

        private string CreateMessage(EntryData entry)
        {
            return $"A entry with name {entry.title} is already registered.";
        }

        private void OnCancel()
        {
            _entryData = null;
            _registeredEntry.SetActive(false);
        }

        private void OnOverwrite()
        {
            if (_entryData != null)
            {
                TemplateManager.Instance.SetEntryData(_entryData);
                TemplateManager.Instance.template.Open();
                _entryData = null;
                _registeredEntry.SetActive(false);
            }
        }

        public void Show(EntryData entry)
        {
            _entryData = entry;
            _registeredEntryText.text = CreateMessage(entry);
            _registeredEntry.SetActive(true);
        }

        public void Show(IEnumerable<EntryData> entries)
        {
            _entriesData = entries.ToList();
            _registeredAllEntryText.text = CreateMessage(_entriesData[0]);
            _registeredAllEntry.SetActive(true);
        }
    }
}
