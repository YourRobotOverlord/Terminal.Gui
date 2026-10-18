using System.Diagnostics;
using System.Text.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Time;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

internal static class Program
{
    private static void Main (string [] args)
    {
        List<object> results = [];

        foreach (int tabCount in new [] { 1, 4, 20 })
        {
            foreach (int controlCount in new [] { 1, 50 })
            {
                using IDriver driver = CreateDriver ();
                using View root = new () { Driver = driver, CanFocus = true, Width = 160, Height = 30 };
                Tabs tabs = new ();
                root.Add (tabs);
                Button? dirty = null;
                var contentDraws = 0;
                var subViewVisits = 0;

                for (var t = 0; t < tabCount; t++)
                {
                    View page = new () { Title = $"T{t:00}" };
                    page.DrawingContent += (_, _) => contentDraws++;

                    for (var c = 0; c < controlCount; c++)
                    {
                        Button button = new () { Text = $"B{c}", X = c % 10 * 12, Y = c / 10 };
                        button.DrawComplete += (_, _) => subViewVisits++;
                        page.Add (button);

                        if (t == 0 && c == 0)
                        {
                            dirty = button;
                        }
                    }

                    tabs.Add (page);
                }

                root.Layout ();
                root.Draw ();

                for (var i = 0; i < 30; i++)
                {
                    Redraw ();
                }

                Stopwatch calibration = Stopwatch.StartNew ();

                for (var i = 0; i < 10; i++)
                {
                    Redraw ();
                }

                calibration.Stop ();
                int iterations = Math.Clamp ((int)(1000 / Math.Max (calibration.Elapsed.TotalMilliseconds, 1)), 10, 200);
                List<double> times = [];
                List<double> allocations = [];
                contentDraws = 0;
                subViewVisits = 0;

                for (var round = 0; round < 5; round++)
                {
                    long before = GC.GetAllocatedBytesForCurrentThread ();
                    Stopwatch timer = Stopwatch.StartNew ();

                    for (var i = 0; i < iterations; i++)
                    {
                        Redraw ();
                    }

                    timer.Stop ();
                    long allocated = GC.GetAllocatedBytesForCurrentThread () - before;
                    times.Add (timer.Elapsed.TotalMicroseconds / iterations);
                    allocations.Add ((double)allocated / iterations);
                }

                times.Sort ();
                allocations.Sort ();
                results.Add (new
                {
                    tabCount,
                    controlCount,
                    iterations,
                    medianMicroseconds = times [2],
                    bytesPerRedraw = allocations [2],
                    pageContentDraws = contentDraws,
                    controlVisitsPerRedraw = (double)subViewVisits / (5 * iterations)
                });

                void Redraw ()
                {
                    dirty!.SetNeedsDraw ();
                    root.SetClipToScreen ();
                    root.Draw ();
                }
            }
        }

        File.WriteAllText (args [0], JsonSerializer.Serialize (results, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static IDriver CreateDriver ()
    {
        SystemTimeProvider timeProvider = new ();
        AnsiOutput output = new ();
        AnsiComponentFactory factory = new (null, output, null);
        AnsiResponseParser parser = new (timeProvider);
        AnsiRequestScheduler scheduler = new (parser);
        ISizeMonitor sizeMonitor = factory.CreateSizeMonitor (output, new OutputBufferImpl ());
        DriverImpl driver = new (factory, new AnsiInputProcessor (null!, timeProvider), new OutputBufferImpl (), output, scheduler, sizeMonitor);
        sizeMonitor.Initialize (driver);
        driver.SetScreenSize (160, 30);
        driver.Clipboard = new FakeClipboard ();

        return driver;
    }
}
