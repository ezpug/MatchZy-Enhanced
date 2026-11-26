using CounterStrikeSharp.API;

namespace MatchZy;

/// <summary>
/// ezpug: an engine cvar's value as text, and a text written back, by the cvar's engine type.
///
/// MatchZy records the value of every cvar a match file changes (<c>GetConvarStringValue</c>)
/// and can write one back from text (<c>SetConvarValue</c>). Both pick the CLR type to read or
/// write from the cvar's <see cref="ConVarType"/>. CounterStrikeSharp 1.0.379 replaced the
/// untyped <c>ConVar</c> (obsolete since then) with <c>ConVar&lt;T&gt;</c>, whose <c>Value</c>
/// refuses a <c>T</c> that is not the engine type's, so this table is the thing that has to be
/// right. It is pure so the tests can hold it without a game server.
///
/// The text is what it always was: <c>ToString()</c> and <c>Convert.To…</c> in the current
/// culture, a bool as <c>True</c>/<c>False</c>, and nothing for a vector, an angle, a colour
/// or an unknown type. One arm differs on purpose: a <c>Float64</c> cvar is read and written
/// as a <c>double</c>, which <c>ConVar&lt;double&gt;</c> requires, where the untyped API read
/// and wrote it as a <c>float</c>.
/// </summary>
public static class ConvarValues
{
    /// <summary>A cvar's value, read or written as one CLR type: the seam over <c>ConVarBase.As&lt;T&gt;()</c>.</summary>
    public interface ICvarValue
    {
        /// <summary>The value as <typeparamref name="T"/>; throws when the cvar is not one.</summary>
        T Read<T>();

        /// <summary>Writes the value as <typeparamref name="T"/>; throws when the cvar is not one.</summary>
        void Write<T>(T value);

        /// <summary>The value as the engine prints it (<c>ConVarBase.ValueAsString</c>).</summary>
        string Text { get; }
    }

    /// <summary>The value of a cvar of <paramref name="type"/> as MatchZy records it.</summary>
    public static string Format(ConVarType type, ICvarValue cvar) => type switch
    {
        ConVarType.Bool => cvar.Read<bool>().ToString(),
        ConVarType.Float32 => cvar.Read<float>().ToString(),
        ConVarType.Float64 => cvar.Read<double>().ToString(),
        ConVarType.UInt16 => cvar.Read<ushort>().ToString(),
        ConVarType.Int16 => cvar.Read<short>().ToString(),
        ConVarType.UInt32 => cvar.Read<uint>().ToString(),
        ConVarType.Int32 => cvar.Read<int>().ToString(),
        ConVarType.Int64 => cvar.Read<long>().ToString(),
        ConVarType.UInt64 => cvar.Read<ulong>().ToString(),
        ConVarType.String => cvar.Text,
        _ => "",
    };

    /// <summary>
    /// Writes <paramref name="value"/> to a cvar of <paramref name="type"/>, parsed as that
    /// type. A bool is true for an integer of 1 or more, else whatever <c>Convert.ToBoolean</c>
    /// says (so <c>"0"</c> throws, as it always has). <c>false</c> when the type has no text
    /// form, and nothing is written; a value that does not parse throws.
    /// </summary>
    public static bool Assign(ConVarType type, ICvarValue cvar, string value)
    {
        switch (type)
        {
            case ConVarType.Bool: cvar.Write(int.TryParse(value, out int intValue) && intValue >= 1 || Convert.ToBoolean(value)); return true;
            case ConVarType.Float32: cvar.Write(Convert.ToSingle(value)); return true;
            case ConVarType.Float64: cvar.Write(Convert.ToDouble(value)); return true;
            case ConVarType.UInt16: cvar.Write(Convert.ToUInt16(value)); return true;
            case ConVarType.Int16: cvar.Write(Convert.ToInt16(value)); return true;
            case ConVarType.UInt32: cvar.Write(Convert.ToUInt32(value)); return true;
            case ConVarType.Int32: cvar.Write(Convert.ToInt32(value)); return true;
            case ConVarType.Int64: cvar.Write(Convert.ToInt64(value)); return true;
            case ConVarType.UInt64: cvar.Write(Convert.ToUInt64(value)); return true;
            case ConVarType.String: cvar.Write(value); return true;
            default: return false;
        }
    }
}
