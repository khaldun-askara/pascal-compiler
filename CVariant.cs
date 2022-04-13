public enum VariantType
{
    vtInt,
    vtReal,
    vtString,
    vtBoolean
}

public abstract class CVariant
{
    private VariantType variantType;

    public CVariant(VariantType variantType)
    {
        this.variantType = variantType;
    }

    public VariantType VariantType { get => variantType; }

    public abstract override string ToString();
}

class CIntVariant : CVariant
{
    int value;

    public CIntVariant(int value) : base(VariantType.vtInt)
    {
        this.value = value;
    }

    public int Value { get => value; }

    public override string ToString()
    {
        return value.ToString();
    }
}

class CRealVariant : CVariant
{
    double value;
    public CRealVariant(double value) : base(VariantType.vtReal)
    {
        this.value = value;
    }

    public double Value { get => value; }

    public override string ToString()
    {
        return value.ToString();
    }
}

class CBooleanVariant : CVariant
{
    bool value;

    public CBooleanVariant(bool value) : base(VariantType.vtBoolean)
    {
        this.value = value;
    }

    public bool Value { get => value; }

    public override string ToString()
    {
        return value.ToString();
    }
}

class CStringVariant : CVariant
{
    string value;

    public CStringVariant(string value) : base(VariantType.vtString)
    {
        this.value = value;
    }

    public string Value { get => value; }

    public override string ToString()
    {
        return value;
    }
}