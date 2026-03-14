using FluentAssertions;
using Spectre.Console.Tui;
using Spectre.Console.Tui.Widgets.Controls;
using Xunit;

namespace Spectre.Console.Tui.Tests.Widgets;

public class DataGridTests
{
    [Fact]
    public void AddColumn_Should_Add()
    {
        var dg = new DataGrid();
        dg.AddColumn("Name");
        dg.Columns.Should().ContainSingle().Which.Should().Be("Name");
    }

    [Fact]
    public void AddColumns_Should_Add_Multiple()
    {
        var dg = new DataGrid();
        dg.AddColumns("A", "B", "C");
        dg.Columns.Should().HaveCount(3);
    }

    [Fact]
    public void AddRow_Should_Add_Data()
    {
        var dg = new DataGrid();
        dg.AddColumn("Name");
        dg.AddRow("Alice");
        dg.RowCount.Should().Be(1);
        dg.SelectedRow.Should().Be(0);
    }

    [Fact]
    public void Arrow_Navigation()
    {
        var dg = new DataGrid();
        dg.AddColumn("Name");
        dg.AddRow("A");
        dg.AddRow("B");
        dg.AddRow("C");

        dg.OnKeyEvent(new KeyEvent(ConsoleKey.DownArrow, '\0'));
        dg.SelectedRow.Should().Be(1);

        dg.OnKeyEvent(new KeyEvent(ConsoleKey.UpArrow, '\0'));
        dg.SelectedRow.Should().Be(0);
    }

    [Fact]
    public void Home_And_End()
    {
        var dg = new DataGrid();
        dg.AddColumn("N");
        dg.AddRow("A"); dg.AddRow("B"); dg.AddRow("C");

        dg.OnKeyEvent(new KeyEvent(ConsoleKey.End, '\0'));
        dg.SelectedRow.Should().Be(2);

        dg.OnKeyEvent(new KeyEvent(ConsoleKey.Home, '\0'));
        dg.SelectedRow.Should().Be(0);
    }

    [Fact]
    public void ClearRows_Should_Reset()
    {
        var dg = new DataGrid();
        dg.AddColumn("N");
        dg.AddRow("A");
        dg.ClearRows();
        dg.RowCount.Should().Be(0);
        dg.SelectedRow.Should().Be(-1);
    }

    [Fact]
    public void GetRow_Should_Return_Data()
    {
        var dg = new DataGrid();
        dg.AddColumn("Name");
        dg.AddRow("Alice");
        dg.GetRow(0).Should().BeEquivalentTo(new[] { "Alice" });
        dg.GetRow(-1).Should().BeNull();
        dg.GetRow(1).Should().BeNull();
    }

    [Fact]
    public void Enter_Should_Fire_RowActivated()
    {
        var dg = new DataGrid();
        dg.AddColumn("N");
        dg.AddRow("A");
        var activated = -1;
        dg.RowActivated += (_, i) => activated = i;
        dg.OnKeyEvent(new KeyEvent(ConsoleKey.Enter, '\r'));
        activated.Should().Be(0);
    }
}
