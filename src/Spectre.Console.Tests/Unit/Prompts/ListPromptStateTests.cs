namespace Spectre.Console.Tests.Unit;

public sealed class ListPromptStateTests
{
    private ListPromptState<string> CreateListPromptState(int count, int pageSize, bool shouldWrap, bool searchEnabled, bool filterEnabled = false)
        => new(
            Enumerable.Range(0, count).Select(i => new ListPromptItem<string>(i.ToString())).ToList(),
            text => text,
            pageSize, shouldWrap, SelectionMode.Independent, true, searchEnabled, filterEnabled);

    [Fact]
    public void Should_Have_Start_Index_Zero()
    {
        // Given
        var state = CreateListPromptState(100, 10, false, false);

        // When
        /* noop */

        // Then
        state.Index.Should().Be(0);
    }

    [Theory]
    [InlineData(ConsoleKey.UpArrow)]
    [InlineData(ConsoleKey.K)]
    public void Should_Decrease_Index(ConsoleKey key)
    {
        // Given
        var state = CreateListPromptState(100, 10, false, false);
        state.Update(ConsoleKey.End.ToConsoleKeyInfo());
        var index = state.Index;

        // When
        state.Update(key.ToConsoleKeyInfo());

        // Then
        state.Index.Should().Be(index - 1);
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow, true)]
    [InlineData(ConsoleKey.DownArrow, false)]
    [InlineData(ConsoleKey.J, true)]
    [InlineData(ConsoleKey.J, false)]
    public void Should_Increase_Index(ConsoleKey key, bool wrap)
    {
        // Given
        var state = CreateListPromptState(100, 10, wrap, false);
        var index = state.Index;

        // When
        state.Update(key.ToConsoleKeyInfo());

        // Then
        state.Index.Should().Be(index + 1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Should_Go_To_End(bool wrap)
    {
        // Given
        var state = CreateListPromptState(100, 10, wrap, false);

        // When
        state.Update(ConsoleKey.End.ToConsoleKeyInfo());

        // Then
        state.Index.Should().Be(99);
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.J)]
    public void Should_Clamp_Index_If_No_Wrap(ConsoleKey key)
    {
        // Given
        var state = CreateListPromptState(100, 10, false, false);
        state.Update(ConsoleKey.End.ToConsoleKeyInfo());

        // When
        state.Update(key.ToConsoleKeyInfo());

        // Then
        state.Index.Should().Be(99);
    }

    [Theory]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.J)]
    public void Should_Wrap_Index_If_Wrap(ConsoleKey key)
    {
        // Given
        var state = CreateListPromptState(100, 10, true, false);
        state.Update(ConsoleKey.End.ToConsoleKeyInfo());

        // When
        state.Update(key.ToConsoleKeyInfo());

        // Then
        state.Index.Should().Be(0);
    }

    [Theory]
    [InlineData(ConsoleKey.UpArrow)]
    [InlineData(ConsoleKey.K)]
    public void Should_Wrap_Index_If_Wrap_And_Down(ConsoleKey key)
    {
        // Given
        var state = CreateListPromptState(100, 10, true, false);

        // When
        state.Update(key.ToConsoleKeyInfo());

        // Then
        state.Index.Should().Be(99);
    }

    [Fact]
    public void Should_Wrap_Index_If_Wrap_And_Page_Up()
    {
        // Given
        var state = CreateListPromptState(10, 100, true, false);

        // When
        state.Update(ConsoleKey.PageUp.ToConsoleKeyInfo());

        // Then
        state.Index.Should().Be(0);
    }

    [Theory]
    [InlineData(ConsoleKey.UpArrow)]
    [InlineData(ConsoleKey.K)]
    public void Should_Wrap_Index_If_Wrap_And_Offset_And_Page_Down(ConsoleKey key)
    {
        // Given
        var state = CreateListPromptState(10, 100, true, false);
        state.Update(ConsoleKey.End.ToConsoleKeyInfo());
        state.Update(key.ToConsoleKeyInfo());

        // When
        state.Update(ConsoleKey.PageDown.ToConsoleKeyInfo());

        // Then
        state.Index.Should().Be(8);
    }

    [Fact]
    public void Should_Jump_To_First_Matching_Item_When_Searching()
    {
        // Given
        var state = CreateListPromptState(10, 100, true, true);

        // When
        state.Update(ConsoleKey.D3.ToConsoleKeyInfo());

        // Then
        state.Index.Should().Be(3);
    }

    [Fact]
    public void Should_Jump_Back_To_First_Item_When_Clearing_Search_Term()
    {
        // Given
        var state = CreateListPromptState(10, 100, true, true);

        // When
        state.Update(ConsoleKey.D3.ToConsoleKeyInfo());
        state.Update(ConsoleKey.Backspace.ToConsoleKeyInfo());

        // Then
        state.Index.Should().Be(0);
    }

    // ----------------------------------------------------------------
    // Filter mode (SearchMode.Filter)
    // ----------------------------------------------------------------

    private ListPromptState<string> CreateFilterState(IReadOnlyList<string> items, int pageSize = 100, bool wrap = false)
        => new(
            items.Select(s => new ListPromptItem<string>(s)).ToList(),
            text => text,
            pageSize, wrap, SelectionMode.Independent, true, searchEnabled: true, filterEnabled: true);

    [Fact]
    public void Filter_Should_Move_Cursor_To_First_Match_On_Type()
    {
        // Items: "apple", "banana", "apricot" — typing 'a' matches all, 'ap' matches apple + apricot
        var state = CreateFilterState(["apple", "banana", "apricot"]);
        state.Update('a'.ToConsoleKeyInfo());
        state.Update('p'.ToConsoleKeyInfo());

        // Index should be 0 ("apple", first match)
        state.Index.Should().Be(0);
    }

    [Fact]
    public void Filter_GetDisplayItems_Should_Return_Only_Matching_Items()
    {
        var state = CreateFilterState(["apple", "banana", "apricot"]);
        state.Update('a'.ToConsoleKeyInfo());
        state.Update('p'.ToConsoleKeyInfo());

        // "ap" matches "apple" (index 0) and "apricot" (index 2)
        var display = state.GetDisplayItems();
        display.Count.Should().Be(2);
        display[0].Data.Should().Be("apple");
        display[1].Data.Should().Be("apricot");
    }

    [Fact]
    public void Filter_GetDisplayIndex_Should_Reflect_Position_In_Filtered_Set()
    {
        var state = CreateFilterState(["apple", "banana", "apricot"]);
        state.Update('a'.ToConsoleKeyInfo()); // all three match
        state.Update(ConsoleKey.DownArrow.ToConsoleKeyInfo()); // move to "banana" within filter
        state.Update('p'.ToConsoleKeyInfo()); // narrow to "apple" + "apricot" — "banana" no longer matches

        // Cursor should have jumped to first match ("apple")
        state.GetDisplayIndex().Should().Be(0);
        state.GetDisplayItems()[0].Data.Should().Be("apple");
    }

    [Fact]
    public void Filter_Navigation_DownArrow_Moves_Within_Filtered_Set()
    {
        var state = CreateFilterState(["apple", "banana", "apricot"]);
        state.Update('a'.ToConsoleKeyInfo()); // "apple", "banana", "apricot" all match
        state.Update(ConsoleKey.DownArrow.ToConsoleKeyInfo()); // move to "banana"

        // Display index = 1 (second in filtered results — "banana")
        state.GetDisplayIndex().Should().Be(1);
        state.GetDisplayItems()[1].Data.Should().Be("banana");
    }

    [Fact]
    public void Filter_Navigation_UpArrow_Moves_Within_Filtered_Set()
    {
        var state = CreateFilterState(["apple", "banana", "apricot"]);
        state.Update('a'.ToConsoleKeyInfo());
        state.Update(ConsoleKey.DownArrow.ToConsoleKeyInfo()); // → banana (pos 1)
        state.Update(ConsoleKey.UpArrow.ToConsoleKeyInfo());   // → apple  (pos 0)

        state.GetDisplayIndex().Should().Be(0);
    }

    [Fact]
    public void Filter_Backspace_Widens_The_Filter()
    {
        var state = CreateFilterState(["apple", "banana", "apricot"]);
        state.Update('a'.ToConsoleKeyInfo());
        state.Update('p'.ToConsoleKeyInfo()); // 2 matches: apple, apricot
        state.Update(ConsoleKey.Backspace.ToConsoleKeyInfo()); // back to 'a': 3 matches

        state.GetDisplayItems().Count.Should().Be(3);
    }

    [Fact]
    public void Filter_Clear_Restores_Full_List()
    {
        var state = CreateFilterState(["apple", "banana", "apricot"]);
        state.Update('x'.ToConsoleKeyInfo()); // no matches
        state.Update(ConsoleKey.Backspace.ToConsoleKeyInfo()); // clear

        // No active filter — GetDisplayItems returns all Items
        state.GetDisplayItems().Count.Should().Be(3);
        state.SearchText.Should().Be(string.Empty);
    }

    [Fact]
    public void Filter_Empty_Search_GetDisplayIndex_Returns_Full_Index()
    {
        var state = CreateFilterState(["apple", "banana", "apricot"]);

        // No search text → GetDisplayIndex == Index
        state.GetDisplayIndex().Should().Be(state.Index);
    }

    [Fact]
    public void Filter_WrapAround_Wraps_At_End_Of_Filtered_Set()
    {
        var state = CreateFilterState(["apple", "banana", "apricot"], wrap: true);
        state.Update('a'.ToConsoleKeyInfo()); // 3 matches
        // Navigate to last filtered item
        state.Update(ConsoleKey.End.ToConsoleKeyInfo());
        // One more down should wrap to first
        state.Update(ConsoleKey.DownArrow.ToConsoleKeyInfo());

        state.GetDisplayIndex().Should().Be(0);
    }

    [Fact]
    public void Filter_WrapAround_Wraps_At_Start_Of_Filtered_Set()
    {
        var state = CreateFilterState(["apple", "banana", "apricot"], wrap: true);
        state.Update('a'.ToConsoleKeyInfo()); // 3 matches, cursor at index 0 (apple)
        state.Update(ConsoleKey.UpArrow.ToConsoleKeyInfo()); // wrap → last (apricot)

        state.GetDisplayIndex().Should().Be(2);
    }

    [Fact]
    public void Filter_NoMatch_Does_Not_Move_Cursor()
    {
        var state = CreateFilterState(["apple", "banana", "apricot"]);
        var initialIndex = state.Index;
        state.Update('z'.ToConsoleKeyInfo()); // no match

        // Cursor does not move; GetDisplayItems falls back to Items because filter has 0 results
        state.Index.Should().Be(initialIndex);
        state.GetDisplayItems().Count.Should().Be(3); // full list shown when no match
    }
}