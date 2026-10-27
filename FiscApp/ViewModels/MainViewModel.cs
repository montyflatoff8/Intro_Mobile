using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FiscApp.Events;
using FiscApp.Models;
using FiscApp.Models.Api;
using FiscApp.Services;
using FiscApp.Services.Api;
using FiscApp.Services.Interfaces;

namespace FiscApp.ViewModels;

/// <summary>
/// Backs MainPage (the Transaction List page). Owns every form field the "add/edit transaction"
/// UI needs as a bindable property, and every action as a Command — MainPage.xaml.cs itself now
/// contains nothing but InitializeComponent() and wiring the two alert/action-sheet events up to
/// the real DisplayAlert/DisplayActionSheet calls, since a ViewModel shouldn't call those directly.
/// </summary>
public partial class MainViewModel : BaseViewModel
{
    private readonly IFinanceDataStore store;
    private readonly IAuthService auth;
    private readonly ITransactionApiClient api;

    // Text of the toolbar button. Refreshed every time the page appears, because logging in
    // happens on another page and we come back here afterwards.
    [ObservableProperty]
    private string loginButtonText = "Log in";

    // Problem loading the list (e.g. "Can't reach the API"). Empty = hidden.
    [ObservableProperty]
    private string statusMessage = string.Empty;

    // Bound to the RefreshView's spinner (pull down on the list to reload).
    [ObservableProperty]
    private bool isRefreshing;

    // Validation messages from the API (400), shown under the matching form field.
    // Empty = hidden (MainPage uses an IsStringNotNullOrEmpty converter for IsVisible).
    [ObservableProperty]
    private string descriptionError = string.Empty;

    [ObservableProperty]
    private string amountError = string.Empty;

    [ObservableProperty]
    private string notesError = string.Empty;

    // Errors that don't belong to one of the fields above (date, type, category).
    [ObservableProperty]
    private string formError = string.Empty;

    // [ObservableProperty] (from CommunityToolkit.Mvvm) generates a public "Description" property
    // with change notification from this private field — equivalent to writing out a full
    // get/set/OnPropertyChanged block by hand, just without the boilerplate.
    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string amountText = string.Empty;

    [ObservableProperty]
    private string? notes;

    [ObservableProperty]
    private DateTime date = DateTime.Today;

    [ObservableProperty]
    private TransactionType? selectedType;

    [ObservableProperty]
    private BudgetCategory? selectedCategory;

    // Runs automatically when SelectedType changes (generated partial method). Category only
    // applies to expenses, so switching to Income clears any selected category. The category
    // picker itself is always visible in MainPage.xaml; IsExpenseSelected is available for
    // binding but isn't currently used there.
    partial void OnSelectedTypeChanged(TransactionType? value)
    {
        OnPropertyChanged(nameof(IsExpenseSelected));
        if (value != TransactionType.Expense)
        {
            SelectedCategory = null;
        }
    }

