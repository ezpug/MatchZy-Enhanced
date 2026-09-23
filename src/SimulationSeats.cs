using System.Linq;
using Newtonsoft.Json.Linq;

namespace MatchZy;

/// <summary>
/// ezpug: which roster entries a simulated match seats a bot for.
///
/// An entry in a team's <c>players</c> map is either the player's name, as in every Get5 /
/// MatchZy match file (<c>"7656…": "tk"</c>), or an object that also says whether a bot
/// plays the seat (<c>"7656…": { "name": "tk", "simulated": false }</c>). In simulation mode
/// an absent or <c>true</c> flag is a seat a bot plays, and <c>false</c> is a seat a person
/// takes: no bot is spawned for it, and the person connects and readies the way they would
/// in any match. Outside simulation mode the flag means nothing.
///
/// Only a JSON boolean <c>false</c> leaves a seat to a person. Anything else (absent, true,
/// a string) keeps today's behaviour, so a file written before the flag existed plays exactly
/// as it did.
/// </summary>
public static class SimulationSeats
{
    /// <summary>The player's name from a roster entry of either form.</summary>
    public static string NameOf(JToken? entry)
    {
        return entry switch
        {
            null => "",
            JObject obj => obj["name"]?.ToString() ?? "",
            _ => entry.ToString(),
        };
    }

    /// <summary>Whether a simulated match seats a bot for this roster entry.</summary>
    public static bool IsSimulated(JToken? entry)
    {
        return entry is not JObject obj
            || obj["simulated"] is not { Type: JTokenType.Boolean } flag
            || flag.Value<bool>();
    }

    /// <summary>Whether a team's <c>players</c> map leaves any seat to a person.</summary>
    public static bool HasHumanSeat(JToken? teamPlayers)
    {
        return teamPlayers is JObject obj && obj.Properties().Any(p => !IsSimulated(p.Value));
    }
}
