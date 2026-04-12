// Copilot
#nullable enable
using UnitTests;

namespace ViewsTests;

/// <summary>
///     Parallelizable tests for <see cref="SpinnerView"/> render behavior.
/// </summary>
public class SpinnerViewTests : TestDriverBase
{
    [Fact]
    public void AdvanceAnimation_DefaultDelay_ThrottlesRapidCalls ()
    {
        // SpinnerStyle.Line default delay is 130 ms; calling immediately does not advance the frame.
        IDriver driver = CreateTestDriver (5, 1);
        driver.Clip = new Region (driver.Screen);

        SpinnerView view = new ()
        {
            Driver = driver,
            X = 0,
            Y = 0
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw ();
        string frame1 = driver.Contents! [0, 0].Grapheme;

        // Called immediately — not enough time has elapsed (130 ms throttle).
        view.AdvanceAnimation ();
        view.Draw ();
        string frame2 = driver.Contents [0, 0].Grapheme;

        Assert.Equal (frame1, frame2);
    }

    [Fact]
    public void AutoSpin_SetBeforeEndInit_GetterReturnsTrueWithNoApp ()
    {
        // Regression test for https://github.com/gui-cs/Terminal.Gui/issues/4879
        // Before the fix AutoSpin returned `_timeout != null`. When App is null the timeout
        // can never be registered, so the getter falsely returned false even though the
        // caller had set AutoSpin = true. The fix uses a dedicated _autoSpin backing field.
        SpinnerView spinner = new () { AutoSpin = true };

        // App is null here (no running application), so _timeout is null.
        // The getter must still report true based on the backing field.
        Assert.True (spinner.AutoSpin);

        spinner.BeginInit ();
        spinner.EndInit ();

        // After init the intent must be preserved.
        Assert.True (spinner.AutoSpin);
    }

    [Fact]
    public void AdvanceAnimation_ZeroDelay_AdvancesFrame ()
    {
        // SpinnerStyle.Line sequence: ["-", @"\", "|", "/"]
        // Constructor calls AdvanceAnimation() once (from DateTime.MinValue) → _currentIdx = 1 ("\").
        // After setting SpinDelay = 0 and sleeping 1 ms the next call advances to index 2 ("|").
        IDriver driver = CreateTestDriver (5, 1);
        driver.Clip = new Region (driver.Screen);

        SpinnerView view = new ()
        {
            Driver = driver,
            SpinDelay = 0,
            X = 0,
            Y = 0
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Guarantee >0 ms has elapsed since _lastRender was set in the constructor.
        Thread.Sleep (2);

        view.AdvanceAnimation ();
        view.Draw ();

        Assert.Equal ("|", driver.Contents! [0, 0].Grapheme);
    }

    // Copilot
    [Fact]
    public void UseProgressIndicator_Default_IsFalse ()
    {
        SpinnerView spinner = new ();

        Assert.False (spinner.UseProgressIndicator);
    }

    // Copilot
    [Fact]
    public void UseProgressIndicator_SetTrue_ReturnsTrue ()
    {
        SpinnerView spinner = new () { UseProgressIndicator = true };

        Assert.True (spinner.UseProgressIndicator);
    }

    // Copilot
    [Fact]
    public void UseProgressIndicator_WithNoApp_DoesNotThrow ()
    {
        // When App is null, the property and AutoSpin setters must be safe to call.
        SpinnerView spinner = new ();

        Exception? ex = Record.Exception (() =>
                                          {
                                              spinner.UseProgressIndicator = true;
                                              spinner.AutoSpin = true;
                                              spinner.AutoSpin = false;
                                              spinner.UseProgressIndicator = false;
                                          });

        Assert.Null (ex);
    }

    // Copilot
    [Fact]
    public void UseProgressIndicator_AutoSpinTrue_WritesIndeterminateSequence ()
    {
        // Arrange: create a full app+driver and inject a ProgressIndicator.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driverImpl = (DriverImpl)app.Driver!;
        driverImpl.ProgressIndicator = new ProgressIndicator (driverImpl);

        SpinnerView spinner = new ()
        {
            UseProgressIndicator = true,
            App = app
        };

        // Act: enable auto-spin, which should send the indeterminate sequence.
        spinner.AutoSpin = true;

        // Assert
        string output = driverImpl.GetOutput ().GetLastOutput ();
        Assert.Contains (EscSeqUtils.OSC_SetProgressIndeterminate (), output, StringComparison.Ordinal);

        app.Dispose ();
    }

    // Copilot
    [Fact]
    public void UseProgressIndicator_AutoSpinFalse_ClearsProgressIndicator ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driverImpl = (DriverImpl)app.Driver!;
        driverImpl.ProgressIndicator = new ProgressIndicator (driverImpl);

        SpinnerView spinner = new ()
        {
            UseProgressIndicator = true,
            App = app
        };

        // Prime by setting to indeterminate first, then turn off.
        spinner.AutoSpin = true;
        spinner.AutoSpin = false;

        // Assert: a clear sequence should have been written.
        string output = driverImpl.GetOutput ().GetLastOutput ();
        Assert.Contains (EscSeqUtils.OSC_ClearProgress (), output, StringComparison.Ordinal);

        app.Dispose ();
    }

    // Copilot
    [Fact]
    public void UseProgressIndicator_SetTrueWhileAutoSpinTrue_SendsIndeterminateImmediately ()
    {
        // Arrange: AutoSpin is already true when UseProgressIndicator is enabled.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driverImpl = (DriverImpl)app.Driver!;
        driverImpl.ProgressIndicator = new ProgressIndicator (driverImpl);

        SpinnerView spinner = new ()
        {
            App = app
        };

        // Start spinning before linking the indicator.
        spinner.AutoSpin = true;

        // Enabling UseProgressIndicator while already spinning should immediately send indeterminate.
        spinner.UseProgressIndicator = true;

        string output = driverImpl.GetOutput ().GetLastOutput ();
        Assert.Contains (EscSeqUtils.OSC_SetProgressIndeterminate (), output, StringComparison.Ordinal);

        app.Dispose ();
    }

    // Copilot
    [Fact]
    public void UseProgressIndicator_WhenFalse_AutoSpinDoesNotWriteProgressSequence ()
    {
        // When UseProgressIndicator is false (the default), AutoSpin should not touch the terminal
        // progress indicator at all.
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        DriverImpl driverImpl = (DriverImpl)app.Driver!;
        driverImpl.ProgressIndicator = new ProgressIndicator (driverImpl);

        SpinnerView spinner = new ()
        {
            App = app,
            UseProgressIndicator = false
        };

        spinner.AutoSpin = true;

        string output = driverImpl.GetOutput ().GetLastOutput ();
        Assert.DoesNotContain (EscSeqUtils.OSC_SetProgressIndeterminate (), output, StringComparison.Ordinal);
        Assert.DoesNotContain (EscSeqUtils.OSC_ClearProgress (), output, StringComparison.Ordinal);

        app.Dispose ();
    }
}
