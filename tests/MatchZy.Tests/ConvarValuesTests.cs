using System.Globalization;
using CounterStrikeSharp.API;
using MatchZy;
using Xunit;

namespace MatchZy.Tests;

/// <summary>
/// ezpug: a cvar's value as the text MatchZy records, and a text written back
/// (<see cref="ConvarValues"/>). The engine is not here. What is here is the part that can
/// break: which CLR type each engine type is read and written as. The fake refuses a wrong
/// type the way CounterStrikeSharp 1.0.381's <c>ConVar&lt;T&gt;.Value</c> does, so an arm
/// asking for the wrong type is red here, not an empty string on a server.
/// </summary>
public class ConvarValuesTests
{
    /// <summary>A cvar holding one value of one CLR type, which is the only <c>T</c> it accepts.</summary>
    private sealed class Cvar(object value) : ConvarValues.ICvarValue
    {
        public object Value { get; private set; } = value;

        public int Touches { get; private set; }

        public T Read<T>()
        {
            Touches++;
            return Value is T typed
                ? typed
                : throw new InvalidOperationException($"ConVar is a {Value.GetType()} but you are trying to get a {typeof(T)} value.");
        }

        public void Write<T>(T value)
        {
            Touches++;
            if (value is null || value.GetType() != Value.GetType())
                throw new InvalidOperationException($"ConVar is a {Value.GetType()} but you are trying to set a {typeof(T)} value.");
            Value = value;
        }

        public string Text
        {
            get
            {
                Touches++;
                return (string)Value;
            }
        }
    }

    /// <summary>Runs <paramref name="body"/> in the invariant culture, which the text formats assume.</summary>
    private static void Invariant(Action body)
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try { body(); }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    public static TheoryData<ConVarType, object, string> Typed => new()
    {
        { ConVarType.Bool, true, "True" },
        { ConVarType.Bool, false, "False" },
        { ConVarType.Int16, (short)-7, "-7" },
        { ConVarType.UInt16, (ushort)65535, "65535" },
        { ConVarType.Int32, 115, "115" },
        { ConVarType.UInt32, 4_000_000_000u, "4000000000" },
        { ConVarType.Int64, -9_000_000_000L, "-9000000000" },
        { ConVarType.UInt64, 18_000_000_000_000_000_000ul, "18000000000000000000" },
        { ConVarType.Float32, 0.5f, "0.5" },
        { ConVarType.Float64, 0.125d, "0.125" },
        { ConVarType.String, "EZPug · pug · Mirage", "EZPug · pug · Mirage" },
    };

    [Theory]
    [MemberData(nameof(Typed))]
    public void EachEngineTypeIsReadAsItsOwnClrType(ConVarType type, object value, string expected)
    {
        Invariant(() => Assert.Equal(expected, ConvarValues.Format(type, new Cvar(value))));
    }

    [Theory]
    [MemberData(nameof(Typed))]
    public void WhatIsRecordedIsWrittenBackAsTheSameValue(ConVarType type, object value, string text)
    {
        Invariant(() =>
        {
            var cvar = new Cvar(DefaultOf(value));
            Assert.True(ConvarValues.Assign(type, cvar, text));
            Assert.Equal(value, cvar.Value);
        });
    }

    [Fact]
    public void AFloat64IsADoubleNotAFloat()
    {
        // The untyped ConVar read and wrote a Float64 cvar as a float; ConVar<double> refuses that.
        Invariant(() =>
        {
            Assert.Throws<InvalidOperationException>(() => ConvarValues.Format(ConVarType.Float64, new Cvar(0.5f)));
            var cvar = new Cvar(0d);
            ConvarValues.Assign(ConVarType.Float64, cvar, "0.1");
            Assert.Equal(0.1d, cvar.Value);
        });
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("2", true)]
    [InlineData("true", true)]
    [InlineData("False", false)]
    public void ABoolIsAnIntegerOfOneOrMoreOrABooleanWord(string text, bool expected)
    {
        var cvar = new Cvar(!expected);
        Assert.True(ConvarValues.Assign(ConVarType.Bool, cvar, text));
        Assert.Equal(expected, cvar.Value);
    }

    [Fact]
    public void ABoolOfZeroThrowsAsUpstreamsAlwaysHas()
    {
        // "0" parses as an integer below one, so the expression falls through to
        // Convert.ToBoolean("0"), which throws. Upstream's rule, kept as it is.
        Assert.Throws<FormatException>(() => ConvarValues.Assign(ConVarType.Bool, new Cvar(true), "0"));
    }

    [Theory]
    [InlineData(ConVarType.Color)]
    [InlineData(ConVarType.Vector2)]
    [InlineData(ConVarType.Vector3)]
    [InlineData(ConVarType.Vector4)]
    [InlineData(ConVarType.Qangle)]
    [InlineData(ConVarType.Invalid)]
    public void ATypeWithNoTextFormIsEmptyAndNeverTouched(ConVarType type)
    {
        var cvar = new Cvar(new object());
        Assert.Equal("", ConvarValues.Format(type, cvar));
        Assert.False(ConvarValues.Assign(type, cvar, "1"));
        Assert.Equal(0, cvar.Touches);
    }

    [Fact]
    public void AValueOfAnotherTypeThrowsForTheCallerToCatch()
    {
        // GetConvarStringValue and SetConvarValue log it and go on, as they always have.
        Assert.Throws<InvalidOperationException>(() => ConvarValues.Format(ConVarType.Int32, new Cvar(1.5f)));
        Assert.Throws<InvalidOperationException>(() => ConvarValues.Assign(ConVarType.Int32, new Cvar(1.5f), "3"));
        Assert.Throws<FormatException>(() => ConvarValues.Assign(ConVarType.Int32, new Cvar(0), "three"));
    }

    private static object DefaultOf(object value) => value switch
    {
        string => "",
        _ => Activator.CreateInstance(value.GetType())!,
    };
}