    public bool IsExpenseSelected => SelectedType == TransactionType.Expense;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(SaveButtonText))]
    private Transaction? editingTransaction;

    // Non-null EditingTransaction = the form is in "edit" mode rather than "add" mode.
    public bool IsEditing => EditingTransaction is not null;
    public string SaveButtonText => IsEditing ? "Save Changes" : "Add Transaction";

    // Pass-throughs to the shared store so the page binds to the same live collections
    // the Budget and Reports ViewModels watch.
    public ObservableCollection<Transaction> Transactions => store.Transactions;
    public ObservableCollection<FinancialGoal> Goals => store.Goals;
    public ObservableCollection<BudgetCategory> Categories => store.Categories;
    public TransactionType[] TransactionTypes { get; } = Enum.GetValues<TransactionType>();

    public MainViewModel(IFinanceDataStore store, IAuthService auth, ITransactionApiClient api)
    {
        this.store = store;
        this.auth = auth;
        this.api = api;

        // CUSTOM EVENT SUBSCRIPTIONS (see Events/TransactionEvents.cs).
        // The gestures on MainPage never change data directly — they only RAISE these events on
        // the shared store. These two subscriptions are where the ViewModel actually reacts:
        //   TransactionDeletedEvent       → remove the transaction from the model
        //   TransactionEditRequestedEvent → open the edit form for that transaction
        store.TransactionDeleted += OnTransactionDeleted;
        store.TransactionEditRequested += OnTransactionEditRequested;
    }

    // Called by MainPage.OnAppearing: first launch, and every time we return (e.g. from Login).
    public async Task OnAppearingAsync()
    {
        LoginButtonText = await auth.IsLoggedInAsync() ? "Log out" : "Log in";
        await LoadTransactionsAsync();
    }

    // Pull-to-refresh on the list.
    [RelayCommand]
    private async Task Refresh()
    {
        await LoadTransactionsAsync();
        IsRefreshing = false;
    }

    /// <summary>
    /// GET api/transactions (public - works logged out) and replace the local list with it.
    /// The API is the source of truth; the store is just what's on screen right now.
    /// </summary>
    private async Task LoadTransactionsAsync()
    {
        try
        {
            var result = await api.GetAllAsync();
            if (!result.IsSuccess)
            {
                StatusMessage = result.ErrorMessage;
                return;
            }

            // API order is already newest first. ResolveCategory turns "Groceries" into the
            // local BudgetCategory object, so the Budget and Reports pages keep working.
            store.ReplaceTransactions(result.Value!.Select(dto => dto.ToModel(store.ResolveCategory)));
            StatusMessage = string.Empty;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            // API stopped or still starting. Keep whatever is on screen; pull down to retry.
            StatusMessage = ApiUnreachableMessage + " Pull down to retry.";
        }
    }

    // Toolbar button: logged out -> open the Login page; logged in -> forget the token.
    [RelayCommand]
    private async Task LoginLogout()
    {
        if (await auth.IsLoggedInAsync())
        {
            await auth.LogoutAsync();
            LoginButtonText = "Log in";
        }
        else
        {
            await Shell.Current.GoToAsync(Routes.Login);
        }
    }

    // Handler for TransactionDeletedEvent: this is the ViewModel "being informed" that the user
    // swiped to delete. It now asks the API to delete first, and only removes the row locally
    // once the API agrees. ("async void" is only acceptable here because it's an event handler.)
    private async void OnTransactionDeleted(object? sender, TransactionDeletedEvent e)
    {
        try
        {
            var result = await api.DeleteAsync(e.Transaction.Id);

            if (result.IsUnauthorized)
            {
                // Not logged in (or token expired): nothing was deleted. Go log in, then try again.
                await SendToLoginAsync(auth);
                return;
            }

            if (!result.IsSuccess && !result.IsNotFound)
            {
                await ShowAlertAsync("Couldn't Delete", result.ErrorMessage);
                return;
            }

            // 204 = deleted. 404 = already gone (e.g. deleted in Postman). Either way, remove it here.
            store.RemoveTransaction(e.Transaction);

            // If the deleted transaction was open in the edit form, clear the form so the user
            // can't "save changes" to something that no longer exists.
            if (EditingTransaction == e.Transaction)
            {
                ResetForm();
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            await ShowAlertAsync("Couldn't Delete", ApiUnreachableMessage);
        }
    }

    // Handler for TransactionEditRequestedEvent: loads the transaction into the form.
    private void OnTransactionEditRequested(object? sender, TransactionEditRequestedEvent e)
        => PopulateFormForEdit(e.Transaction);

    // Bound to the Add/Save button. Checks the form locally first (instant feedback), then sends
    // it to the API: POST for a new transaction, PUT when editing. The list only changes once the
    // API says it saved. AsyncRelayCommand disables the button while this runs (no double-saves).
    [RelayCommand]
    private async Task SaveTransaction()
    {
        ClearFieldErrors();

        if (string.IsNullOrWhiteSpace(Description))
        {
            await ShowAlertAsync("Missing Description", "Please describe what this transaction was for.");
            return;
        }

        if (!decimal.TryParse(AmountText, out var amount) || amount <= 0)
        {
            await ShowAlertAsync("Invalid Amount", "Please enter an amount greater than zero.");
            return;
        }

        // Requirement: an unselected type must NOT silently become an Expense — it has to be
        // an explicit choice.
        if (SelectedType is null)
        {
            await ShowAlertAsync("Missing Type", "Please choose whether this is an Income or an Expense.");
            return;
        }

        // Requirement: an expense can't be saved without a category.
        if (SelectedType == TransactionType.Expense && SelectedCategory is null)
        {
            await ShowAlertAsync("Missing Category", "Please choose a budget category for this expense.");
            return;
        }

        var trimmedNotes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
        var category = SelectedType == TransactionType.Expense ? SelectedCategory : null;

        var transaction = new Transaction
        {
            Id = EditingTransaction?.Id ?? 0,
            Description = Description.Trim(),
            Amount = amount,
            Type = SelectedType.Value,
            Date = Date,
            Notes = trimmedNotes,
            Category = category
        };

        try
        {
            ApiResult result;
            if (EditingTransaction is not null)
            {
                // PUT api/transactions/{id} -> 204 No Content.
                result = await api.UpdateAsync(EditingTransaction.Id, transaction.ToWriteDto());
                if (result.IsSuccess)
                {
                    var index = Transactions.IndexOf(EditingTransaction);
                    if (index >= 0)
                    {
                        Transactions[index] = transaction;
                    }
                }
            }
            else
            {
                // POST api/transactions -> 201 with the saved transaction (now with its real Id).
                var created = await api.CreateAsync(transaction.ToWriteDto());
                result = created;
                if (created.IsSuccess)
                {
                    Transactions.Insert(0, created.Value!.ToModel(store.ResolveCategory));
                }
            }

            if (result.IsSuccess)
            {
                ResetForm();
                return;
            }

            await HandleSaveFailureAsync(result);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            // Form is NOT cleared, so the user can retry once the API is back.
            await ShowAlertAsync("Couldn't Save", ApiUnreachableMessage);
        }
    }

    /// <summary>
    /// One place that decides what each failed save means for the user. The form keeps its
    /// values in every case, so nothing they typed is lost.
    /// </summary>
    private async Task HandleSaveFailureAsync(ApiResult result)
    {
        if (result.IsUnauthorized)
        {
            // Logged out or token expired. This page stays on the navigation stack underneath
            // Login, so after logging in the form is still filled in - just tap Save again.
            await SendToLoginAsync(auth);
        }
        else if (result.IsValidationError)
        {
            ShowFieldErrors(result.Problem?.Errors);
        }
        else if (result.IsConflict)
        {
            await ShowAlertAsync("Duplicate Transaction", result.ErrorMessage);
        }
        else if (result.IsNotFound)
        {
            // Edited something that was deleted elsewhere (e.g. in Postman): reload the real list.
            await ShowAlertAsync("Not Found", result.ErrorMessage);
            ResetForm();
            await LoadTransactionsAsync();
        }
        else
        {
            await ShowAlertAsync("Couldn't Save", result.ErrorMessage);
        }
    }

    /// <summary>
    /// 400 from the API: put each message under its field. The keys are the API's JSON names
    /// ("description", "amount", "notes") - matched ignoring case, just in case. Anything else
    /// (date, type, category, or a body-level error) goes in the general error area.
    /// </summary>
    private void ShowFieldErrors(Dictionary<string, string[]>? errors)
    {
        if (errors is null || errors.Count == 0)
        {
            FormError = "Some of the values aren't valid.";
            return;
        }

        var other = new List<string>();
        foreach (var (key, messages) in errors)
        {
            var text = string.Join("\n", messages);
            switch (key.ToLowerInvariant())
            {
                case "description": DescriptionError = text; break;
                case "amount": AmountError = text; break;
                case "notes": NotesError = text; break;
                default: other.Add(text); break;
            }
        }

        FormError = string.Join("\n", other);
    }

    private void ClearFieldErrors()
    {
        DescriptionError = AmountError = NotesError = FormError = string.Empty;
    }

    // Bound to the "Edit" swipe action — populates the form immediately, no confirmation needed
    // since swiping and tapping "Edit" is already an explicit choice.
    [RelayCommand]
    private void EditTransaction(Transaction transaction) => PopulateFormForEdit(transaction);

    // GESTURE: swipe left → "Delete". Bound to the Delete SwipeItem in MainPage.xaml.
    // Raises TransactionDeletedEvent instead of deleting directly; OnTransactionDeleted (above)
    // is what actually removes the transaction when the event arrives.
    [RelayCommand]
    private void DeleteTransaction(Transaction transaction) => store.RaiseTransactionDeleted(transaction);

    /// <summary>
    /// GESTURE: long press on a transaction row (MainPage.xaml's toolkit:TouchBehavior).
    /// Pops up a menu with three options, so extra actions are available without cluttering
    /// each row with buttons:
    ///   • "View Details" – shows every field of the transaction (the "detailed view").
    ///   • "Edit"         – raises TransactionEditRequestedEvent (the "quick edit option").
    ///   • "Delete"       – raises TransactionDeletedEvent (same path as swipe-to-delete).
    /// Edit and Delete go through the store's custom events rather than calling
    /// PopulateFormForEdit / RemoveTransaction directly, so the gesture is decoupled from what
    /// "edit" or "delete" actually does.
    /// </summary>
    [RelayCommand]
    private async Task LongPressTransaction(Transaction transaction)
    {
        const string viewDetails = "View Details";
        const string edit = "Edit";
        const string delete = "Delete";

        var choice = await ShowActionSheetAsync(
            $"{transaction.Description} ({transaction.DisplayAmount})",
            "Cancel",
            delete,
            new[] { viewDetails, edit });

        switch (choice)
        {
            case viewDetails:
                await ShowAlertAsync("Transaction Details", BuildDetailsText(transaction), "Close");
                break;
            case edit:
                store.RaiseTransactionEditRequested(transaction);
                break;
            case delete:
                store.RaiseTransactionDeleted(transaction);
                break;
        }
    }

    // Builds the multi-line body of the "View Details" pop-up.
    private static string BuildDetailsText(Transaction t) =>
        $"Description: {t.Description}\n" +
        $"Amount: {t.DisplayAmount}\n" +
        $"Type: {t.Type}\n" +
        $"Category: {t.CategoryDisplay}\n" +
        $"Date: {t.Date:dddd, MMMM d, yyyy}\n" +
        $"Notes: {(string.IsNullOrWhiteSpace(t.Notes) ? "(none)" : t.Notes)}";

    // Copies a transaction's values into the form and switches it into edit mode.
    private void PopulateFormForEdit(Transaction transaction)
    {
        ClearFieldErrors();
        EditingTransaction = transaction;
        Description = transaction.Description;
        AmountText = transaction.Amount.ToString();
        Notes = transaction.Notes;
        Date = transaction.Date;
        SelectedType = transaction.Type;
        SelectedCategory = transaction.Category;
    }

    // Clears every field and leaves edit mode (button text goes back to "Add Transaction").
    private void ResetForm()
    {
        ClearFieldErrors();
        EditingTransaction = null;
        Description = string.Empty;
        AmountText = string.Empty;
        Notes = null;
        Date = DateTime.Today;
        SelectedType = null;
        SelectedCategory = null;
    }
}
