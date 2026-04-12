#nullable enable

using Microsoft.Extensions.Logging;
using Serilog.Events;
using UICatalog;

namespace UnitTests.NonParallelizable.UICatalogTests;

public class UICatalogSettingsPersistenceTests
{
    // Copilot
    [Fact]
    public void PersistCurrentConfiguration_WhenConfigurationManagerDisabled_DoesNotThrow ()
    {
        Assert.False (ConfigurationManager.IsEnabled);

        UICatalogSettingsPersistence.PersistCurrentConfiguration ();
    }

    // Copilot
    [Fact]
    public void PersistThemeSelection_Saves_SelectedTheme_ToAppCurrentConfig ()
    {
        Assert.False (ConfigurationManager.IsEnabled);

        string originalAppName = ConfigurationManager.AppName;
        string originalCurrentDirectory = Environment.CurrentDirectory;
        string tempRoot = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ());

        try
        {
            Directory.CreateDirectory (tempRoot);
            Directory.SetCurrentDirectory (tempRoot);

            ConfigurationManager.Enable (ConfigLocations.HardCoded);
            ConfigurationManager.AppName = "UICatalog";

            ThemeManager.Themes! ["Saved Test Theme"] = ThemeManager.Themes [ThemeManager.DEFAULT_THEME_NAME];

            ThemeManager.Theme = "Saved Test Theme";
            UICatalogSettingsPersistence.PersistThemeSelection ();

            string expectedPath = Path.Combine (tempRoot, ".tui", "UICatalog.config.json");

            Assert.True (File.Exists (expectedPath));

            string json = File.ReadAllText (expectedPath);

            Assert.Contains ("\"Theme\": \"Saved Test Theme\"", json);
        }
        finally
        {
            Directory.SetCurrentDirectory (originalCurrentDirectory);
            ConfigurationManager.AppName = originalAppName;
            ConfigurationManager.Disable (true);

            if (Directory.Exists (tempRoot))
            {
                Directory.Delete (tempRoot, true);
            }
        }
    }

    // Copilot
    [Fact]
    public void PersistCurrentConfiguration_Uses_Default_AppCurrent_Location ()
    {
        Assert.False (ConfigurationManager.IsEnabled);

        string originalAppName = ConfigurationManager.AppName;
        string originalCurrentDirectory = Environment.CurrentDirectory;
        string tempRoot = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ());

        try
        {
            Directory.CreateDirectory (tempRoot);
            Directory.SetCurrentDirectory (tempRoot);

            ConfigurationManager.Enable (ConfigLocations.HardCoded);
            ConfigurationManager.AppName = "UICatalog";

            UICatalogSettingsPersistence.PersistCurrentConfiguration ();

            string expectedPath = Path.Combine (tempRoot, ".tui", "UICatalog.config.json");

            Assert.True (File.Exists (expectedPath));
        }
        finally
        {
            Directory.SetCurrentDirectory (originalCurrentDirectory);
            ConfigurationManager.AppName = originalAppName;
            ConfigurationManager.Disable (true);

            if (Directory.Exists (tempRoot))
            {
                Directory.Delete (tempRoot, true);
            }
        }
    }

    // Copilot
    [Fact]
    public void PersistCurrentConfiguration_Saves_ConfigBackedOptions ()
    {
        Assert.False (ConfigurationManager.IsEnabled);

        string originalAppName = ConfigurationManager.AppName;
        string originalCurrentDirectory = Environment.CurrentDirectory;
        string tempRoot = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ());

        try
        {
            Directory.CreateDirectory (tempRoot);
            Directory.SetCurrentDirectory (tempRoot);

            ConfigurationManager.Enable (ConfigLocations.HardCoded);
            ConfigurationManager.AppName = "UICatalog";

            Driver.Force16Colors = true;
            UICatalogRunnable.CachedRunnableScheme = "Saved Runnable Scheme";
            global::UICatalog.UICatalog.PersistedDebugLogLevel = LogLevel.Error.ToString ();
#pragma warning disable CS0618
            Application.IsMouseDisabled = true;
#pragma warning restore CS0618

            UICatalogSettingsPersistence.PersistCurrentConfiguration ();

            string expectedPath = Path.Combine (tempRoot, ".tui", "UICatalog.config.json");

            Assert.True (File.Exists (expectedPath));

            string json = File.ReadAllText (expectedPath);

            Assert.Contains ("\"Driver.Force16Colors\": true", json);
            Assert.Contains ("\"Application.IsMouseDisabled\": true", json);
        }
        finally
        {
            Directory.SetCurrentDirectory (originalCurrentDirectory);
            ConfigurationManager.AppName = originalAppName;
            ConfigurationManager.Disable (true);

            if (Directory.Exists (tempRoot))
            {
                Directory.Delete (tempRoot, true);
            }
        }
    }

    // Copilot
    [Fact]
    public void Constructor_DoesNotOverwrite_PersistedRunnableScheme ()
    {
        string originalScheme = UICatalogRunnable.CachedRunnableScheme;

        try
        {
            UICatalogRunnable.CachedRunnableScheme = "Persisted Scheme";

            UICatalogRunnable runnable = new ();

            Assert.Equal ("Persisted Scheme", UICatalogRunnable.CachedRunnableScheme);
            Assert.Equal ("Persisted Scheme", runnable.SchemeName);
        }
        finally
        {
            UICatalogRunnable.CachedRunnableScheme = originalScheme;
        }
    }

    // Copilot
    [Fact]
    public void PersistedDebugLogLevel_Updates_Options_And_LogLevelSwitch ()
    {
        string originalPersistedDebugLogLevel = global::UICatalog.UICatalog.PersistedDebugLogLevel;
        UICatalogCommandLineOptions originalOptions = global::UICatalog.UICatalog.Options;
        LogEventLevel originalMinimumLevel = global::UICatalog.UICatalog.LogLevelSwitch.MinimumLevel;

        try
        {
            global::UICatalog.UICatalog.PersistedDebugLogLevel = LogLevel.Debug.ToString ();

            Assert.Equal (LogLevel.Debug.ToString (), global::UICatalog.UICatalog.Options.DebugLogLevel);
            Assert.Equal (LogEventLevel.Debug, global::UICatalog.UICatalog.LogLevelSwitch.MinimumLevel);
        }
        finally
        {
            global::UICatalog.UICatalog.Options = originalOptions;
            global::UICatalog.UICatalog.PersistedDebugLogLevel = originalPersistedDebugLogLevel;
            global::UICatalog.UICatalog.LogLevelSwitch.MinimumLevel = originalMinimumLevel;
        }
    }

    // Copilot
    [Fact]
    public void ApplyMousePreference_Synchronizes_LiveAppMouse_State ()
    {
#pragma warning disable CS0618
        bool originalValue = Application.IsMouseDisabled;
        IApplication? app = null;

        try
        {
            Application.IsMouseDisabled = true;
            app = Application.Create ();
            app.Init ();

            Assert.False (app.Mouse.IsMouseDisabled);

            UICatalogSettingsPersistence.ApplyMousePreference (app);

            Assert.True (app.Mouse.IsMouseDisabled);
        }
        finally
        {
            app?.Dispose ();
            Application.IsMouseDisabled = originalValue;
        }
#pragma warning restore CS0618
    }
}
