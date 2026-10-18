using UnitTests;

namespace ViewBaseTests.Draw;

// CoPilot - gpt-6.1-sol
public class SharedLineCanvasCacheTests : TestDriverBase
{
    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void CleanSharedView_DoesNotRebuildBordersOrVisitSubViews (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out View right);
        string expected = driver.ToString ();
        var borderDraws = 0;
        var controlVisits = 0;
        right.Border.View!.DrawComplete += (_, _) => borderDraws++;
        right.SubViews.Single ().DrawComplete += (_, _) => controlVisits++;

        for (var i = 0; i < 3; i++)
        {
            left.SubViews.Single ().SetNeedsDraw ();
            root.SetClipToScreen ();
            root.Draw ();
            Assert.Equal (expected, driver.ToString ());
        }

        Assert.Equal (0, borderDraws);
        Assert.Equal (0, controlVisits);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void DirectDraw_RemovingBorder_ReplacesCachedLinesWithEmptyContribution (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out View right);
        right.BorderStyle = LineStyle.None;
        root.Layout ();
        right.SetClipToScreen ();
        right.Draw ();
        root.ClearNeedsDraw ();

        left.SubViews.Single ().SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        AssertMatchesFullRedraw (root, driver);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void CompositionModeChange_RebuildsCachedContribution (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out View right);
        right.Arrangement = overlapped ? ViewArrangement.Fixed : ViewArrangement.Overlapped;

        left.SubViews.Single ().SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        AssertMatchesFullRedraw (root, driver);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void PendingCustomLines_AreIncludedAndRetainedOnCleanRedraw (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out View right);
        right.LineCanvas.AddLine (new Point (14, 3), 4, Orientation.Horizontal, LineStyle.Double);

        left.SubViews.Single ().SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        Assert.Equal ("═", driver.Contents! [3, 14].Grapheme);
        string expected = driver.ToString ();

        left.SubViews.Single ().SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        Assert.Equal (expected, driver.ToString ());
    }

    [Fact]
    public void CulledView_RebuildsEarlierSharedContributionWhenSharingResumes ()
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = new () { Driver = driver, Width = 24, Height = 8 };
        View bottom = new ()
        {
            Width = Dim.Fill (), Height = Dim.Fill (), Arrangement = ViewArrangement.Overlapped,
            BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true
        };
        View top = new () { Width = Dim.Fill (), Height = Dim.Fill (), Arrangement = ViewArrangement.Overlapped, Visible = false };
        root.Add (bottom, top);
        root.Layout ();
        root.Draw ();

        var bottomDraws = 0;
        bottom.DrawComplete += (_, _) => bottomDraws++;
        bottom.SuperViewRendersLineCanvas = false;
        bottom.BorderStyle = LineStyle.Double;
        top.Visible = true;
        root.Layout ();
        root.SetClipToScreen ();
        root.Draw ();
        Assert.Equal (0, bottomDraws);
        Assert.False (bottom.NeedsDraw);

        var borderDraws = 0;
        bottom.Border.View!.DrawComplete += (_, _) => borderDraws++;
        bottom.SuperViewRendersLineCanvas = true;
        top.SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();

        Assert.Equal (1, borderDraws);
        AssertMatchesFullRedraw (root, driver);
    }

    private static View CreateViews (IDriver driver, bool overlapped, out View left, out View right)
    {
        View root = new () { Driver = driver, Width = 24, Height = 8 };
        ViewArrangement arrangement = overlapped ? ViewArrangement.Overlapped : ViewArrangement.Fixed;
        left = new () { Width = 12, Height = 6, Title = "Left", BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true, Arrangement = arrangement };
        right = new () { X = 11, Width = 12, Height = 6, Title = "Right", BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true, Arrangement = arrangement };
        left.Add (new Button { Text = "Left" });
        right.Add (new Button { Text = "Right" });
        root.Add (left, right);
        root.Layout ();
        root.Draw ();

        return root;
    }

    private static void AssertMatchesFullRedraw (View root, IDriver driver)
    {
        string partial = driver.ToString ();
        root.SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        Assert.Equal (driver.ToString (), partial);
    }
}
