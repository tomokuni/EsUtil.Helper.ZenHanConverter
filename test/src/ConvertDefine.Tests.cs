using System.Linq;
using Xunit;
using EsUtil.Helper.ZenHanConverter;


namespace EsUtil.Helper.ZenHanConverterTest;

public class ConvertDefineTests
{
    [Fact]
    public void AllList_先頭エントリがCSVと一致()
    {
        var first = Define.AllList.First();

        Assert.Equal("Ascii", first.Category);
        Assert.Equal("Numeric", first.Group);
        Assert.Equal("Number", first.SubGroup);
        Assert.Equal("ToHan", first.Forward);
        Assert.Equal("ToZen", first.Inverse);
        Assert.Equal("０", first.Source);
        Assert.Equal("0", first.Target);
        Assert.Equal("n0", first.Name);
    }

    [Fact]
    public void Ascii_ZenToHan_数値変換()
    {
        var list = GroupOf.Ascii.ToHanMap;

        var actual = list.Convert("０１２３４５６７８９０");

        Assert.Equal("01234567890", actual);
    }

    [Fact]
    public void Ascii_HanToZen_数値変換()
    {
        var list = GroupOf.Ascii.ToZenMap;

        var actual = list.Convert("123");

        Assert.Equal("１２３", actual);
    }

    [Fact]
    public void Ascii_ZenToHan_英字変換()
    {
        var list = GroupOf.Ascii.ToHanMap;

        var actual = list.Convert("ＡＢｃＤｅ");

        Assert.Equal("ABcDe", actual);
    }

    [Fact]
    public void Ascii_UpperToLower_全角大文字を小文字へ()
    {
        var list = GroupOf.Ascii.ToLowerMap;

        var actual = list.Convert("ＡＢＣＺ");

        Assert.Equal("ａｂｃｚ", actual);
    }

    [Fact]
    public void Ascii_UpperToLower_半角大文字を小文字へ()
    {
        var list = GroupOf.Ascii.ToLowerMap;

        var actual = list.Convert("ABCZ");

        Assert.Equal("abcz", actual);
    }

    [Fact]
    public void Kana_KataToHira_カタカナをひらがなへ()
    {
        var list = GroupOf.Kana.ToHiraMap;

        var actual = list.Convert("カタカナ");

        Assert.Equal("かたかな", actual);
    }

    [Fact]
    public void Ascii_Comma_ZenToHan()
    {
        var list = NameOf.Ascii.Comma.ToHanMap;

        var actual = list.Convert("，，");

        Assert.Equal(",,", actual);
    }

    [Fact]
    public void Ascii_ZenToHan_円記号とバックスラッシュ()
    {
        var list = GroupOf.Ascii.ToHanMap;

        var actual = list.Convert("￥＼");

        Assert.Equal("¥\\", actual);
    }

    [Fact]
    public void Ascii_YenToBS_円記号とバックスラッシュの相互変換()
    {
        var list = GroupOf.Ascii.ToBslashFromYenMap;

        var actual = list.Convert("￥¥");

        Assert.Equal("＼\\", actual);
    }

    [Fact]
    public void Kana_Prolong_長音記号とハイフン()
    {
        var list = NameOf.Kana.Prolong.ToAsciiMap;

        var actual = list.Convert("ーーー");

        Assert.Equal("---", actual);
    }

    [Fact]
    public void Ascii_N5_ZenToHan_個別定義を取得()
    {
        var list = NameOf.Ascii.n5.ToHanMap;

        var actual = list.Convert("５５");

        Assert.Equal("55", actual);
    }

    [Fact]
    public void Ascii_N6_ZenToHan_キャッシュ同一インスタンス()
    {
        var first = NameOf.Ascii.n6.ToHanMap;
        var second = NameOf.Ascii.n6.ToHanMap;

        Assert.Same(first, second);
    }
}
