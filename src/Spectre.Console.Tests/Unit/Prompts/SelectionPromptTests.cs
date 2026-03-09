namespace Spectre.Console.Tests.Unit;

public sealed class SelectionPromptTests
{
    private const string ESC = "\u001b";

    [Fact]
    public void Should_Not_Throw_When_Selecting_An_Item_With_Escaped_Markup()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Enter);
        var input = "[red]This text will never be red[/]".EscapeMarkup();

        // When
        var prompt = new SelectionPrompt<string>()
                .Title("Select one")
                .AddChoices(input);
        prompt.Show(console);

        // Then
        console.Output.Should().Contain(@"[red]This text will never be red[/]");
    }

    [Fact]
    public void Should_Select_The_First_Leaf_Item()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        var prompt = new SelectionPrompt<string>()
                .Title("Select one")
                .Mode(SelectionMode.Leaf)
                .AddChoiceGroup("Group one", "A", "B")
                .AddChoiceGroup("Group two", "C", "D");
        var selection = prompt.Show(console);

        // Then
        selection.Should().Be("A");
    }

    [Fact]
    public void Should_Select_The_Last_Leaf_Item_When_Wrapping_Around()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.UpArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        var prompt = new SelectionPrompt<string>()
            .Title("Select one")
            .Mode(SelectionMode.Leaf)
            .WrapAround()
            .AddChoiceGroup("Group one", "A", "B")
            .AddChoiceGroup("Group two", "C", "D");
        var selection = prompt.Show(console);

        // Then
        selection.Should().Be("D");
    }

    [Fact]
    public void Should_Highlight_Search_Term()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.EmitAnsiSequences();
        console.Input.PushText("1");
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        var prompt = new SelectionPrompt<string>()
            .Title("Select one")
            .EnableSearch()
            .AddChoices("Item 1");
        prompt.Show(console);

        // Then
        console.Output.Should().Contain($"{ESC}[38;5;12m> Item {ESC}[0m{ESC}[1;38;5;12;48;5;11m1{ESC}[0m");
    }

    [Fact]
    public void Should_Search_In_Remapped_Result()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.EmitAnsiSequences();
        console.Input.PushText("2");
        console.Input.PushKey(ConsoleKey.Enter);

        var choices = new List<CustomSelectionItem>
        {
            new(33, "Item 1"),
            new(34, "Item 2"),
        };

        var prompt = new SelectionPrompt<CustomSelectionItem>()
            .Title("Select one")
            .EnableSearch()
            .UseConverter(o => o.Name)
            .AddChoices(choices);

        // When
        var selection = prompt.Show(console);

        // Then
        selection.Should().Be(choices[1]);
    }

    [Fact]
    public void Should_Throw_Meaningful_Exception_For_Empty_Prompt()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;

        var prompt = new SelectionPrompt<string>();

        // When
        Action action = () => prompt.Show(console);

        // Then
        var exception = action.Should().Throw<InvalidOperationException>();
        exception.Which.Message.Should().Be("Cannot show an empty selection prompt. Please call the AddChoice() method to configure the prompt.");
    }

    [Fact]
    public void Should_Append_Space_To_Search_If_Search_Is_Enabled()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.EmitAnsiSequences();
        console.Input.PushText("Item");
        console.Input.PushKey(ConsoleKey.Spacebar);
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        var prompt = new SelectionPrompt<string>()
            .Title("Search for something with space")
            .EnableSearch()
            .AddChoices("Item1")
            .AddChoices("Item 2");
        string result = prompt.Show(console);

        // Then
        result.Should().Be("Item 2");
        console.Output.Should().Contain($"{ESC}[38;5;12m> {ESC}[0m{ESC}[1;38;5;12;48;5;11mItem {ESC}[0m{ESC}[38;5;12m2{ESC}[0m ");
    }

    [Fact]
    public void Should_Return_CancelResult_On_Cancel_FuncVersion()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Escape);

        // When
        var prompt = new SelectionPrompt<string>()
                .Title("Select one")
                .Mode(SelectionMode.Leaf)
                .AddChoiceGroup("Group one", "A", "B")
                .AddChoiceGroup("Group two", "C", "D")
                .AddCancelResult(() => "E");
        var selection = prompt.Show(console);

        // Then
        selection.Should().Be("E");
    }

    [Fact]
    public void Should_Return_CancelResult_On_Cancel_ValueVersion()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Escape);

        // When
        var prompt = new SelectionPrompt<string>()
                .Title("Select one")
                .Mode(SelectionMode.Leaf)
                .AddChoiceGroup("Group one", "A", "B")
                .AddChoiceGroup("Group two", "C", "D")
                .AddCancelResult("E");
        var selection = prompt.Show(console);

        // Then
        selection.Should().Be("E");
    }

    [Fact]
    public void Should_Ignore_Escape_If_CancelResult_Not_Set()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Escape);
        console.Input.PushKey(ConsoleKey.Enter);

        // When
        var prompt = new SelectionPrompt<string>()
                .Title("Select one")
                .Mode(SelectionMode.Leaf)
                .AddChoiceGroup("Group one", "A", "B")
                .AddChoiceGroup("Group two", "C", "D");
        var selection = prompt.Show(console);

        // Then
        selection.Should().Be("A");
    }

    [Fact]
    public void Should_Return_Correct_Item_With_No_Title_And_Single_Choice()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Enter);

        // When / Then — regression for #773: CursorUp(0) was emitted for single-line
        // renders, which many terminals interpret as "move up 1", corrupting the display
        var prompt = new SelectionPrompt<string>()
            .AddChoices("OnlyChoice");
        var result = prompt.Show(console);

        result.Should().Be("OnlyChoice");
    }

    [Fact]
    public void Should_Not_Throw_When_Non_Current_Item_Contains_Square_Brackets()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Enter);

        // When / Then (should not throw — regression for markup injection bug)
        var prompt = new SelectionPrompt<string>()
            .Title("Select one")
            .AddChoices("[01] First item", "[02] Second item");
        var result = prompt.Show(console);

        result.Should().Be("[01] First item");
    }

    [Fact]
    public void Should_Not_Throw_When_Searching_Items_With_Square_Brackets()
    {
        // Given
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushText("01");
        console.Input.PushKey(ConsoleKey.Enter);

        // When / Then (should not throw — regression for markup injection bug #1653)
        var prompt = new SelectionPrompt<string>()
            .Title("Select one")
            .EnableSearch()
            .AddChoices("[01] First item", "[02] Second item");
        var result = prompt.Show(console);

        result.Should().Be("[01] First item");
    }
}

