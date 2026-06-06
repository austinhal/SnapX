using ShareX.HelpersLib;
using ShareXMac.Models;
using Xunit;

namespace ShareXMac.Tests;

public class KeyComboHelperTests
{
    [Fact]
    public void ToString_ReturnsEmpty_ForNull()
        => Assert.Equal("", KeyComboHelper.ToString(null));

    [Fact]
    public void ToString_FormatsWithModifiers()
        => Assert.Equal("Cmd+Shift+3", KeyComboHelper.ToString(new KeyCombo("Cmd+Shift", "3")));

    [Fact]
    public void ToString_FormatsWithoutModifiers()
        => Assert.Equal("F5", KeyComboHelper.ToString(new KeyCombo("", "F5")));

    [Fact]
    public void Parse_ReturnsNull_ForEmpty()
    {
        Assert.Null(KeyComboHelper.Parse(""));
        Assert.Null(KeyComboHelper.Parse(null));
    }

    [Fact]
    public void Parse_ExtractsModifiersAndKey()
    {
        var combo = KeyComboHelper.Parse("Cmd+Shift+3");
        Assert.Equal("Cmd+Shift", combo!.Modifiers);
        Assert.Equal("3", combo.Key);
    }

    [Fact]
    public void Parse_HandlesKeyWithNoModifiers()
    {
        var combo = KeyComboHelper.Parse("F5");
        Assert.Equal("", combo!.Modifiers);
        Assert.Equal("F5", combo.Key);
    }

    [Fact]
    public void RoundTrip_PreservesCombo()
    {
        var original = new KeyCombo("Cmd+Shift", "4");
        Assert.Equal(original, KeyComboHelper.Parse(KeyComboHelper.ToString(original)));
    }
}
