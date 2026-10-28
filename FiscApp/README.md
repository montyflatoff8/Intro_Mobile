# FiscApp

A personal finance tracker built with .NET MAUI (MVVM, CommunityToolkit.Mvvm). Transactions are stored in SQL Server **through [FiscApi](https://github.com/montyflatoff8/FiscApi)**. The app never connects to the database itself.

## Run it

1. **Start FiscApi first** with the `https` profile. Follow its [README](https://github.com/montyflatoff8/FiscApi#run-it-from-a-fresh-clone) (Docker, `.env`, User Secrets, `dotnet run --launch-profile https`).
2. Open `FiscApp.slnx` in Visual Studio, select an **Android emulator**, and press **F5**.
   - Use a **Debug** build. Only Debug builds trust the API's local development certificate.
   - On the emulator the app calls `https://10.0.2.2:7008/` (the emulator's name for your PC). On Windows it calls `https://localhost:7008/`.
3. Browse transactions without logging in. Tap **Log in** (top right), or try to add, edit, or delete a transaction. Log in as `demo` with the demo password from FiscApi's User Secrets.

## How it talks to the API

| Piece | Job |
|---|---|
| `Services/Api/ApiSettings.cs` | Base address and the 15-second timeout |
| `Services/Api/DevCertificateHandler.cs` | Debug only: trusts the API's `CN=localhost` dev certificate on Android |
| `Services/Auth/AuthService.cs` | `POST api/auth/login`; stores the token |
| `Services/Auth/SecureTokenStore.cs` | Token + expiry in `SecureStorage` (encrypted by the OS) |
| `Services/Auth/AuthTokenHandler.cs` | Adds `Authorization: Bearer <token>` to every transactions request |
| `Services/Api/TransactionApiClient.cs` | GET / POST / PUT / DELETE `api/transactions`, returning `ApiResult` |
| `ViewModels/MainViewModel.cs` | Loads the list from the API, saves and deletes through it, shows validation errors under fields, and sends you to Log in on a 401 |
| `ViewModels/LoginViewModel.cs` + `Pages/LoginPage.xaml` | The login screen |

| API answer | What the app does |
|---|---|
| 401 on a write | Clears the token, opens Log in with "Please log in to make changes." Your form is kept |
| 400 | Shows each message under its field (description, amount, notes) |
| 409 | "Duplicate Transaction" alert |
| No answer | "Can't reach the API. Is it running?" Pull down on the list to retry |

Goals and budget categories are stored on the device only. Categories that appear in API data (e.g. `Food`) are added automatically.