public sealed class SearchFilterTests
{
    [Fact]
    public void Should_Return_First_Filtered_Match_On_Enter()
    {
        // Type "ap" → filter to "apple" and "apricot" → Enter selects "apple"
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushText("ap");
        console.Input.PushKey(ConsoleKey.Enter);

        var result = new SelectionPrompt<string>()
            .EnableSearch(SearchMode.Filter)
            .AddChoices("banana", "apple", "apricot", "cherry")
            .Show(console);

        result.Should().Be("apple");
    }

    [Fact]
    public void Should_Return_Second_Filtered_Match_After_Navigation()
    {
        // Type "ap" → "apple", "apricot" — move down → "apricot" → Enter
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushText("ap");
        console.Input.PushKey(ConsoleKey.DownArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        var result = new SelectionPrompt<string>()
            .EnableSearch(SearchMode.Filter)
            .AddChoices("banana", "apple", "apricot", "cherry")
            .Show(console);

        result.Should().Be("apricot");
    }

    [Fact]
    public void Should_Return_Item_After_Backspace_Widens_Filter()
    {
        // "ap" → 2 matches; backspace → "a" → 3 matches; down → "banana" → Enter
        // Note: "banana" contains 'a', so it appears when filter is just 'a'.
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushText("ap");
        console.Input.PushKey(ConsoleKey.Backspace); // now "a"
        console.Input.PushKey(ConsoleKey.Enter);     // "apple" still first match

        var result = new SelectionPrompt<string>()
            .EnableSearch(SearchMode.Filter)
            .AddChoices("banana", "apple", "apricot", "cherry")
            .Show(console);

        // With filter "a": "banana"(idx0), "apple"(idx1), "apricot"(idx2) match.
        // Cursor stays on "apple" (already in filtered set after backspace).
        result.Should().Be("apple");
    }

    [Fact]
    public void EnableSearch_Overload_Should_Throw_For_Null_Prompt()
    {
        var ex = Record.Exception(() =>
            ((SelectionPrompt<string>)null!).EnableSearch(SearchMode.Filter));
        ex.Should().BeOfType<ArgumentNullException>()
              .Which.ParamName.Should().Be("obj");
    }

    [Fact]
    public void Highlight_Mode_Should_Keep_All_Items_Visible_And_Jump_To_Match()
    {
        // Existing behaviour: search text jumps cursor to "item 3" but all items remain
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushText("3");
        console.Input.PushKey(ConsoleKey.Enter);

        var result = new SelectionPrompt<string>()
            .EnableSearch(SearchMode.Highlight)
            .AddChoices("item 1", "item 2", "item 3", "item 4")
            .Show(console);

        result.Should().Be("item 3");
    }
}

public sealed class DefaultValueTests
{
    [Fact]
    public void Should_Pre_Position_Cursor_On_Default_Value()
    {
        // When Enter is pressed immediately, the selected item should be the default.
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Enter);

