#nullable enable

namespace UICatalog;

/// <summary>
///     Centralizes UI Catalog configuration persistence so additional user-selected options can be persisted through a
///     single path.
/// </summary>
public static class UICatalogSettingsPersistence
{
    /// <summary>
    ///     The default configuration location used for persisted UI Catalog settings.
    /// </summary>
    public static ConfigLocations DefaultLocation => ConfigLocations.AppCurrent;

    /// <summary>
    ///     Persists the current effective UI Catalog configuration state.
    /// </summary>
    public static void PersistCurrentConfiguration ()
    {
        if (!ConfigurationManager.IsEnabled)
        {
            Logging.Debug ("Configuration Manager is disabled; skipping UI Catalog configuration persistence.");

            return;
        }

        Logging.Information ($"Persisting UI Catalog configuration to {DefaultLocation}.");
        ConfigurationManager.Save (DefaultLocation);
    }

    /// <summary>
    ///     Persists the currently selected theme and any related effective configuration state.
    /// </summary>
    public static void PersistThemeSelection ()
    {
        if (!ConfigurationManager.IsEnabled)
        {
            Logging.Debug ("Configuration Manager is disabled; skipping theme persistence.");

            return;
        }

        ConfigurationManager.Apply ();
        PersistCurrentConfiguration ();
    }

    /// <summary>
    ///     Persists the current status-bar preference.
    /// </summary>
    public static void PersistStatusBarPreference () => PersistCurrentConfiguration ();

    /// <summary>
    ///     Persists the current driver color-mode preference.
    /// </summary>
    public static void PersistDriverColorMode () => PersistCurrentConfiguration ();

    /// <summary>
    ///     Persists the current mouse preference.
    /// </summary>
    public static void PersistMousePreference (IApplication? app)
    {
        ApplyMousePreference (app);
        PersistCurrentConfiguration ();
    }

    /// <summary>
    ///     Applies the current mouse preference to the live application instance.
    /// </summary>
    public static void ApplyMousePreference (IApplication? app)
    {
        if (app is null)
        {
            return;
        }

#pragma warning disable CS0618
        app.Mouse.IsMouseDisabled = Application.IsMouseDisabled;
#pragma warning restore CS0618
    }

    /// <summary>
    ///     Persists the current runnable scheme preference.
    /// </summary>
    public static void PersistRunnableSchemeSelection () => PersistCurrentConfiguration ();

    /// <summary>
    ///     Persists the current debug log-level preference.
    /// </summary>
    public static void PersistDebugLogLevel () => PersistCurrentConfiguration ();
}
