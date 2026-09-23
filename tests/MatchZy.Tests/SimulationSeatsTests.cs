using MatchZy;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MatchZy.Tests;

public class SimulationSeatsTests
{
    private static JObject Players(string json) => JObject.Parse(json);

    [Fact]
    public void AStringEntryIsANameAndABotsSeat()
    {
        var players = Players("""{ "76561198000000001": "tk" }""");
        var entry = players["76561198000000001"];
        Assert.Equal("tk", SimulationSeats.NameOf(entry));
        Assert.True(SimulationSeats.IsSimulated(entry));
        Assert.False(SimulationSeats.HasHumanSeat(players));
    }

    [Fact]
    public void AnObjectEntryWithSimulatedFalseIsAPersonsSeat()
    {
        var players = Players("""{ "76561198000000001": { "name": "tk", "simulated": false } }""");
        var entry = players["76561198000000001"];
        Assert.Equal("tk", SimulationSeats.NameOf(entry));
        Assert.False(SimulationSeats.IsSimulated(entry));
        Assert.True(SimulationSeats.HasHumanSeat(players));
    }

    [Theory]
    [InlineData("""{ "name": "tk" }""")]
    [InlineData("""{ "name": "tk", "simulated": true }""")]
    [InlineData("""{ "name": "tk", "simulated": "false" }""")]
    [InlineData("""{ "name": "tk", "simulated": 0 }""")]
    [InlineData("""{ "name": "tk", "simulated": null }""")]
    public void OnlyABooleanFalseLeavesTheSeatToAPerson(string json)
    {
        var entry = JObject.Parse(json);
        Assert.True(SimulationSeats.IsSimulated(entry));
        Assert.Equal("tk", SimulationSeats.NameOf(entry));
    }

    [Fact]
    public void AnObjectWithoutANameReadsAsEmpty()
    {
        Assert.Equal("", SimulationSeats.NameOf(JObject.Parse("""{ "simulated": false }""")));
        Assert.Equal("", SimulationSeats.NameOf(null));
    }

    [Fact]
    public void OneHumanSeatAmongPuppetsIsFound()
    {
        var players = Players("""
            {
              "76561198000000001": "a",
              "76561198000000002": { "name": "b", "simulated": true },
              "76561198000000003": { "name": "tk", "simulated": false }
            }
            """);
        Assert.True(SimulationSeats.HasHumanSeat(players));
        Assert.Equal(new[] { "76561198000000003" },
            players.Properties().Where(p => !SimulationSeats.IsSimulated(p.Value)).Select(p => p.Name));
    }

    [Fact]
    public void NoRosterHasNoHumanSeat()
    {
        Assert.False(SimulationSeats.HasHumanSeat(null));
        Assert.False(SimulationSeats.HasHumanSeat(new JArray("tk")));
    }
}

public class SimulationWatchdogForceStartTests
{
    [Theory]
    [InlineData(1, false, false)]
    [InlineData(2, false, true)]
    [InlineData(5, false, true)]
    [InlineData(2, true, false)]
    [InlineData(50, true, false)]
    public void NeverForceStartsARosterWithAPersonsSeat(int attempts, bool humans, bool expected)
    {
        Assert.Equal(expected, SimulationRosterLogic.ShouldWatchdogForceStart(attempts, humans));
    }
}
