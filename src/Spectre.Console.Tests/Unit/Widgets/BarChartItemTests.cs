namespace Spectre.Console.Tests.Unit;

public sealed class BarChartItemTests
{
    public sealed class Constructor
    {
        [Fact]
        public void Should_Throw_For_Null_Label()
        {
            var ex = Record.Exception(() => new BarChartItem(null!, 10.0));
            ex.Should().BeOfType<ArgumentNullException>()
                  .Which.ParamName.Should().Be("label");
        }

        [Fact]
        public void Should_Set_Label()
        {
            var item = new BarChartItem("Apple", 12.0);
            item.Label.Should().Be("Apple");
        }

        [Fact]
        public void Should_Set_Value()
        {
            var item = new BarChartItem("Apple", 42.5);
            item.Value.Should().Be(42.5);
        }

        [Fact]
        public void Should_Set_Color()
        {
            var item = new BarChartItem("Apple", 12.0, Color.Red);
            item.Color.Should().Be(Color.Red);
        }

        [Fact]
        public void Should_Default_Color_To_Null()
        {
            var item = new BarChartItem("Apple", 12.0);
            item.Color.Should().BeNull();
        }
    }

    public sealed class LabelProperty
    {
        [Fact]
        public void Setter_Should_Update_Label()
        {
            var item = new BarChartItem("Apple", 12.0);
            item.Label = "Pear";
            item.Label.Should().Be("Pear");
        }

        [Fact]
        public void Setter_Should_Throw_For_Null()
        {
            var item = new BarChartItem("Apple", 12.0);
            var ex = Record.Exception(() => item.Label = null!);
            ex.Should().BeOfType<ArgumentNullException>()
                  .Which.ParamName.Should().Be("value");
        }

        [Fact]
        public void Setter_Should_Accept_Empty_String()
        {
            var item = new BarChartItem("Apple", 12.0);
            item.Label = string.Empty;
            item.Label.Should().Be(string.Empty);
        }

        [Fact]
        public void Getter_Should_Reflect_Setter_Through_Interface()
        {
            IBarChartItem item = new BarChartItem("Apple", 12.0);
            ((BarChartItem)item).Label = "Mango";
            item.Label.Should().Be("Mango");
        }
    }

    public sealed class ValueProperty
    {
        [Fact]
        public void Setter_Should_Update_Value()
        {
            var item = new BarChartItem("Apple", 12.0);
            item.Value = 99.9;
            item.Value.Should().Be(99.9);
        }

        [Fact]
        public void Setter_Should_Accept_Zero()
        {
            var item = new BarChartItem("Apple", 12.0);
            item.Value = 0.0;
            item.Value.Should().Be(0.0);
        }

        [Fact]
        public void Setter_Should_Accept_Negative_Value()
        {
            // Negative values are permitted; the renderer clamps via MaxValue
            var item = new BarChartItem("Apple", 12.0);
            item.Value = -5.0;
            item.Value.Should().Be(-5.0);
        }

        [Fact]
        public void Getter_Should_Reflect_Setter_Through_Interface()
        {
            IBarChartItem item = new BarChartItem("Apple", 12.0);
            ((BarChartItem)item).Value = 77.0;
            item.Value.Should().Be(77.0);
        }
    }

    public sealed class ColorProperty
    {
        [Fact]
        public void Setter_Should_Update_Color()
        {
            var item = new BarChartItem("Apple", 12.0, Color.Blue);
            item.Color = Color.Green;
            item.Color.Should().Be(Color.Green);
        }

        [Fact]
        public void Setter_Should_Accept_Null()
        {
            var item = new BarChartItem("Apple", 12.0, Color.Red);
            item.Color = null;
            item.Color.Should().BeNull();
        }

        [Fact]
        public void Getter_Should_Reflect_Setter_Through_Interface()
        {
            IBarChartItem item = new BarChartItem("Apple", 12.0, Color.Blue);
            ((BarChartItem)item).Color = Color.Yellow;
            item.Color.Should().Be(Color.Yellow);
        }
    }

    public sealed class LiveScenario
    {
        [Fact]
        public void Items_Added_To_Chart_Should_Reflect_Mutations()
        {
            // Verifies that BarChart.Data holds the same object reference so that
            // mutations to a BarChartItem after AddItem are visible at render time.
            var item = new BarChartItem("Apple", 12.0);
            var chart = new BarChart().AddItem(item);

            item.Value = 99.0;
            item.Label = "Pear";
            item.Color = Color.Red;

            var stored = chart.Data[0] as BarChartItem;
            stored.Should().NotBeNull();
            stored.Value.Should().Be(99.0);
            stored.Label.Should().Be("Pear");
            stored.Color.Should().Be(Color.Red);
        }
    }
}
