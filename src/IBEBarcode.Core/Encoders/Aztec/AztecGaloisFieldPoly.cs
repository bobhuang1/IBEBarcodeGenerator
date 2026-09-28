namespace IBEBarcode.Core.Encoders.Aztec;

internal sealed class AztecGaloisFieldPoly
{
    private readonly AztecGaloisField _field;

    public int[] Coefficients { get; }

    public int Degree => Coefficients.Length - 1;

    public bool IsZero => Coefficients[0] == 0;

    public AztecGaloisFieldPoly(AztecGaloisField field, int[] coefficients)
    {
        _field = field;

        if (coefficients.Length > 1 && coefficients[0] == 0)
        {
            var firstNonZero = 1;

            while (firstNonZero < coefficients.Length && coefficients[firstNonZero] == 0)
            {
                firstNonZero++;
            }

            Coefficients = firstNonZero == coefficients.Length
                ? new[] { 0 }
                : coefficients[firstNonZero..];
        }
        else
        {
            Coefficients = coefficients;
        }
    }

    public int GetCoefficient(int degree) => Coefficients[Coefficients.Length - 1 - degree];

    public AztecGaloisFieldPoly AddOrSubtract(AztecGaloisFieldPoly other)
    {
        if (IsZero)
        {
            return other;
        }

        if (other.IsZero)
        {
            return this;
        }

        var smaller = Coefficients;
        var larger = other.Coefficients;

        if (smaller.Length > larger.Length)
        {
            (smaller, larger) = (larger, smaller);
        }

        var result = new int[larger.Length];
        var lengthDiff = larger.Length - smaller.Length;
        Array.Copy(larger, result, lengthDiff);

        for (var i = lengthDiff; i < larger.Length; i++)
        {
            result[i] = smaller[i - lengthDiff] ^ larger[i];
        }

        return new AztecGaloisFieldPoly(_field, result);
    }

    public AztecGaloisFieldPoly Multiply(AztecGaloisFieldPoly other)
    {
        if (IsZero || other.IsZero)
        {
            return new AztecGaloisFieldPoly(_field, new[] { 0 });
        }

        var a = Coefficients;
        var b = other.Coefficients;
        var product = new int[a.Length + b.Length - 1];

        for (var i = 0; i < a.Length; i++)
        {
            for (var j = 0; j < b.Length; j++)
            {
                product[i + j] ^= _field.Multiply(a[i], b[j]);
            }
        }

        return new AztecGaloisFieldPoly(_field, product);
    }

    public AztecGaloisFieldPoly MultiplyByMonomial(int degree, int coefficient)
    {
        if (coefficient == 0)
        {
            return new AztecGaloisFieldPoly(_field, new[] { 0 });
        }

        var product = new int[Coefficients.Length + degree];

        for (var i = 0; i < Coefficients.Length; i++)
        {
            product[i] = _field.Multiply(Coefficients[i], coefficient);
        }

        return new AztecGaloisFieldPoly(_field, product);
    }

    public AztecGaloisFieldPoly[] Divide(AztecGaloisFieldPoly other)
    {
        var quotient = new AztecGaloisFieldPoly(_field, new[] { 0 });
        var remainder = this;

        var denominatorLeadingTerm = other.GetCoefficient(other.Degree);
        var inverseDenominatorLeadingTerm = _field.Inverse(denominatorLeadingTerm);

        while (remainder.Degree >= other.Degree && !remainder.IsZero)
        {
            var degreeDifference = remainder.Degree - other.Degree;
            var scale = _field.Multiply(remainder.GetCoefficient(remainder.Degree), inverseDenominatorLeadingTerm);
            var term = other.MultiplyByMonomial(degreeDifference, scale);
            var iterationQuotient = BuildMonomial(degreeDifference, scale);
            quotient = quotient.AddOrSubtract(iterationQuotient);
            remainder = remainder.AddOrSubtract(term);
        }

        return new[] { quotient, remainder };
    }

    private AztecGaloisFieldPoly BuildMonomial(int degree, int coefficient)
    {
        if (coefficient == 0)
        {
            return new AztecGaloisFieldPoly(_field, new[] { 0 });
        }

        var coefficients = new int[degree + 1];
        coefficients[0] = coefficient;
        return new AztecGaloisFieldPoly(_field, coefficients);
    }
}
