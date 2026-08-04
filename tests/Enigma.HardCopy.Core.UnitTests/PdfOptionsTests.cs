using System;
using Xunit;

namespace Enigma.HardCopy.Core.UnitTests;

public sealed class PdfOptionsTests
{
    [Fact]
    public void Default_IsTheValidatedConfiguration()
    {
        PdfOptions options = PdfOptions.Default;

        Assert.Equal(QrErrorCorrectionLevel.Medium, options.Qr.ErrorCorrection);
        Assert.Equal(0.35f, options.MinimumModuleSizeInMillimetres);
        Assert.Equal(10f, options.PageMarginInMillimetres);
        Assert.Equal(3f, options.CodeSpacingInMillimetres);
    }

    [Theory]
    [InlineData(PdfOptions.MinModuleSizeInMillimetres)]
    [InlineData(PdfOptions.MaxModuleSizeInMillimetres)]
    [InlineData(0.35f)]
    public void MinimumModuleSizeInMillimetres_WithinRange_IsAccepted(float value)
        => Assert.Equal(value, new PdfOptions { MinimumModuleSizeInMillimetres = value }.MinimumModuleSizeInMillimetres);

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(PdfOptions.MinModuleSizeInMillimetres - 0.01f)]
    [InlineData(PdfOptions.MaxModuleSizeInMillimetres + 0.01f)]
    public void MinimumModuleSizeInMillimetres_OutOfRange_Throws(float value)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new PdfOptions { MinimumModuleSizeInMillimetres = value });

    [Theory]
    [InlineData(PdfOptions.MinPageMarginInMillimetres)]
    [InlineData(PdfOptions.MaxPageMarginInMillimetres)]
    public void PageMarginInMillimetres_WithinRange_IsAccepted(float value)
        => Assert.Equal(value, new PdfOptions { PageMarginInMillimetres = value }.PageMarginInMillimetres);

    [Theory]
    [InlineData(PdfOptions.MinPageMarginInMillimetres - 0.01f)]
    [InlineData(PdfOptions.MaxPageMarginInMillimetres + 0.01f)]
    public void PageMarginInMillimetres_OutOfRange_Throws(float value)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new PdfOptions { PageMarginInMillimetres = value });

    [Theory]
    [InlineData(0f)]
    [InlineData(PdfOptions.MaxCodeSpacingInMillimetres)]
    public void CodeSpacingInMillimetres_WithinRange_IsAccepted(float value)
        => Assert.Equal(value, new PdfOptions { CodeSpacingInMillimetres = value }.CodeSpacingInMillimetres);

    [Theory]
    [InlineData(-0.01f)]
    [InlineData(PdfOptions.MaxCodeSpacingInMillimetres + 0.01f)]
    public void CodeSpacingInMillimetres_OutOfRange_Throws(float value)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new PdfOptions { CodeSpacingInMillimetres = value });

    [Fact]
    public void Qr_Null_Throws()
        => Assert.Throws<ArgumentNullException>(() => new PdfOptions { Qr = null! });

    [Fact]
    public void Qr_CarriesTheGivenRenderOptions()
    {
        QrRenderOptions qr = new() { ErrorCorrection = QrErrorCorrectionLevel.High, PixelsPerModule = 4 };

        Assert.Same(qr, new PdfOptions { Qr = qr }.Qr);
    }

    // A record's value equality is what lets a ViewModel compare a pending configuration against the applied
    // one without writing a comparer.
    [Fact]
    public void Equality_IsByValue()
    {
        PdfOptions options = new() { MinimumModuleSizeInMillimetres = 0.5f };

        Assert.Equal(options, new PdfOptions { MinimumModuleSizeInMillimetres = 0.5f });
        Assert.NotEqual(options, PdfOptions.Default);
    }
}
