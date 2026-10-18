using UnitTests;

namespace ViewsTests;

// CoPilot - gpt-6.1-sol
public class TabsDrawingTests : TestDriverBase
{
    [Theory]
    [InlineData (Side.Top, 0)]
    [InlineData (Side.Top, 1)]
    [InlineData (Side.Top, 2)]
    [InlineData (Side.Bottom, 0)]
    [InlineData (Side.Bottom, 1)]
    [InlineData (Side.Bottom, 2)]
    [InlineData (Side.Left, 0)]
    [InlineData (Side.Left, 1)]
    [InlineData (Side.Left, 2)]
    [InlineData (Side.Right, 0)]
    [InlineData (Side.Right, 1)]
    [InlineData (Side.Right, 2)]
    public void PartialRedraw_PreservesTabBorders (Side side, int selectedIndex)
    {
        using IDriver driver = CreateTestDriver (40, 20);
        using View superView = new () { Driver = driver, CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        Tabs tabs = new () { TabSide = side };
        superView.Add (tabs);

        View [] pages = [new () { Title = "One" }, new () { Title = "Two" }, new () { Title = "Three" }];

        foreach (View page in pages)
        {
            page.Add (new Button { Text = "Button" });
            tabs.Add (page);
        }

        tabs.Value = pages [selectedIndex];
        superView.Layout ();
        superView.Draw ();
        string expected = driver.ToString ();
        var contentDraws = 0;
        var viewportClears = 0;

        foreach (View page in pages)
        {
            page.DrawingContent += (_, _) => contentDraws++;
            page.ClearedViewport += (_, _) => viewportClears++;
        }

        foreach (View page in pages)
        {
            page.SetNeedsDraw ();
            superView.SetClipToScreen ();
            superView.Draw ();
            Assert.Equal (expected, driver.ToString ());

            contentDraws = 0;
            viewportClears = 0;
            page.SubViews.Single ().SetNeedsDraw ();
            superView.SetClipToScreen ();
            superView.Draw ();
            Assert.Equal (expected, driver.ToString ());
            Assert.Equal (0, contentDraws);
            Assert.Equal (0, viewportClears);

            ((BorderView)page.Border.View!).TitleView!.SetNeedsDraw ();
            superView.SetClipToScreen ();
            superView.Draw ();
            Assert.Equal (expected, driver.ToString ());
            Assert.Equal (0, contentDraws);
            Assert.Equal (0, viewportClears);
        }
    }
}
