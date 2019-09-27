static class Format
{
    public static string Money(decimal value) => "$" + decimal.Truncate(value*100m)/100m;
}
