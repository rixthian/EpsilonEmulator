Imports System.IO
Imports System.Net.Http
Imports System.Diagnostics
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports Avalonia
Imports Avalonia.Controls
Imports Avalonia.Input
Imports Avalonia.Interactivity
Imports Avalonia.Markup.Xaml
Imports Avalonia.Media
Imports Avalonia.Media.Imaging
Imports Avalonia.Platform

Partial Public Class MainWindow : Inherits Window
    Private WithEvents Window As Window
    Private WithEvents HabboLogoButton As Image
    Private WithEvents GithubButton As Image
    Private WithEvents SulakeButton As Image
    Private WithEvents FooterButton As CustomButton
    Private WithEvents RedeemCodeButton As CustomButton
    Private WithEvents LaunchClientButton As CustomButton
    Private WithEvents SaveConfigButton As CustomButton
    Private WithEvents OpenCmsButton As CustomButton
    Private WithEvents RefreshButton As CustomButton
    Private WithEvents ProfileComboBox As ComboBox
    Private AccessCodeTextBox As TextBox
    Private HotelBaseUrlTextBox As TextBox
    Private LauncherApiBaseUrlTextBox As TextBox
    Private StatusTextBlock As TextBlock
    Private ProfileSummaryTextBlock As TextBlock
    Private PlatformValueTextBlock As TextBlock
    Private ChannelValueTextBlock As TextBlock
    Private DefaultProfileValueTextBlock As TextBlock
    Private RedeemedValueTextBlock As TextBlock
    Private ActivityLogTextBlock As TextBlock
    Private ChannelsItemsControl As ItemsControl

    Private ReadOnly HttpClient As New HttpClient()
    Private ReadOnly JsonOptions As New JsonSerializerOptions With {
        .PropertyNameCaseInsensitive = True
    }

    Private CurrentDesktopConfig As LauncherDesktopConfigSnapshot
    Private CurrentLocalConfig As LauncherLocalConfig
    Private CurrentRedeemResult As LauncherRedeemResult
    Private CurrentProfileSelection As LauncherProfileSelectionResult
    Private CurrentProfiles As List(Of LauncherDesktopProfileSnapshot) = New List(Of LauncherDesktopProfileSnapshot)()
    Private CurrentChannels As List(Of LauncherUpdateChannelSnapshot) = New List(Of LauncherUpdateChannelSnapshot)()
    Private IsBootstrapLoaded As Boolean

    Sub New()
        InitializeComponent()
        AddHandler Opened, AddressOf MainWindow_Opened
    End Sub

    Private Sub InitializeComponent(Optional loadXaml As Boolean = True)
        If loadXaml Then
            AvaloniaXamlLoader.Load(Me)
        End If

        Window = FindNameScope().Find("Window")
        HabboLogoButton = Window.FindNameScope.Find("HabboLogoButton")
        GithubButton = Window.FindNameScope.Find("GithubButton")
        SulakeButton = Window.FindNameScope.Find("SulakeButton")
        FooterButton = Window.FindNameScope.Find("FooterButton")
        RedeemCodeButton = Window.FindNameScope.Find("RedeemCodeButton")
        LaunchClientButton = Window.FindNameScope.Find("LaunchClientButton")
        SaveConfigButton = Window.FindNameScope.Find("SaveConfigButton")
        OpenCmsButton = Window.FindNameScope.Find("OpenCmsButton")
        RefreshButton = Window.FindNameScope.Find("RefreshButton")
        ProfileComboBox = Window.FindNameScope.Find("ProfileComboBox")
        AccessCodeTextBox = Window.FindNameScope.Find("AccessCodeTextBox")
        HotelBaseUrlTextBox = Window.FindNameScope.Find("HotelBaseUrlTextBox")
        LauncherApiBaseUrlTextBox = Window.FindNameScope.Find("LauncherApiBaseUrlTextBox")
        StatusTextBlock = Window.FindNameScope.Find("StatusTextBlock")
        ProfileSummaryTextBlock = Window.FindNameScope.Find("ProfileSummaryTextBlock")
        PlatformValueTextBlock = Window.FindNameScope.Find("PlatformValueTextBlock")
        ChannelValueTextBlock = Window.FindNameScope.Find("ChannelValueTextBlock")
        DefaultProfileValueTextBlock = Window.FindNameScope.Find("DefaultProfileValueTextBlock")
        RedeemedValueTextBlock = Window.FindNameScope.Find("RedeemedValueTextBlock")
        ActivityLogTextBlock = Window.FindNameScope.Find("ActivityLogTextBlock")
        ChannelsItemsControl = Window.FindNameScope.Find("ChannelsItemsControl")

        FooterButton.Text = "Launcher desktop for Epsilon Hotel"
        LaunchClientButton.IsButtonDisabled = True
        SetStatus("Cargando launcher desktop…")
        AddLog("Launcher iniciado.")
    End Sub

    Private Async Sub MainWindow_Opened(sender As Object, e As EventArgs)
        If IsBootstrapLoaded Then
            Return
        End If

        IsBootstrapLoaded = True
        Await BootstrapAsync()
    End Sub

    Private Async Function BootstrapAsync() As Task
        Try
            CurrentLocalConfig = Await LoadLocalConfigAsync()
            CurrentDesktopConfig = Await GetJsonAsync(Of LauncherDesktopConfigSnapshot)(BuildLauncherApiUrl("/launcher/desktop/config"))
            If CurrentDesktopConfig Is Nothing Then
                Throw New Exception("desktop_config_missing")
            End If

            NormalizeLocalConfig()
            CurrentChannels = Await LoadChannelsAsync()
            Await LoadProfilesAsync()
            HydrateLocalConfigControls()
            RenderChannels()
            RenderState()
            SetStatus("Launcher listo. Falta canjear un código único emitido por la CMS.")
            AddLog("Contrato desktop cargado desde el launcher backend.")
        Catch ex As Exception
            SetStatus("No se pudo cargar el contrato del launcher: " & ex.Message)
            AddLog("Error cargando launcher: " & ex.Message)
        End Try
    End Function

    Private Sub NormalizeLocalConfig()
        If CurrentLocalConfig Is Nothing Then
            CurrentLocalConfig = New LauncherLocalConfig()
        End If

        If String.IsNullOrWhiteSpace(CurrentLocalConfig.HotelBaseUrl) Then
            CurrentLocalConfig.HotelBaseUrl = CurrentDesktopConfig.HotelBaseUrl
        End If

        If String.IsNullOrWhiteSpace(CurrentLocalConfig.LauncherApiBaseUrl) Then
            CurrentLocalConfig.LauncherApiBaseUrl = CurrentDesktopConfig.LauncherApiBaseUrl
        End If

        If String.IsNullOrWhiteSpace(CurrentLocalConfig.DefaultChannel) Then
            CurrentLocalConfig.DefaultChannel = CurrentDesktopConfig.DefaultChannel
        End If

        If String.IsNullOrWhiteSpace(CurrentLocalConfig.DefaultProfileKey) Then
            CurrentLocalConfig.DefaultProfileKey = CurrentDesktopConfig.DefaultProfileKey
        End If
    End Sub

    Private Async Function LoadChannelsAsync() As Task(Of List(Of LauncherUpdateChannelSnapshot))
        Dim response = Await GetJsonAsync(Of LauncherUpdateChannelsResponse)(BuildLauncherApiUrl("/launcher/update/channels"))
        If response Is Nothing OrElse response.Channels Is Nothing Then
            Return New List(Of LauncherUpdateChannelSnapshot)
        End If
        If String.IsNullOrWhiteSpace(CurrentLocalConfig.DefaultChannel) Then
            CurrentLocalConfig.DefaultChannel = response.DefaultChannel
        End If
        Return response.Channels
    End Function

    Private Async Function LoadProfilesAsync() As Task
        Dim response = Await GetJsonAsync(Of LauncherProfilesResponse)(
            BuildLauncherApiUrl("/launcher/launch-profiles?platformKind=" & Uri.EscapeDataString(DetectPlatformKind()) & "&channel=" & Uri.EscapeDataString(CurrentLocalConfig.DefaultChannel)))
        CurrentProfiles = If(response?.Profiles, New List(Of LauncherDesktopProfileSnapshot)())

        ProfileComboBox.ItemsSource = CurrentProfiles
        Dim selectedProfileKey = If(Not String.IsNullOrWhiteSpace(CurrentLocalConfig.LastProfileKey), CurrentLocalConfig.LastProfileKey, If(Not String.IsNullOrWhiteSpace(response?.DefaultProfileKey), response.DefaultProfileKey, CurrentLocalConfig.DefaultProfileKey))
        Dim selectedProfile = CurrentProfiles.FirstOrDefault(Function(item) item.ProfileKey = selectedProfileKey)
        If selectedProfile Is Nothing AndAlso CurrentProfiles.Count > 0 Then
            selectedProfile = CurrentProfiles(0)
        End If
        ProfileComboBox.SelectedItem = selectedProfile
        RenderSelectedProfileState()
    End Function

    Private Sub HydrateLocalConfigControls()
        HotelBaseUrlTextBox.Text = CurrentLocalConfig.HotelBaseUrl
        LauncherApiBaseUrlTextBox.Text = CurrentLocalConfig.LauncherApiBaseUrl
    End Sub

    Private Async Function LoadLocalConfigAsync() As Task(Of LauncherLocalConfig)
        Try
            Dim localConfigPath = GetLocalConfigPath()
            If File.Exists(localConfigPath) = False Then
                Return New LauncherLocalConfig()
            End If

            Dim rawJson = Await File.ReadAllTextAsync(localConfigPath)
            Return JsonSerializer.Deserialize(Of LauncherLocalConfig)(rawJson, JsonOptions)
        Catch
            Return New LauncherLocalConfig()
        End Try
    End Function

    Private Async Function SaveLocalConfigAsync() As Task
        Dim localConfigPath = GetLocalConfigPath()
        Directory.CreateDirectory(Path.GetDirectoryName(localConfigPath))
        Dim rawJson = JsonSerializer.Serialize(CurrentLocalConfig, New JsonSerializerOptions With {
            .WriteIndented = True
        })
        Await File.WriteAllTextAsync(localConfigPath, rawJson)
    End Function

    Private Function GetLocalConfigPath() As String
        Return Path.Combine(GetAppDataPath(), "EpsilonLauncher", "config.json")
    End Function

    Private Function GetAppDataPath() As String
        Dim appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        If String.IsNullOrWhiteSpace(appData) Then
            appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")
        End If
        Directory.CreateDirectory(appData)
        Return appData
    End Function

    Private Function BuildLauncherApiUrl(pathAndQuery As String) As String
        Dim baseUrl = If(CurrentLocalConfig?.LauncherApiBaseUrl, Nothing)
        If String.IsNullOrWhiteSpace(baseUrl) Then
            baseUrl = If(CurrentDesktopConfig?.LauncherApiBaseUrl, "http://127.0.0.1:5001")
        End If
        Return New Uri(New Uri(EnsureEndsWithSlash(baseUrl)), pathAndQuery.TrimStart("/"c)).ToString()
    End Function

    Private Function BuildHotelUrl(pathAndQuery As String) As String
        Dim baseUrl = If(CurrentLocalConfig?.HotelBaseUrl, Nothing)
        If String.IsNullOrWhiteSpace(baseUrl) Then
            baseUrl = If(CurrentDesktopConfig?.HotelBaseUrl, "http://127.0.0.1:4100")
        End If
        Return New Uri(New Uri(EnsureEndsWithSlash(baseUrl)), pathAndQuery.TrimStart("/"c)).ToString()
    End Function

    Private Function EnsureEndsWithSlash(value As String) As String
        If value.EndsWith("/") Then
            Return value
        End If
        Return value & "/"
    End Function

    Private Async Function GetJsonAsync(Of T)(url As String) As Task(Of T)
        HttpClient.DefaultRequestHeaders.Clear()
        Dim response = Await HttpClient.GetAsync(url)
        Dim body = Await response.Content.ReadAsStringAsync()
        If response.IsSuccessStatusCode = False Then
            Throw New Exception(If(String.IsNullOrWhiteSpace(body), response.StatusCode.ToString(), body))
        End If
        Return JsonSerializer.Deserialize(Of T)(body, JsonOptions)
    End Function

    Private Async Function PostJsonAsync(Of T)(url As String, payload As Object) As Task(Of T)
        HttpClient.DefaultRequestHeaders.Clear()
        Dim content = New StringContent(JsonSerializer.Serialize(payload), Text.Encoding.UTF8, "application/json")
        Dim response = Await HttpClient.PostAsync(url, content)
        Dim body = Await response.Content.ReadAsStringAsync()
        If response.IsSuccessStatusCode = False Then
            Throw New Exception(If(String.IsNullOrWhiteSpace(body), response.StatusCode.ToString(), body))
        End If
        Return JsonSerializer.Deserialize(Of T)(body, JsonOptions)
    End Function

    Private Function DetectPlatformKind() As String
        If RuntimeInformation.IsOSPlatform(OSPlatform.OSX) Then
            Return "macOS"
        End If
        If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
            Return "Windows"
        End If
        If RuntimeInformation.IsOSPlatform(OSPlatform.Linux) Then
            Return "Linux"
        End If
        Return "Unknown"
    End Function

    Private Sub RenderState()
        PlatformValueTextBlock.Text = DetectPlatformKind()
        ChannelValueTextBlock.Text = If(CurrentLocalConfig?.DefaultChannel, "-")
        DefaultProfileValueTextBlock.Text = If(CurrentLocalConfig?.DefaultProfileKey, "-")
        RedeemedValueTextBlock.Text = If(CurrentRedeemResult Is Nothing, "no", "sí")
    End Sub

    Private Sub RenderChannels()
        Dim stack As New StackPanel With {
            .Spacing = 10
        }

        For Each channel In CurrentChannels
            Dim card As New Border With {
                .Background = New SolidColorBrush(Color.Parse("#1A3D5D")),
                .BorderBrush = New SolidColorBrush(Color.Parse("#2E597B")),
                .BorderThickness = New Thickness(1),
                .CornerRadius = New CornerRadius(12),
                .Padding = New Thickness(14)
            }
            Dim panel As New StackPanel()
            panel.Children.Add(New TextBlock With {
                .Text = channel.ChannelKey?.ToUpperInvariant(),
                .Foreground = New SolidColorBrush(Color.Parse("#9FC0D8")),
                .FontSize = 12,
                .FontWeight = FontWeight.Bold
            })
            panel.Children.Add(New TextBlock With {
                .Text = channel.DisplayName,
                .Foreground = New SolidColorBrush(Color.Parse("#EEF4FB")),
                .FontSize = 18,
                .FontWeight = FontWeight.Bold
            })
            card.Child = panel
            stack.Children.Add(card)
        Next

        ChannelsItemsControl.ItemsSource = New List(Of Control) From {stack}
    End Sub

    Private Sub RenderSelectedProfileState()
        If CurrentRedeemResult Is Nothing Then
            ProfileSummaryTextBlock.Text = "Falta canjear un código único emitido por la CMS."
            LaunchClientButton.IsButtonDisabled = True
            Return
        End If

        If CurrentProfileSelection Is Nothing Then
            ProfileSummaryTextBlock.Text = "Selecciona un perfil. El launcher aún no ha decidido qué cliente abrir."
            LaunchClientButton.IsButtonDisabled = True
            Return
        End If

        Dim message = "Perfil: " & CurrentProfileSelection.Profile.DisplayName & Environment.NewLine &
            "Cliente: " & CurrentProfileSelection.Profile.ClientKind & Environment.NewLine &
            "Estrategia: " & CurrentProfileSelection.LaunchStrategy & Environment.NewLine &
            "Arranque inmediato: " & If(CurrentProfileSelection.CanStartNow, "sí", "no")

        If String.IsNullOrWhiteSpace(CurrentProfileSelection.BlockingReason) = False Then
            message &= Environment.NewLine & "Bloqueo: " & CurrentProfileSelection.BlockingReason
        End If

        ProfileSummaryTextBlock.Text = message
        LaunchClientButton.IsButtonDisabled = CurrentProfileSelection.CanStartNow = False
    End Sub

    Private Sub SetStatus(message As String)
        StatusTextBlock.Text = message
    End Sub

    Private Sub AddLog(message As String)
        Dim prefix = DateTime.Now.ToString("HH:mm:ss")
        If String.IsNullOrWhiteSpace(ActivityLogTextBlock.Text) Then
            ActivityLogTextBlock.Text = "[" & prefix & "] " & message
        Else
            ActivityLogTextBlock.Text = "[" & prefix & "] " & message & Environment.NewLine & ActivityLogTextBlock.Text
        End If
    End Sub

    Private Async Sub RedeemCodeButton_Click(sender As Object, e As EventArgs) Handles RedeemCodeButton.Click
        Dim accessCode = AccessCodeTextBox.Text?.Trim()
        If String.IsNullOrWhiteSpace(accessCode) Then
            SetStatus("Falta el código del launcher.")
            Return
        End If

        Try
            SetStatus("Canjeando código…")
            CurrentRedeemResult = Await PostJsonAsync(Of LauncherRedeemResult)(
                BuildLauncherApiUrl("/launcher/access-codes/redeem"),
                New With {
                    .Code = accessCode,
                    .DeviceLabel = Environment.MachineName & " (" & DetectPlatformKind() & ")",
                    .PlatformKind = DetectPlatformKind()
                })
            SetStatus("Código canjeado. El launcher recibió el handoff. El usuario todavía no está dentro del hotel.")
            AddLog("Código canjeado correctamente.")
            RenderState()
            Await SelectCurrentProfileAsync()
        Catch ex As Exception
            CurrentRedeemResult = Nothing
            CurrentProfileSelection = Nothing
            LaunchClientButton.IsButtonDisabled = True
            RenderState()
            RenderSelectedProfileState()
            SetStatus("No se pudo canjear el código: " & ex.Message)
            AddLog("Error canjeando código: " & ex.Message)
        End Try
    End Sub

    Private Async Function SelectCurrentProfileAsync() As Task
        Dim selectedProfile = TryCast(ProfileComboBox.SelectedItem, LauncherDesktopProfileSnapshot)
        If selectedProfile Is Nothing OrElse CurrentRedeemResult Is Nothing Then
            CurrentProfileSelection = Nothing
            RenderSelectedProfileState()
            Return
        End If

        CurrentProfileSelection = Await PostJsonAsync(Of LauncherProfileSelectionResult)(
            BuildLauncherApiUrl("/launcher/launch-profiles/select"),
            New With {
                .Ticket = CurrentRedeemResult.Ticket,
                .ProfileKey = selectedProfile.ProfileKey,
                .PlatformKind = DetectPlatformKind()
            })

        CurrentLocalConfig.LastProfileKey = selectedProfile.ProfileKey
        Await SaveLocalConfigAsync()
        RenderSelectedProfileState()
        AddLog("Perfil seleccionado: " & selectedProfile.DisplayName)
    End Function

    Private Async Sub ProfileComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ProfileComboBox.SelectionChanged
        If IsBootstrapLoaded = False Then
            Return
        End If

        Try
            Await SelectCurrentProfileAsync()
        Catch ex As Exception
            SetStatus("No se pudo seleccionar el perfil: " & ex.Message)
            AddLog("Error seleccionando perfil: " & ex.Message)
        End Try
    End Sub

    Private Async Sub LaunchClientButton_Click(sender As Object, e As EventArgs) Handles LaunchClientButton.Click
        If CurrentProfileSelection Is Nothing OrElse CurrentRedeemResult Is Nothing Then
            Return
        End If

        If CurrentProfileSelection.CanStartNow = False Then
            SetStatus("Ese perfil todavía no tiene cliente publicado.")
            Return
        End If

        Try
            Await PostJsonAsync(Of LauncherClientStartedResponse)(
                BuildLauncherApiUrl("/launcher/client-started"),
                New With {
                    .Ticket = CurrentRedeemResult.Ticket,
                    .ProfileKey = CurrentProfileSelection.Profile.ProfileKey,
                    .ClientKind = CurrentProfileSelection.Profile.ClientKind,
                    .PlatformKind = DetectPlatformKind()
                })

            Dim launchUrl = ToAbsoluteUrl(CurrentProfileSelection.LaunchUrl)
            Process.Start(New ProcessStartInfo(launchUrl) With {.UseShellExecute = True})
            SetStatus("Cliente abierto. La presencia real ahora depende del emulador.")
            AddLog("Cliente abierto: " & CurrentProfileSelection.Profile.DisplayName)
        Catch ex As Exception
            SetStatus("No se pudo abrir el cliente: " & ex.Message)
            AddLog("Error abriendo cliente: " & ex.Message)
        End Try
    End Sub

    Private Function ToAbsoluteUrl(rawValue As String) As String
        If String.IsNullOrWhiteSpace(rawValue) Then
            Return ""
        End If

        If Uri.TryCreate(rawValue, UriKind.Absolute, Nothing) Then
            Return rawValue
        End If

        Return New Uri(New Uri(EnsureEndsWithSlash(CurrentLocalConfig.LauncherApiBaseUrl)), rawValue.TrimStart("/"c)).ToString()
    End Function

    Private Async Sub SaveConfigButton_Click(sender As Object, e As EventArgs) Handles SaveConfigButton.Click
        CurrentLocalConfig.HotelBaseUrl = HotelBaseUrlTextBox.Text?.Trim()
        CurrentLocalConfig.LauncherApiBaseUrl = LauncherApiBaseUrlTextBox.Text?.Trim()
        Await SaveLocalConfigAsync()
        SetStatus("Config local guardada.")
        AddLog("Config local guardada.")
        RenderState()
    End Sub

    Private Sub OpenCmsButton_Click(sender As Object, e As EventArgs) Handles OpenCmsButton.Click
        Try
            Process.Start(New ProcessStartInfo(BuildHotelUrl("/sites/epsilon-access/")) With {.UseShellExecute = True})
        Catch ex As Exception
            SetStatus("No se pudo abrir la CMS: " & ex.Message)
            AddLog("Error abriendo CMS: " & ex.Message)
        End Try
    End Sub

    Private Async Sub RefreshButton_Click(sender As Object, e As EventArgs) Handles RefreshButton.Click
        Await BootstrapAsync()
    End Sub

    Private Sub FooterButton_Click(sender As Object, e As EventArgs) Handles FooterButton.Click
        Try
            Process.Start(New ProcessStartInfo("https://github.com/LilithRainbows/HabboCustomLauncher") With {.UseShellExecute = True})
        Catch
        End Try
    End Sub

    Private Sub GithubButton_PointerPressed(sender As Object, e As PointerPressedEventArgs) Handles GithubButton.PointerPressed
        Try
            Process.Start(New ProcessStartInfo("https://github.com/LilithRainbows/HabboCustomLauncher") With {.UseShellExecute = True})
        Catch
        End Try
    End Sub

    Private Sub SulakeButton_PointerPressed(sender As Object, e As PointerPressedEventArgs) Handles SulakeButton.PointerPressed
        Try
            Process.Start(New ProcessStartInfo("https://www.sulake.com/habbo/") With {.UseShellExecute = True})
        Catch
        End Try
    End Sub

    Private Sub HabboLogoButton_PointerEntered(sender As Object, e As PointerEventArgs) Handles HabboLogoButton.PointerEntered
        HabboLogoButton.Source = New Bitmap(AssetLoader.Open(New Uri("avares://" & Assembly.GetExecutingAssembly().GetName().Name & "/Assets/habbo-logo-big-2.png")))
    End Sub

    Private Sub HabboLogoButton_PointerExited(sender As Object, e As PointerEventArgs) Handles HabboLogoButton.PointerExited
        HabboLogoButton.Source = New Bitmap(AssetLoader.Open(New Uri("avares://" & Assembly.GetExecutingAssembly().GetName().Name & "/Assets/habbo-logo-big.png")))
    End Sub

    Private Sub GithubButton_PointerEntered(sender As Object, e As PointerEventArgs) Handles GithubButton.PointerEntered
        GithubButton.Source = New Bitmap(AssetLoader.Open(New Uri("avares://" & Assembly.GetExecutingAssembly().GetName().Name & "/Assets/github-icon-2.png")))
    End Sub

    Private Sub GithubButton_PointerExited(sender As Object, e As PointerEventArgs) Handles GithubButton.PointerExited
        GithubButton.Source = New Bitmap(AssetLoader.Open(New Uri("avares://" & Assembly.GetExecutingAssembly().GetName().Name & "/Assets/github-icon.png")))
    End Sub

    Private Sub SulakeButtonButton_PointerEntered(sender As Object, e As PointerEventArgs) Handles SulakeButton.PointerEntered
        SulakeButton.Source = New Bitmap(AssetLoader.Open(New Uri("avares://" & Assembly.GetExecutingAssembly().GetName().Name & "/Assets/habbo-footer-2.png")))
    End Sub

    Private Sub SulakeButtonButton_PointerExited(sender As Object, e As PointerEventArgs) Handles SulakeButton.PointerExited
        SulakeButton.Source = New Bitmap(AssetLoader.Open(New Uri("avares://" & Assembly.GetExecutingAssembly().GetName().Name & "/Assets/habbo-footer.png")))
    End Sub
