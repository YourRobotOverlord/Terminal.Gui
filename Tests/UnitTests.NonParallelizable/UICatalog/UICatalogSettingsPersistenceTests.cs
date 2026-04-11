#nullable enable

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
}
