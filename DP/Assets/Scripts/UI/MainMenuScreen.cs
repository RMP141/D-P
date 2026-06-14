using System;
using ConvoyManager.Core;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace ConvoyManager.UI
{
    public class MainMenuScreen
    {
        private readonly IUIManager _uiManager;
        private readonly EventBus _eventBus;
        private VisualElement _root;
        private Button _continueBtn;
        private CompositeDisposable _disposables = new CompositeDisposable();

        public event Action OnNewGameClicked;
        public event Action OnLoadGameClicked;
        public event Action OnContinueClicked;
        public event Action OnQuitClicked;

        public MainMenuScreen(IUIManager uiManager, EventBus eventBus)
        {
            _uiManager = uiManager;
            _eventBus = eventBus;
        }

        public void Initialize(VisualElement rootVisualElement)
        {
            var visualTree = Resources.Load<VisualTreeAsset>("UI/MainMenu");
            _root = visualTree.CloneTree();
            _root.style.flexGrow = 1;
            _root.style.justifyContent = Justify.Center;
            _root.style.alignItems = Align.Center;
            _uiManager.RegisterScreen("MainMenu", _root);

            _continueBtn = _root.Q<Button>("continue-btn");
            if (_continueBtn != null)
            {
                _continueBtn.clicked += () => OnContinueClicked?.Invoke();
                _continueBtn.style.display = DisplayStyle.None;
            }

            var newGameBtn = _root.Q<Button>("new-game-btn");
            if (newGameBtn != null)
                newGameBtn.clicked += () => OnNewGameClicked?.Invoke();

            var loadGameBtn = _root.Q<Button>("load-game-btn");
            if (loadGameBtn != null)
                loadGameBtn.clicked += () => OnLoadGameClicked?.Invoke();

            var quitBtn = _root.Q<Button>("quit-btn");
            if (quitBtn != null)
                quitBtn.clicked += () => OnQuitClicked?.Invoke();
        }

        public void SetContinueVisible(bool visible)
        {
            if (_continueBtn != null)
                _continueBtn.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