End Class

Public Class LauncherDesktopConfigSnapshot
    Public Property HotelBaseUrl As String
    Public Property LauncherApiBaseUrl As String
    Public Property DefaultChannel As String
    Public Property DefaultProfileKey As String
End Class

Public Class LauncherUpdateChannelsResponse
    Public Property DefaultChannel As String
    Public Property Channels As List(Of LauncherUpdateChannelSnapshot)
End Class

Public Class LauncherUpdateChannelSnapshot
    Public Property ChannelKey As String
    Public Property DisplayName As String
End Class

Public Class LauncherProfilesResponse
    Public Property PlatformKind As String
    Public Property DefaultProfileKey As String
    Public Property Profiles As List(Of LauncherDesktopProfileSnapshot)
End Class

Public Class LauncherDesktopProfileSnapshot
    Public Property ProfileKey As String
    Public Property DisplayName As String
    Public Property Channel As String
    Public Property ClientKind As String
    Public Property EntryExecutable As String

    Public Overrides Function ToString() As String
        Return DisplayName & " (" & ClientKind & ")"
    End Function
End Class

Public Class LauncherRedeemResult
    Public Property Succeeded As Boolean
    Public Property Ticket As String
    Public Property EntryAssetUrl As String
    Public Property LauncherUrl As String
    Public Property Profile As LauncherClientProfileSnapshot
End Class

Public Class LauncherClientProfileSnapshot
    Public Property ProfileKey As String
    Public Property DisplayName As String
End Class

Public Class LauncherProfileSelectionResult
    Public Property Succeeded As Boolean
    Public Property PlatformKind As String
    Public Property Profile As LauncherDesktopProfileSnapshot
    Public Property LaunchStrategy As String
    Public Property CanStartNow As Boolean
    Public Property BlockingReason As String
    Public Property LaunchUrl As String
End Class

Public Class LauncherClientStartedResponse
    Public Property Succeeded As Boolean
End Class

Public Class LauncherLocalConfig
    Public Property HotelBaseUrl As String
    Public Property LauncherApiBaseUrl As String
    Public Property DefaultChannel As String
    Public Property DefaultProfileKey As String
    Public Property LastProfileKey As String
End Class
