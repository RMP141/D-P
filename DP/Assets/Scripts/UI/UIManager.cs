using System;
using System.Collections.Generic;
using ConvoyManager.Core;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace ConvoyManager.UI
{
    public class UIManager : IStartable, IDisposable, IUIManager
    {
        private UIDocument _uiDocument;
        private readonly EventBus _eventBus;
        private readonly Dictionary<string, VisualElement> _screens = new Dictionary<string, VisualElement>();
        private VisualElement _currentScreen;
        private VisualElement _screenContainer;
        private CompositeDisposable _disposables = new CompositeDisposable();

        public UIManager(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Start()
        {
            _uiDocument = UnityEngine.Object.FindFirstObjectByType<UIDocument>();
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            root.style.flexGrow = 1;
            root.style.flexDirection = FlexDirection.Column;

            _screenContainer = new VisualElement();
            _screenContainer.style.flexGrow = 1;
            _screenContainer.style.flexDirection = FlexDirection.Column;
            root.Add(_screenContainer);

        }

        public void RegisterScreen(string name, VisualElement screen)
        {
            if (_uiDocument == null) return;
            if (!_screens.ContainsKey(name))
            {
                screen.style.display = DisplayStyle.None;
                screen.style.flexGrow = 1;
                screen.style.flexShrink = 0;
                _screenContainer.Add(screen);
                _screens[name] = screen;
            }
        }

        public void ShowScreen(string screenName)
        {
            if (_currentScreen != null)
                _currentScreen.style.display = DisplayStyle.None;

            if (_screens.TryGetValue(screenName, out var screen))
            {
                screen.style.display = DisplayStyle.Flex;
                _currentScreen = screen;
            }

            _eventBus.Publish(new ShowScreenEvent(screenName));
        }

        public void HideCurrentScreen()
        {
            if (_currentScreen != null)
            {
                _currentScreen.style.display = DisplayStyle.None;
                _currentScreen = null;
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }

}