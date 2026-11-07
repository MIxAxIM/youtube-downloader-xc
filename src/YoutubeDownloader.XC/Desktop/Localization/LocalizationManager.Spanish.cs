using System.Collections.Generic;

namespace YoutubeDownloader.XC.Localization;

public partial class LocalizationManager
{
    private static readonly IReadOnlyDictionary<string, string> SpanishLocalization =
        new Dictionary<string, string>
        {
            // Dashboard
            [nameof(QueryPlaceholderText)] = "URL o consulta de búsqueda",
            [nameof(QueryTooltip)] =
                "Se acepta cualquier URL o ID de YouTube válido. Antepone un signo de interrogación (?) para buscar por texto.",
            [nameof(ProcessQueryTooltip)] = "Procesar consulta (Enter)",
            [nameof(AuthTooltip)] = "Autenticación",
            [nameof(SettingsTooltip)] = "Configuración",
            [nameof(DashboardPlaceholder)] = """
                Pega una **URL** o ingresa una **consulta de búsqueda** para comenzar
                Presiona **Shift+Enter** para agregar múltiples elementos
                """,
            [nameof(DownloadsFileColumnHeader)] = "Archivo",
            [nameof(DownloadsStatusColumnHeader)] = "Estado",
            [nameof(ContextMenuRemoveSuccessful)] = "Eliminar descargas exitosas",
            [nameof(ContextMenuRemoveInactive)] = "Eliminar descargas inactivas",
            [nameof(ContextMenuRestartFailed)] = "Reiniciar descargas fallidas",
            [nameof(ContextMenuCancelAll)] = "Cancelar todas las descargas",
            [nameof(DownloadStatusEnqueued)] = "Pendiente...",
            [nameof(DownloadStatusCompleted)] = "Listo",
            [nameof(DownloadStatusCanceled)] = "Cancelado",
            [nameof(DownloadStatusFailed)] = "Fallido",
            [nameof(ClickToCopyErrorTooltip)] = "Nota: Haz clic para copiar este mensaje de error",
            [nameof(ShowFileTooltip)] = "Mostrar archivo",
            [nameof(PlayTooltip)] = "Reproducir",
            [nameof(CancelDownloadTooltip)] = "Cancelar descarga",
            [nameof(RestartDownloadTooltip)] = "Reiniciar descarga",
            // Settings
            [nameof(SettingsTitle)] = "Configuración",
            [nameof(ThemeLabel)] = "Tema",
            [nameof(ThemeTooltip)] = "Tema de interfaz preferido",
            [nameof(LanguageLabel)] = "Idioma",
            [nameof(LanguageTooltip)] =
                "Idioma de visualización preferido para la interfaz de usuario",
            [nameof(AutoUpdateLabel)] = "Actualización automática",
            [nameof(AutoUpdateTooltip)] = """
                Realizar actualizaciones automáticas en cada inicio.
                **Advertencia:** se recomienda dejar esta opción habilitada para asegurar la compatibilidad con la última versión de YouTube.
                """,
            [nameof(PersistAuthLabel)] = "Conservar autenticación",
            [nameof(PersistAuthTooltip)] = """
                Guardar las cookies de autenticación en un archivo para persistirlas entre sesiones.
                **Advertencia**: aunque las cookies se almacenan con cifrado, un atacante con acceso a su sistema podría recuperarlas.
                """,
            [nameof(InjectAltLanguagesLabel)] = "Insertar idiomas alternativos",
            [nameof(InjectAltLanguagesTooltip)] =
                "Insertar pistas de audio en idiomas alternativos (si están disponibles) en los archivos descargados",
            [nameof(InjectSubtitlesLabel)] = "Insertar subtítulos",
            [nameof(InjectSubtitlesTooltip)] =
                "Insertar subtítulos (si están disponibles) en los archivos descargados",
            [nameof(InjectTagsLabel)] = "Insertar etiquetas multimedia",
            [nameof(InjectTagsTooltip)] =
                "Insertar etiquetas multimedia (si están disponibles) en los archivos descargados",
            [nameof(SkipExistingFilesLabel)] = "Omitir archivos existentes",
            [nameof(SkipExistingFilesTooltip)] =
                "Al descargar múltiples videos, omitir los que ya tengan archivos correspondientes en el directorio de salida",
            [nameof(FileNameTemplateLabel)] = "Plantilla de nombre de archivo",
            [nameof(FileNameTemplateTooltip)] = """
                Plantilla para generar nombres de archivo de los videos descargados.

                Tokens disponibles:
                **$num** — posición del video en la lista (si aplica)
                **$id** — ID del video
                **$title** — título del video
                **$author** — autor del video
                """,
            [nameof(ParallelLimitLabel)] = "Límite paralelo",
            [nameof(ParallelLimitTooltip)] =
                "Cuántas descargas pueden estar activas al mismo tiempo",
            [nameof(UseProxyLabel)] = "Usar proxy",
            [nameof(UseProxyTooltip)] =
                "Enviar las solicitudes de YouTube, las descargas, las etiquetas y las actualizaciones a través de este proxy mientras esté activado.",
            [nameof(ProxyProtocolLabel)] = "Protocolo",
            [nameof(ProxyHostLabel)] = "Host",
            [nameof(ProxyPortLabel)] = "Puerto",
            [nameof(ProxyUsernameLabel)] = "Usuario",
            [nameof(ProxyPasswordLabel)] = "Contraseña",
            [nameof(FFmpegPathLabel)] = "Ruta de FFmpeg",
            [nameof(FFmpegPathTooltip)] =
                "Ruta al ejecutable de FFmpeg. Dejar vacío para detección automática.",
            [nameof(FFmpegPathPlaceholderText)] = "Auto",
            [nameof(FFmpegPathResetTooltip)] = "Restablecer detección automática",
            [nameof(FFmpegPathBrowseTooltip)] = "Buscar ejecutable de FFmpeg",
            // Auth Setup
            [nameof(AuthenticationTitle)] = "Autenticación",
            [nameof(AuthenticatedText)] = "Actualmente estás autenticado",
            [nameof(LogOutButton)] = "Cerrar sesión",
            [nameof(AuthenticationPlaceholderText)] = """
                Cargando...

                Si el navegador no aparece:
                - Windows: instala Microsoft Edge WebView2 Runtime.
                - macOS: instala las últimas actualizaciones del sistema.
                - Linux: instala GTK 3, WebKitGTK 4.1 y libsoup 3.
                """,
            // Download Single Setup
            [nameof(CopyMenuItem)] = "Copiar",
            [nameof(LiveLabel)] = "En vivo",
            [nameof(AudioLabel)] = "Audio",
            [nameof(UpscaledLabel)] = "Reescalado",
            [nameof(FormatLabel)] = "Formato",
            [nameof(VideoLabel)] = "Vídeo",
            [nameof(AudioBitrateLabel)] = "Tasa de bits de audio",
            [nameof(PasteButton)] = "Pegar enlace",
            [nameof(PasteTooltip)] = "Pegar un enlace del portapapeles y añadirlo a la cola",
            [nameof(DownloadAllButton)] = "Descargar todo",
            [nameof(WaitingLabel)] = "Esperando...",
            [nameof(DownloadedProgress)] = "Descargado {0} de {1}",
            [nameof(UnsupportedBitrateTooltip)] =
                "YouTube no ofreció esta tasa. La aplicación aún puede codificar el archivo a esa tasa.",
            [nameof(GenreLabel)] = "Género",
            [nameof(DateLabel)] = "Fecha",
            [nameof(SpeedLimitLabel)] = "Límite de velocidad",
            [nameof(UnlimitedLabel)] = "Ilimitado",
            [nameof(HistoryTooltip)] = "Historial",
            [nameof(HistoryTitle)] = "Historial",
            [nameof(ClearHistoryButton)] = "BORRAR",
            [nameof(HelpTooltip)] = "Ayuda",
            [nameof(HelpTitle)] = "Ayuda",
            [nameof(HelpMessage)] = """
                Pega un enlace para añadir videos a la cola. No se descarga nada hasta que pulses un botón de descarga.

                La barra superior fija el formato y la tasa de las tarjetas que no han empezado. Cada tarjeta puede cambiar su calidad, y las de audio su tasa.

                Una tasa atenuada con un icono de información no la ofreció YouTube. La aplicación aún puede codificarla.

                La tarjeta se llena de izquierda a derecha mientras descarga.
                """,
            [nameof(ClipboardEmptyMessage)] = "El portapapeles no contiene un enlace",
            // Download Multiple Setup
            [nameof(ContainerLabel)] = "Contenedor",
            [nameof(VideoQualityLabel)] = "Calidad de video",
            // Common buttons
            [nameof(CloseButton)] = "CERRAR",
            [nameof(DownloadButton)] = "DESCARGAR",
            [nameof(CancelButton)] = "CANCELAR",
            // Dialog messages
            [nameof(UnstableBuildTitle)] = "Advertencia: versión inestable",
            [nameof(UnstableBuildMessage)] = """
                Estás usando una versión de desarrollo de {0}. Estas versiones no han sido probadas exhaustivamente y pueden contener errores.

                Las actualizaciones automáticas están desactivadas para versiones de desarrollo.

                Haz clic en VER LANZAMIENTOS para descargar una versión estable.
                """,
            [nameof(SeeReleasesButton)] = "VER LANZAMIENTOS",
            [nameof(FFmpegMissingTitle)] = "Falta FFmpeg",
            [nameof(FFmpegMissingMessage)] = """
                FFmpeg no se encontró en tu sistema. Es necesario para que {0} funcione. ¿Te gustaría descargarlo ahora?

                Como alternativa, puedes cerrar este diálogo y establecer manualmente una ruta personalizada de FFmpeg en configuración.
                """,
            [nameof(FFmpegDownloadingTitle)] = "Descargando FFmpeg...",
            [nameof(FFmpegDownloadCompletedTitle)] = "FFmpeg descargado",
            [nameof(NothingFoundTitle)] = "Nada encontrado",
            [nameof(NothingFoundMessage)] =
                "No se encontraron videos basados en la consulta o URL proporcionada",
            [nameof(ErrorTitle)] = "Error",
            [nameof(UpdateDownloadingMessage)] = "Descargando actualización de {0} v{1}...",
            [nameof(UpdateReadyMessage)] =
                "La actualización se ha descargado y se instalará al salir",
            [nameof(UpdateInstallNowButton)] = "INSTALAR AHORA",
            [nameof(UpdateFailedMessage)] = "Error al realizar la actualización de la aplicación",
        };
}
