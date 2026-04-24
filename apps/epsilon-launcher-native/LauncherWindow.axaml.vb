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
Imports Avalonia.Threading

Partial Public Class LauncherWindow : Inherits Window
    Private WithEvents Window As Window
    Private WithEvents GameLogoButton As Image
    Private WithEvents GithubButton As Image
    Private WithEvents VendorFooterButton As Image
    Private WithEvents FooterButton As LauncherButton
    Private WithEvents RedeemCodeButton As LauncherButton
    Private WithEvents LaunchClientButton As LauncherButton
    Private WithEvents SaveConfigButton As LauncherButton
    Private WithEvents OpenCmsButton As LauncherButton
    Private WithEvents RefreshButton As LauncherButton
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
    Private CurrentConnectionState As LauncherConnectionStateSnapshot
    Private CurrentProfiles As List(Of LauncherDesktopProfileSnapshot) = New List(Of LauncherDesktopProfileSnapshot)()
    Private CurrentChannels As List(Of LauncherUpdateChannelSnapshot) = New List(Of LauncherUpdateChannelSnapshot)()
    Private ReadOnly ConnectionTimer As New DispatcherTimer()
    Private IsBootstrapLoaded As Boolean
    Private IsRefreshingConnectionState As Boolean
    Private IsLaunchingClient As Boolean
    Private LastConnectionPhaseKey As String
    Private LastStartupAccessCode As String

    Sub New()
        InitializeComponent()
        AddHandler Opened, AddressOf LauncherWindow_Opened
    End Sub

    Private Sub InitializeComponent(Optional loadXaml As Boolean = True)
        If loadXaml Then
            AvaloniaXamlLoader.Load(Me)
        End If

        Window = FindNameScope().Find("Window")
        GameLogoButton = Window.FindNameScope.Find("GameLogoButton")
        GithubButton = Window.FindNameScope.Find("GithubButton")
        VendorFooterButton = Window.FindNameScope.Find("VendorFooterButton")
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
        ConnectionTimer.Interval = TimeSpan.FromSeconds(3)
        AddHandler ConnectionTimer.Tick, AddressOf ConnectionTimer_Tick
        SetStatus("Cargando launcher desktop…")
        AddLog("Launcher iniciado.")
    End Sub

    Private Async Sub LauncherWindow_Opened(sender As Object, e As EventArgs)
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
            Await TryRedeemStartupCodeAsync()
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
            baseUrl = If(CurrentDesktopConfig?.HotelBaseUrl, "http://127.0.0.1:8081")
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
        If CurrentConnectionState IsNot Nothing AndAlso String.IsNullOrWhiteSpace(CurrentConnectionState.PhaseDisplayName) = False Then
            RedeemedValueTextBlock.Text = CurrentConnectionState.PhaseDisplayName
        ElseIf CurrentRedeemResult IsNot Nothing Then
            RedeemedValueTextBlock.Text = "Código canjeado"
        Else
            RedeemedValueTextBlock.Text = "Pendiente"
        End If
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
            ProfileSummaryTextBlock.Text = "Selecciona un modo. La app todavía no ha decidido cómo iniciar el juego."
            LaunchClientButton.IsButtonDisabled = True
            Return
        End If

        Dim message = "Modo: " & CurrentProfileSelection.Profile.DisplayName & Environment.NewLine &
            "Estrategia: " & CurrentProfileSelection.LaunchStrategy & Environment.NewLine &
            "Arranque inmediato: " & If(CurrentProfileSelection.CanStartNow, "sí", "no")

        If CurrentConnectionState IsNot Nothing AndAlso String.IsNullOrWhiteSpace(CurrentConnectionState.PhaseDisplayName) = False Then
            message &= Environment.NewLine & "Conexión real: " & CurrentConnectionState.PhaseDisplayName
        End If

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

        Await RedeemAccessCodeAsync(accessCode, False)
    End Sub

    Private Async Function RedeemAccessCodeAsync(accessCode As String, automatic As Boolean) As Task
        If String.IsNullOrWhiteSpace(accessCode) Then
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
            CurrentConnectionState = Nothing
            ConnectionTimer.Stop()
            SetStatus("Código canjeado. Ya puedes iniciar el juego desde esta app.")
            AddLog(If(automatic, "Código aplicado automáticamente al abrir la app.", "Código canjeado correctamente."))
            RenderState()
            Await SelectCurrentProfileAsync()
            Await RefreshConnectionStateAsync(True)

            If automatic AndAlso CurrentProfileSelection IsNot Nothing AndAlso CurrentProfileSelection.CanStartNow Then
                Await LaunchSelectedClientAsync(True)
            End If
        Catch ex As Exception
            CurrentRedeemResult = Nothing
            CurrentProfileSelection = Nothing
            CurrentConnectionState = Nothing
            ConnectionTimer.Stop()
            LaunchClientButton.IsButtonDisabled = True
            RenderState()
            RenderSelectedProfileState()
            SetStatus("No se pudo canjear el código: " & ex.Message)
            AddLog("Error canjeando código: " & ex.Message)
        End Try
    End Function

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
        AddLog("Modo seleccionado: " & selectedProfile.DisplayName)
    End Function

    Private Async Sub ProfileComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ProfileComboBox.SelectionChanged
        If IsBootstrapLoaded = False Then
            Return
        End If

        Try
            Await SelectCurrentProfileAsync()
        Catch ex As Exception
            SetStatus("No se pudo seleccionar el modo: " & ex.Message)
            AddLog("Error seleccionando modo: " & ex.Message)
        End Try
    End Sub

    Private Async Sub LaunchClientButton_Click(sender As Object, e As EventArgs) Handles LaunchClientButton.Click
        Await LaunchSelectedClientAsync(False)
    End Sub

    Private Async Function LaunchSelectedClientAsync(automatic As Boolean) As Task
        If CurrentProfileSelection Is Nothing OrElse CurrentRedeemResult Is Nothing Then
            If automatic = False Then
                SetStatus("Primero debes canjear un código y seleccionar un modo.")
            End If
            Return
        End If

        If CurrentProfileSelection.CanStartNow = False Then
            SetStatus("Ese modo todavía no tiene un juego publicado para iniciar.")
            Return
        End If

        If IsLaunchingClient Then
            Return
        End If

        Dim launchUrl As String = String.Empty
        Try
            IsLaunchingClient = True
            If automatic Then
                Await Task.Delay(1000)
            End If
            launchUrl = ToAbsoluteUrl(CurrentProfileSelection.LaunchUrl)
            Await PostJsonAsync(Of LauncherTelemetryAck)(
                BuildLauncherApiUrl("/launcher/telemetry"),
                New With {
                    .Ticket = CurrentRedeemResult.Ticket,
                    .EventKey = "launcher_client_launch_requested",
                    .Detail = "El launcher desktop intenta ejecutar el loader del juego.",
                    .Data = New Dictionary(Of String, String) From {
                        {"profileKey", CurrentProfileSelection.Profile.ProfileKey},
                        {"clientKind", CurrentProfileSelection.Profile.ClientKind},
                        {"platformKind", DetectPlatformKind()},
                        {"launchUrl", launchUrl},
                        {"automatic", If(automatic, "true", "false")}
                    }
                })
            OpenLaunchTarget(launchUrl)
            Await PostJsonAsync(Of LauncherClientStartedResponse)(
                BuildLauncherApiUrl("/launcher/client-started"),
                New With {
                    .Ticket = CurrentRedeemResult.Ticket,
                    .ProfileKey = CurrentProfileSelection.Profile.ProfileKey,
                    .ClientKind = CurrentProfileSelection.Profile.ClientKind,
                    .PlatformKind = DetectPlatformKind()
                })
            ConnectionTimer.Start()
            SetStatus(If(automatic,
                "Juego iniciado automáticamente. Esperando confirmación real del emulador.",
                "Juego iniciado. Esperando confirmación real del emulador."))
            AddLog(If(automatic,
                "Juego iniciado automáticamente: " & CurrentProfileSelection.Profile.DisplayName,
                "Juego iniciado: " & CurrentProfileSelection.Profile.DisplayName))
            Await RefreshConnectionStateAsync(True)
            If automatic Then
                ScheduleAutomaticLaunchRetry(launchUrl)
            End If
        Catch ex As Exception
            Try
                PostJsonAsync(Of LauncherTelemetryAck)(
                    BuildLauncherApiUrl("/launcher/telemetry"),
                    New With {
                        .Ticket = CurrentRedeemResult.Ticket,
                        .EventKey = "launcher_client_launch_failed",
                        .Detail = ex.Message,
                        .Data = New Dictionary(Of String, String) From {
                            {"profileKey", CurrentProfileSelection.Profile.ProfileKey},
                            {"clientKind", CurrentProfileSelection.Profile.ClientKind},
                            {"platformKind", DetectPlatformKind()},
                            {"launchUrl", launchUrl},
                            {"automatic", If(automatic, "true", "false")}
                        }
                    }).GetAwaiter().GetResult()
            Catch
            End Try
            SetStatus("No se pudo iniciar el juego: " & ex.Message)
            AddLog("Error iniciando juego: " & ex.Message)
        Finally
            IsLaunchingClient = False
        End Try
    End Function

    Private Async Sub ScheduleAutomaticLaunchRetry(launchUrl As String)
        Try
            Await Task.Delay(3000)
            Await RefreshConnectionStateAsync(False)

            If CurrentConnectionState Is Nothing OrElse CurrentConnectionState.PresenceConfirmed Then
                Return
            End If

            If CurrentConnectionState.ClientStarted = False Then
                Return
            End If

            OpenLaunchTarget(launchUrl)
            AddLog("Reintento de inicio del juego tras arranque automático sin presencia confirmada.")
            Await Task.Delay(3000)
            Await RefreshConnectionStateAsync(True)
        Catch ex As Exception
            AddLog("Error en reintento automático del juego: " & ex.Message)
        End Try
    End Sub

    Private Sub OpenLaunchTarget(launchUrl As String)
        If String.IsNullOrWhiteSpace(launchUrl) Then
            Throw New Exception("launch_url_missing")
        End If

        If RuntimeInformation.IsOSPlatform(OSPlatform.OSX) Then
            If Directory.Exists("/Applications/Safari.app") Then
                RunCommand("/usr/bin/open", "-a", "Safari", launchUrl)
            Else
                RunCommand("/usr/bin/open", launchUrl)
            End If
            Return
        End If

        If RuntimeInformation.IsOSPlatform(OSPlatform.Linux) Then
            RunCommand("/usr/bin/xdg-open", launchUrl)
            Return
        End If

        Process.Start(New ProcessStartInfo(launchUrl) With {.UseShellExecute = True})
    End Sub

    Private Sub RunCommand(fileName As String, ParamArray arguments() As String)
        Dim startInfo As New ProcessStartInfo(fileName) With {
            .UseShellExecute = False
        }

        For Each argument In arguments
            startInfo.ArgumentList.Add(argument)
        Next

        Using process As Process = Process.Start(startInfo)
            If process Is Nothing Then
                Throw New Exception("process_start_failed")
            End If

            If process.WaitForExit(10000) = False Then
                Throw New Exception(fileName & "_timeout")
            End If

            If process.ExitCode <> 0 Then
                Throw New Exception(fileName & "_failed_exit_" & process.ExitCode)
            End If
        End Using
    End Sub

    Private Function ToAbsoluteUrl(rawValue As String) As String
        If String.IsNullOrWhiteSpace(rawValue) Then
            Return ""
        End If

        Dim absoluteUri As Uri = Nothing
        If Uri.TryCreate(rawValue, UriKind.Absolute, absoluteUri) AndAlso
            (String.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) OrElse
             String.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) Then
            Return absoluteUri.ToString()
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
            Process.Start(New ProcessStartInfo(BuildHotelUrl("/")) With {.UseShellExecute = True})
        Catch ex As Exception
            SetStatus("No se pudo abrir la CMS: " & ex.Message)
            AddLog("Error abriendo CMS: " & ex.Message)
        End Try
    End Sub

    Private Async Sub RefreshButton_Click(sender As Object, e As EventArgs) Handles RefreshButton.Click
        Await BootstrapAsync()
    End Sub

    Private Async Sub ConnectionTimer_Tick(sender As Object, e As EventArgs)
        Await RefreshConnectionStateAsync(True)
    End Sub

    Private Async Function RefreshConnectionStateAsync(logChanges As Boolean) As Task
        If CurrentRedeemResult Is Nothing OrElse String.IsNullOrWhiteSpace(CurrentRedeemResult.Ticket) Then
            Return
        End If

        If IsRefreshingConnectionState Then
            Return
        End If

        Try
            IsRefreshingConnectionState = True
            Dim state = Await GetJsonAsync(Of LauncherConnectionStateSnapshot)(
                BuildLauncherApiUrl("/launcher/connection-state?ticket=" & Uri.EscapeDataString(CurrentRedeemResult.Ticket)))
            Dim previousPhaseKey = If(CurrentConnectionState?.PhaseKey, String.Empty)

            CurrentConnectionState = state
            RenderState()
            RenderSelectedProfileState()

            If logChanges AndAlso String.Equals(previousPhaseKey, state.PhaseKey, StringComparison.OrdinalIgnoreCase) = False Then
                AddLog("Estado real: " & state.PhaseDisplayName)
            End If

            LastConnectionPhaseKey = state.PhaseKey

            If state.PresenceConfirmed Then
                ConnectionTimer.Stop()
                SetStatus("Presencia confirmada por el emulador en sala " & state.CurrentRoomId & ".")
            ElseIf state.ClientStarted Then
                SetStatus("Loader ejecutado. Esperando presencia real del emulador.")
            ElseIf state.CodeRedeemed Then
                SetStatus("Código canjeado. La app espera que inicies el juego.")
            End If
        Catch ex As Exception
            If logChanges Then
                AddLog("Error leyendo conexión real: " & ex.Message)
            End If
        Finally
            IsRefreshingConnectionState = False
        End Try
    End Function

    Private Async Function TryRedeemStartupCodeAsync() As Task
        Dim startupCode = ResolveStartupAccessCode()
        If String.IsNullOrWhiteSpace(startupCode) Then
            Return
        End If

        If String.Equals(LastStartupAccessCode, startupCode, StringComparison.OrdinalIgnoreCase) Then
            Return
        End If

        LastStartupAccessCode = startupCode
        AccessCodeTextBox.Text = startupCode
        Await RedeemAccessCodeAsync(startupCode, True)
    End Function

    Private Function ResolveStartupAccessCode() As String
        Dim arguments = Environment.GetCommandLineArgs()
        For index = 0 To arguments.Length - 1
            Dim argument = arguments(index)
            If String.IsNullOrWhiteSpace(argument) Then
                Continue For
            End If

            If argument.StartsWith("--code=", StringComparison.OrdinalIgnoreCase) Then
                Return argument.Substring("--code=".Length).Trim()
            End If

            If String.Equals(argument, "--code", StringComparison.OrdinalIgnoreCase) AndAlso index + 1 < arguments.Length Then
                Return arguments(index + 1).Trim()
            End If

            If argument.StartsWith("epsilonlauncher://", StringComparison.OrdinalIgnoreCase) Then
                Try
                    Dim startupUri As New Uri(argument)
                    Dim query = startupUri.Query.TrimStart("?"c).Split("&"c, StringSplitOptions.RemoveEmptyEntries)
                    For Each pair In query
                        Dim parts = pair.Split("="c, 2)
                        If parts.Length = 2 AndAlso String.Equals(parts(0), "code", StringComparison.OrdinalIgnoreCase) Then
                            Return Uri.UnescapeDataString(parts(1)).Trim()
                        End If
                    Next
                Catch
                End Try
            End If
        Next

        Return String.Empty
    End Function

    Private Sub FooterButton_Click(sender As Object, e As EventArgs) Handles FooterButton.Click
        Try
            Process.Start(New ProcessStartInfo("http://127.0.0.1:8081/") With {.UseShellExecute = True})
        Catch
        End Try
    End Sub

    Private Sub GithubButton_PointerPressed(sender As Object, e As PointerPressedEventArgs) Handles GithubButton.PointerPressed
        Try
            Process.Start(New ProcessStartInfo("http://127.0.0.1:8081/") With {.UseShellExecute = True})
        Catch
        End Try
    End Sub

    Private Sub VendorFooterButton_PointerPressed(sender As Object, e As PointerPressedEventArgs) Handles VendorFooterButton.PointerPressed
        Try
            Process.Start(New ProcessStartInfo("http://127.0.0.1:8081/") With {.UseShellExecute = True})
        Catch
        End Try
    End Sub

    Private Sub GameLogoButton_PointerEntered(sender As Object, e As PointerEventArgs) Handles GameLogoButton.PointerEntered
        GameLogoButton.Source = New Bitmap(AssetLoader.Open(New Uri("avares://" & Assembly.GetExecutingAssembly().GetName().Name & "/Assets/game-logo-hover.png")))
    End Sub

    Private Sub GameLogoButton_PointerExited(sender As Object, e As PointerEventArgs) Handles GameLogoButton.PointerExited
        GameLogoButton.Source = New Bitmap(AssetLoader.Open(New Uri("avares://" & Assembly.GetExecutingAssembly().GetName().Name & "/Assets/game-logo.png")))
    End Sub

    Private Sub GithubButton_PointerEntered(sender As Object, e As PointerEventArgs) Handles GithubButton.PointerEntered
        GithubButton.Source = New Bitmap(AssetLoader.Open(New Uri("avares://" & Assembly.GetExecutingAssembly().GetName().Name & "/Assets/source-link-hover.png")))
    End Sub

    Private Sub GithubButton_PointerExited(sender As Object, e As PointerEventArgs) Handles GithubButton.PointerExited
        GithubButton.Source = New Bitmap(AssetLoader.Open(New Uri("avares://" & Assembly.GetExecutingAssembly().GetName().Name & "/Assets/source-link.png")))
    End Sub

    Private Sub VendorFooterButtonButton_PointerEntered(sender As Object, e As PointerEventArgs) Handles VendorFooterButton.PointerEntered
        VendorFooterButton.Source = New Bitmap(AssetLoader.Open(New Uri("avares://" & Assembly.GetExecutingAssembly().GetName().Name & "/Assets/footer-mark-hover.png")))
    End Sub

    Private Sub VendorFooterButtonButton_PointerExited(sender As Object, e As PointerEventArgs) Handles VendorFooterButton.PointerExited
        VendorFooterButton.Source = New Bitmap(AssetLoader.Open(New Uri("avares://" & Assembly.GetExecutingAssembly().GetName().Name & "/Assets/footer-mark.png")))
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

Public Class LauncherTelemetryAck
    Public Property Succeeded As Boolean
End Class

Public Class LauncherConnectionStateSnapshot
    Public Property SessionValid As Boolean
    Public Property LaunchPermitted As Boolean
    Public Property CodeIssued As Boolean
    Public Property CodeRedeemed As Boolean
    Public Property ClientStarted As Boolean
    Public Property PresenceConfirmed As Boolean
    Public Property CurrentRoomId As Nullable(Of Long)
    Public Property PhaseKey As String
    Public Property PhaseDisplayName As String
    Public Property LastEventKey As String
    Public Property LastEventAtUtc As Nullable(Of DateTime)
End Class

Public Class LauncherLocalConfig
    Public Property HotelBaseUrl As String
    Public Property LauncherApiBaseUrl As String
    Public Property DefaultChannel As String
    Public Property DefaultProfileKey As String
    Public Property LastProfileKey As String
End Class