        var result = new SelectionPrompt<string>()
            .Title("Pick one")
            .AddChoices("First", "Second", "Third")
            .DefaultValue("Second")
            .Show(console);

        result.Should().Be("Second");
    }

    [Fact]
    public void Should_Pre_Position_Cursor_On_Last_Item()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Enter);

        var result = new SelectionPrompt<string>()
            .AddChoices("A", "B", "C")
            .DefaultValue("C")
            .Show(console);

        result.Should().Be("C");
    }

    [Fact]
    public void Should_Allow_Navigation_Away_From_Default()
    {
        // Start at "Second", move up once → land on "First".
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.UpArrow);
        console.Input.PushKey(ConsoleKey.Enter);

        var result = new SelectionPrompt<string>()
            .AddChoices("First", "Second", "Third")
            .DefaultValue("Second")
            .Show(console);

        result.Should().Be("First");
    }

    [Fact]
    public void Should_Fall_Back_To_First_Item_When_Default_Not_Found()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Enter);

        var result = new SelectionPrompt<string>()
            .AddChoices("A", "B", "C")
            .DefaultValue("NotInList")
            .Show(console);

        result.Should().Be("A");
    }

    [Fact]
    public void Should_Skip_Group_Default_In_Leaf_Mode_And_Use_First_Leaf()
    {
        // If the default value is a group header in Leaf mode, the cursor should
        // fall back to the first leaf so that Enter works immediately.
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Enter);

        var result = new SelectionPrompt<string>()
            .Mode(SelectionMode.Leaf)
            .AddChoiceGroup("GroupA", "Leaf1", "Leaf2")
            .AddChoiceGroup("GroupB", "Leaf3", "Leaf4")
            .DefaultValue("GroupA")      // group, not a leaf
            .Show(console);

        result.Should().Be("Leaf1");
    }

    [Fact]
    public void Should_Pre_Position_Cursor_On_Leaf_Default_In_Leaf_Mode()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Enter);

        var result = new SelectionPrompt<string>()
            .Mode(SelectionMode.Leaf)
            .AddChoiceGroup("GroupA", "Leaf1", "Leaf2")
            .AddChoiceGroup("GroupB", "Leaf3", "Leaf4")
            .DefaultValue("Leaf3")
            .Show(console);

        result.Should().Be("Leaf3");
    }

    [Fact]
    public void Should_Work_With_Value_Type_Choices()
    {
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushKey(ConsoleKey.Enter);

        var result = new SelectionPrompt<int>()
            .AddChoices(1, 2, 3, 4, 5)
            .DefaultValue(3)
            .Show(console);

        result.Should().Be(3);
    }

    [Fact]
    public void DefaultValue_Extension_Should_Throw_For_Null_Prompt()
    {
        var ex = Record.Exception(() =>
            ((SelectionPrompt<string>)null!).DefaultValue("x"));
        ex.Should().BeOfType<ArgumentNullException>()
              .Which.ParamName.Should().Be("obj");
    }
}

file sealed class CustomSelectionItem
{
    public int Value { get; }
    public string Name { get; }

    public CustomSelectionItem(int value, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Value = value;
        Name = name;
    }
}