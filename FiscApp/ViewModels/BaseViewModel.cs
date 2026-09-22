using CommunityToolkit.Mvvm.ComponentModel;

namespace FiscApp.ViewModels;

/// <summary>
/// Shared base for every ViewModel in the app. Inherits from CommunityToolkit.Mvvm's
/// ObservableObject, which already implements INotifyPropertyChanged and provides the
/// [ObservableProperty] source generator used throughout the ViewModels below (it turns a
/// private field into a full bindable property with change notification, without writing out
/// a backing-field/getter/setter/OnPropertyChanged block by hand for every single property).
///
/// A ViewModel should never directly call Page.DisplayAlert — that would tie application logic
/// back to a specific UI control, exactly what MVVM is meant to avoid. Instead, a ViewModel
/// raises AlertRequested and the Page (which does know how to show a native alert) subscribes
/// to it in its constructor. This keeps validation/error messages in the ViewModel while the
/// actual alert dialog stays a "view concern".
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    public event Func<string, string, string, Task>? AlertRequested;

    protected Task ShowAlertAsync(string title, string message, string cancel = "OK")
        => AlertRequested?.Invoke(title, message, cancel) ?? Task.CompletedTask;

    // Same idea as AlertRequested, but for Page.DisplayActionSheet — used by the long-press
    // gesture's "Edit / Delete" menu. The Page subscribes this to its own DisplayActionSheet call.
    public event Func<string, string, string?, string[], Task<string>>? ActionSheetRequested;

    protected Task<string> ShowActionSheetAsync(string title, string cancel, string? destruction, string[] buttons)
        => ActionSheetRequested?.Invoke(title, cancel, destruction, buttons) ?? Task.FromResult(cancel);
}
