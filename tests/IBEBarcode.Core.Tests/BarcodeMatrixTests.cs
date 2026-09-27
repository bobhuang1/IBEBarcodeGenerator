namespace IBEBarcode.Core.Tests;

public class BarcodeMatrixTests
{
    [Fact]
    public void Create_WithModules_Succeeds()
    {
        var modules = new bool[3, 2];
        modules[0, 0] = true;
        modules[2, 1] = true;

        var matrix = BarcodeMatrix.Create("test", modules);

        Assert.Equal("test", matrix.Value);
        Assert.Equal(3, matrix.Width);
        Assert.Equal(2, matrix.Height);
        Assert.True(matrix[0, 0]);
        Assert.False(matrix[1, 0]);
        Assert.True(matrix[2, 1]);
    }

    [Fact]
    public void Create_WithEmptyDimension_Throws()
    {
        Assert.Throws<ArgumentException>(() => BarcodeMatrix.Create("x", new bool[0, 0]));
    }
}
