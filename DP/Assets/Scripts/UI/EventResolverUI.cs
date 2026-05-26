using ConvoyManager.Core;
using ConvoyManager.Events;
using ConvoyManager.Data;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace ConvoyManager.UI
{
    public class EventResolverUI
    {
        private readonly EventResolver _eventResolver;
        private readonly EventBus _eventBus;
        private readonly IUIManager _uiManager;

        private VisualElement _root;
        private Label _titleLabel;
        private Label _descLabel;
        private VisualElement _buttonsContainer;
        private CompositeDisposable _disposables = new CompositeDisposable();

        public EventResolverUI(EventResolver eventResolver, EventBus eventBus, IUIManager uiManager)
        {
            _eventResolver = eventResolver;
            _eventBus = eventBus;
            _uiManager = uiManager;
        }

        public void Initialize(VisualElement rootVisualElement)
        {
            var visualTree = Resources.Load<VisualTreeAsset>("UI/EventResolverUI");
            _root = visualTree.CloneTree();
            _root.style.display = DisplayStyle.None;
            _uiManager.RegisterScreen("EventResolver", _root);

            _titleLabel = _root.Q<Label>("title");
            _descLabel = _root.Q<Label>("description");
            _buttonsContainer = _root.Q<VisualElement>("buttons");

            _eventBus.Subscribe<EventTriggeredMessage>().Subscribe(ShowEvent).AddTo(_disposables);
        }

        private void ShowEvent(EventTriggeredMessage msg)
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
                    _uiManager.HideCurrentScreen();
                });
                btn.text = option.ButtonText;
                _buttonsContainer.Add(btn);
            }

            _uiManager.ShowScreen("EventResolver");
        }
    }
}