Imports System.Reflection
Imports Avalonia
Imports Avalonia.Media

Module Program
    <STAThread>
    Sub Main(args As String())
        Dim AvaloniaApp = BuildAvaloniaApp()
        AvaloniaApp.StartWithClassicDesktopLifetime(args)
    End Sub

    Public Function BuildAvaloniaApp() As AppBuilder
        Dim fontFamilyName = "avares://" & Assembly.GetExecutingAssembly().GetName().Name & "/Assets/Segoe-UI-Variable-Static-Text.ttf#Segoe UI Variable"
        Dim fontOptions As New FontManagerOptions With {
            .DefaultFamilyName = fontFamilyName,
            .FontFallbacks = {
                New FontFallback With {
                    .FontFamily = New FontFamily(fontFamilyName)
                }
            }
        }

        Return AppBuilder.Configure(Of App) _
            .UsePlatformDetect() _
            .LogToTrace() _
            .With(fontOptions) _
            .WithSystemFontSource(New Uri(fontFamilyName, UriKind.Absolute))
    End Function
End Module
