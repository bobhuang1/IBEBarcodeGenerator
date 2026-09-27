namespace IBEBarcode.Core;

public readonly struct HeightBar : IEquatable<HeightBar>
{
    public bool IsTall { get; }

    public HeightBar(bool isTall)
    {
        IsTall = isTall;
    }

    public bool Equals(HeightBar other) => IsTall == other.IsTall;

    public override bool Equals(object? obj) => obj is HeightBar other && Equals(other);

    public override int GetHashCode() => IsTall.GetHashCode();
}
