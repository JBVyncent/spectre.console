namespace Spectre.Console.Json.Tests;

public sealed class JsonBuilderTests
{
    private static string Render(string json)
    {
        using var console = new TestConsole();
        console.Write(new JsonText(json));
        return console.Output;
    }

    // ── Object newlines ───────────────────────────────────────────────────────

    [Fact]
    public void VisitObject_OpenBrace_FollowedByNewline()
    {
        // Kills line 28 String ("\n"→"") and Statement (remove Append call) mutations
        var output = Render("{\"k\":1}");
        output.Should().Contain("{\n");
    }

    [Fact]
    public void VisitObject_Member_FollowedByNewline()
    {
        // Kills line 41 Statement mutation (remove Append("\n") after member)
        var output = Render("{\"k\":1}");
        output.Should().Contain("1\n");
    }

    [Fact]
    public void VisitObject_ClosingBrace_ImmediatelyAfterNewline()
    {
        // Kills line 44 PostDecrement→PostIncrement mutation (extra indentation before })
        var output = Render("{\"k\":1}");
        output.Should().Contain("\n}");
    }

    // ── Object indentation ────────────────────────────────────────────────────

    [Fact]
    public void VisitObject_LevelOne_ThreeSpacesBeforeMember()
    {
        // Kills line 17 Arithmetic (*3→/3,+3,-3) and line 33 Statement mutations
        var output = Render("{\"k\":1}");
        output.Should().Contain("\n   \"k\"");
    }

    [Fact]
    public void VisitObject_LevelTwo_SixSpacesBeforeInnerMember()
    {
        // Confirms cumulative indentation is multiplicative (level 2 = 6 spaces)
        var output = Render("{\"a\":{\"b\":1}}");
        output.Should().Contain("\n      \"b\"");
    }

    [Fact]
    public void VisitObject_NestedObject_InnerClosingBraceThreeSpaces()
    {
        // Kills line 45 Statement mutation (remove InsertIndentation before inner })
        var output = Render("{\"a\":{\"b\":1}}");
        output.Should().Contain("\n   }");
    }

    [Fact]
    public void VisitObject_TopLevel_ClosingBraceNoLeadingSpaces()
    {
        // Kills line 44 PostDecrement→PostIncrement on top-level object
        var output = Render("{}");
        output.Should().Contain("\n}");
        output.Should().NotMatchRegex(@"\n[ ]+}");
    }

    // ── Object member formatting ──────────────────────────────────────────────

    [Fact]
    public void VisitMember_NameColonSpaceValue_AllPresent()
    {
        // Kills lines 75, 76, 77 Statement mutations (name/colon/space not appended)
        var output = Render("{\"key\":42}");
        output.Should().Contain("\"key\": 42");
    }

    [Fact]
    public void VisitMember_ColonAfterName()
    {
        // Kills line 76 Statement mutation (colon not appended)
        var output = Render("{\"key\":42}");
        output.Should().Contain("\"key\":");
    }

    [Fact]
    public void VisitMember_SpaceAfterColon()
    {
        // Kills line 77 Statement mutation (space not appended)
        var output = Render("{\"key\":42}");
        output.Should().Contain(": 42");
    }

    // ── Object comma ──────────────────────────────────────────────────────────

    [Fact]
    public void VisitObject_MultipleMembers_CommaFollowedByNewline()
    {
        var output = Render("{\"a\":1,\"b\":2}");
        output.Should().Contain("1,\n");
    }

    [Fact]
    public void VisitObject_LastMember_NoTrailingComma()
    {
        var output = Render("{\"a\":1,\"b\":2}");
        output.Should().NotMatchRegex(@"2,\s*}");
    }

    // ── Array newlines ────────────────────────────────────────────────────────

    [Fact]
    public void VisitArray_OpenBracket_FollowedByNewline()
    {
        // Kills line 52 String ("\n"→"") and Statement mutations
        var output = Render("[1]");
        output.Should().Contain("[\n");
    }

    [Fact]
    public void VisitArray_Item_FollowedByNewline()
    {
        // Kills line 65 Statement mutation (remove Append("\n") after item)
        var output = Render("[1]");
        output.Should().Contain("1\n");
    }

    [Fact]
    public void VisitArray_ClosingBracket_ImmediatelyAfterNewline()
    {
        // Kills line 68 PostDecrement→PostIncrement mutation (extra indentation before ])
        var output = Render("[1]");
        output.Should().Contain("\n]");
    }

    // ── Array indentation ─────────────────────────────────────────────────────

    [Fact]
    public void VisitArray_LevelOne_ThreeSpacesBeforeItem()
    {
        // Kills line 57 Statement (no InsertIndentation) and line 17 Arithmetic mutations
        var output = Render("[1]");
        output.Should().Contain("\n   1");
    }

    [Fact]
    public void VisitArray_LevelTwo_SixSpacesBeforeInnerItem()
    {
        var output = Render("[[1]]");
        output.Should().Contain("\n      1");
    }

    [Fact]
    public void VisitArray_NestedArray_InnerClosingBracketThreeSpaces()
    {
        // Kills line 69 Statement mutation (no InsertIndentation before inner ])
        var output = Render("[[1]]");
        output.Should().Contain("\n   ]");
    }

    [Fact]
    public void VisitArray_TopLevel_ClosingBracketNoLeadingSpaces()
    {
        // Kills line 68 PostDecrement→PostIncrement on top-level array
        var output = Render("[]");
        output.Should().Contain("\n]");
        output.Should().NotMatchRegex(@"\n[ ]+]");
    }

    // ── Array comma ───────────────────────────────────────────────────────────

    [Fact]
    public void VisitArray_MultipleItems_CommaFollowedByNewline()
    {
        var output = Render("[1,2]");
        output.Should().Contain("1,\n");
    }

    // ── Cross-level indentation ───────────────────────────────────────────────

    [Fact]
    public void IndentationLevel_ObjectInArray_CorrectSpacing()
    {
        // Object at array level 1: { at 3 spaces; member at 6 spaces; } at 3 spaces
        var output = Render("[{\"k\":1}]");
        output.Should().Contain("\n   {");
        output.Should().Contain("\n      \"k\"");
        output.Should().Contain("\n   }");
    }

    [Fact]
    public void IndentationLevel_ThreeLevels_NineSpacesAtLevelThree()
    {
        var output = Render("{\"a\":{\"b\":{\"c\":1}}}");
        output.Should().Contain("\n         \"c\""); // 9 spaces
    }
}
