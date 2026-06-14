using ConvoyManager.Core;
using ConvoyManager.Events;
using ConvoyManager.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace ConvoyManager.UI
{
    public class EventResolverUI
    {
        private readonly EventResolver _eventResolver;

        private VisualElement _root;
        private Label _titleLabel;
        private Label _descLabel;
        private VisualElement _buttonsContainer;

        public EventResolverUI(EventResolver eventResolver)
        {
            _eventResolver = eventResolver;
        }

        public void Initialize(VisualElement rootVisualElement)
        {
            var visualTree = Resources.Load<VisualTreeAsset>("UI/EventResolverUI");
            _root = visualTree.CloneTree();
            _root.style.position = Position.Absolute;
            _root.style.top = 40;
            _root.style.left = 0;
            _root.style.right = 0;
            _root.style.bottom = 0;
            _root.style.display = DisplayStyle.None;
            rootVisualElement.Add(_root);

            _titleLabel = _root.Q<Label>("title");
            _descLabel = _root.Q<Label>("description");
            _buttonsContainer = _root.Q<VisualElement>("buttons");
        }

        public void Show(EventTriggeredMessage msg)
        {
            _titleLabel.text = msg.EventData.Title;
            _descLabel.text = msg.EventData.Description;
            _buttonsContainer.Clear();

            for (int i = 0; i < msg.EventData.Options.Length; i++)
            {
                int index = i;
                var option = msg.EventData.Options[i];
                var btn = new Button(() =>
                {
                    _eventResolver.Resolve(msg.EventData, index);
                    Hide();
                });
                btn.text = option.ButtonText;
                _buttonsContainer.Add(btn);
            }

            BringToFront();
            _root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _root.style.display = DisplayStyle.None;
        }

        private void BringToFront()
        {
            var parent = _root.parent;
            if (parent != null)
            {
                _root.RemoveFromHierarchy();
                parent.Add(_root);
            }
        }
    }
}
