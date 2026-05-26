namespace ConvoyManager.UI
{
    public interface IUIManager
    {
        void ShowScreen(string screenName);
        void HideCurrentScreen();
        void RegisterScreen(string name, UnityEngine.UIElements.VisualElement screen);
    }
}