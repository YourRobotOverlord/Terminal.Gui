using System.Diagnostics;
using System.Text.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Drawing;
using Terminal.Gui.Time;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

internal static class Program
{
    private static void Main (string [] args)
    {
        List<object> results = [];
        bool tiled = args.Length > 1 && args [1] == "tiled";
        int [] viewCounts = tiled ? [1, 6, 24] : [1, 4, 20];
        int [] controlCounts = tiled ? [1, 10] : [1, 50];

        foreach (int tabCount in viewCounts)
        {
            foreach (int controlCount in controlCounts)
            {
                using IDriver driver = CreateDriver ();
                using View root = new () { Driver = driver, CanFocus = true, Width = 160, Height = 30 };
                Tabs tabs = new ();
                if (!tiled)
                {
                    root.Add (tabs);
                }
                Button? dirty = null;
                var contentDraws = 0;
                var subViewVisits = 0;

                for (var t = 0; t < tabCount; t++)
                {
                    View page = tiled
                                    ? new FrameView
                                      {
                                          Title = $"F{t:00}", X = t % 6 * 24, Y = t / 6 * 7, Width = 25, Height = 8,
                                          BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true
                                      }
                                    : new () { Title = $"T{t:00}" };
                    page.DrawingContent += (_, _) => contentDraws++;

                    for (var c = 0; c < controlCount; c++)
                    {
                        Button button = new () { Text = $"B{c}", X = tiled ? c % 2 * 10 : c % 10 * 12, Y = tiled ? c / 2 : c / 10 };
                        button.DrawComplete += (_, _) => subViewVisits++;
                        page.Add (button);

                        if (t == 0 && c == 0)
                        {
                            dirty = button;
                        }
                    }

                    if (tiled)
                    {
                        page.Add (new Line { Y = 5, Length = 23 });
                        root.Add (page);
                    }
                    else
                    {
                        tabs.Add (page);
                    }
                }

                root.Layout ();
                root.Draw ();
                string expected = driver.ToString ()!;

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
                    layout = tiled ? "tiled" : "tabs",
                    tabCount,
                    controlCount,
                    iterations,
                    medianMicroseconds = times [2],
                    bytesPerRedraw = allocations [2],
                    pageContentDraws = contentDraws,
                    controlVisitsPerRedraw = (double)subViewVisits / (5 * iterations)
                });

                if (driver.ToString () != expected)
                {
                    throw new InvalidOperationException ("Partial redraw changed the fixture's glyphs.");
                }

                root.SetNeedsDraw ();
                root.SetClipToScreen ();
                root.Draw ();

                if (driver.ToString () != expected)
                {
                    throw new InvalidOperationException ("Partial redraw differs from full redraw.");
                }

                if (tiled)
                {
                    tabs.Dispose ();
                }

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
