namespace IBEBarcode.Core.Encoders.Aztec;

internal sealed class AztecReedSolomonEncoder
{
    private readonly AztecGaloisField _field;
    private readonly List<AztecGaloisFieldPoly> _cachedGenerators;

    public AztecReedSolomonEncoder(AztecGaloisField field)
    {
        _field = field;
        _cachedGenerators = new List<AztecGaloisFieldPoly> { new(field, new[] { 1 }) };
    }

    private AztecGaloisFieldPoly BuildGenerator(int degree)
    {
        if (degree >= _cachedGenerators.Count)
        {
            var lastGenerator = _cachedGenerators[^1];

            for (var d = _cachedGenerators.Count; d <= degree; d++)
            {
                var nextGenerator = lastGenerator.Multiply(
                    new AztecGaloisFieldPoly(_field, new[] { 1, _field.Exp(d - 1 + _field.GeneratorBase) }));
                _cachedGenerators.Add(nextGenerator);
                lastGenerator = nextGenerator;
            }
        }

        return _cachedGenerators[degree];
    }

    public void Encode(int[] toEncode, int ecWords)
    {
        var dataWords = toEncode.Length - ecWords;
        var generator = BuildGenerator(ecWords);

        var infoCoefficients = new int[dataWords];
        Array.Copy(toEncode, infoCoefficients, dataWords);

        var info = new AztecGaloisFieldPoly(_field, infoCoefficients);
        info = info.MultiplyByMonomial(ecWords, 1);

        var remainder = info.Divide(generator)[1];
        var coefficients = remainder.Coefficients;
        var numZeroCoefficients = ecWords - coefficients.Length;

        for (var i = 0; i < numZeroCoefficients; i++)
        {
            toEncode[dataWords + i] = 0;
        }

        Array.Copy(coefficients, 0, toEncode, dataWords + numZeroCoefficients, coefficients.Length);
    }
}
