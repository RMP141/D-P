using System;
using ConvoyManager.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace ConvoyManager.UI
{
    public class SaveLoadSelectScreen
    {
        private readonly ISaveSystem _saveSystem;
        private VisualElement _overlay;
        private Label _titleLabel;
        private Button[] _slotButtons;
        private Button[] _deleteButtons;
        private string _mode;

        public event Action<int> OnSaveSlotSelected;
        public event Action<int> OnLoadSlotSelected;
        public event Action<int> OnDeleteSlotClicked;
        public event Action OnBackClicked;
        public event Action OnMainMenuClicked;

        public string Mode => _mode;

        public SaveLoadSelectScreen(ISaveSystem saveSystem)
        {
            _saveSystem = saveSystem;
        }

        public void Initialize(VisualElement root)
        {
            var visualTree = Resources.Load<VisualTreeAsset>("UI/SaveLoadSelect");
            _overlay = visualTree.CloneTree();
            _overlay.style.position = Position.Absolute;
            _overlay.style.top = 0;
            _overlay.style.left = 0;
            _overlay.style.right = 0;
            _overlay.style.bottom = 0;
            _overlay.style.display = DisplayStyle.None;
            root.Add(_overlay);

            _titleLabel = _overlay.Q<Label>("title-label");
            _slotButtons = new Button[3];
            _deleteButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                int slot = i + 1;
                _slotButtons[i] = _overlay.Q<Button>($"slot-{slot}-btn");
                if (_slotButtons[i] != null)
                {
                    _slotButtons[i].clicked += () => OnSlotClicked(slot);
                }
                _deleteButtons[i] = _overlay.Q<Button>($"delete-slot-{slot}-btn");
                if (_deleteButtons[i] != null)
                {
                    _deleteButtons[i].clicked += () => OnDeleteSlotClicked?.Invoke(slot);
                }
            }

            var backBtn = _overlay.Q<Button>("back-btn");
            if (backBtn != null)
                backBtn.clicked += () => OnBackClicked?.Invoke();

            var menuBtn = _overlay.Q<Button>("main-menu-btn");
            if (menuBtn != null)
                menuBtn.clicked += () => OnMainMenuClicked?.Invoke();
        }

        private void OnSlotClicked(int slot)
        {
            if (_mode == "save")
            {
                OnSaveSlotSelected?.Invoke(slot);
            }
            else if (_mode == "load")
            {
                if (_saveSystem.HasSlot(slot))
                    OnLoadSlotSelected?.Invoke(slot);
            }
        }

        public void ShowForSave()
        {
            _mode = "save";
            _titleLabel.text = "Save Game";
            RefreshSlots();
            BringToFront();
            _overlay.style.display = DisplayStyle.Flex;
        }

        public void ShowForLoad()
        {
            _mode = "load";
            _titleLabel.text = "Load Game";
            RefreshSlots();
            BringToFront();
            _overlay.style.display = DisplayStyle.Flex;
        }

        private void BringToFront()
        {
            var parent = _overlay.parent;
            if (parent != null)
            {
                _overlay.RemoveFromHierarchy();
                parent.Add(_overlay);
            }
        }

        public void Hide()
        {
            _overlay.style.display = DisplayStyle.None;
        }

        public void Refresh()
        {
            RefreshSlots();
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < 3; i++)
            {
                int slot = i + 1;
                var meta = _saveSystem.GetSlotMeta(slot);
                if (meta.IsEmpty)
                {
                    _slotButtons[i].text = $"Slot {slot}: Empty";
                    _deleteButtons[i].style.display = DisplayStyle.None;
                }
                else
                {
                    _slotButtons[i].text = $"Slot {slot}: {meta.Discovered}/{meta.Total} hexes\n{meta.Date}";
                    _deleteButtons[i].style.display = DisplayStyle.Flex;
                }
            }
        }
    }
}
