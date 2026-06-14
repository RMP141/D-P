using System;
using UnityEngine.UIElements;

namespace ConvoyManager.UI
{
    public class ConfirmDialog
    {
        private VisualElement _root;
        private Label _messageLabel;
        private Action<bool> _callback;

        public void Initialize(VisualElement rootVisualElement)
        {
            var visualTree = UnityEngine.Resources.Load<VisualTreeAsset>("UI/ConfirmDialog");
            _root = visualTree.CloneTree();
            _root.style.position = Position.Absolute;
            _root.style.top = 0;
            _root.style.left = 0;
            _root.style.right = 0;
            _root.style.bottom = 0;
            _root.style.display = DisplayStyle.None;
            rootVisualElement.Add(_root);

            _messageLabel = _root.Q<Label>("message-label");
            var confirmBtn = _root.Q<Button>("confirm-btn");
            var cancelBtn = _root.Q<Button>("cancel-btn");

            confirmBtn.clicked += () => { _callback?.Invoke(true); Hide(); };
            cancelBtn.clicked += () => { _callback?.Invoke(false); Hide(); };
        }

        public void Show(string message, Action<bool> onResult)
        {
            _messageLabel.text = message;
            _callback = onResult;
            _root.style.display = DisplayStyle.Flex;
            var parent = _root.parent;
            if (parent != null)
            {
                _root.RemoveFromHierarchy();
                parent.Add(_root);
            }
        }

        public void Hide()
        {
            _root.style.display = DisplayStyle.None;
        }
    }
}
