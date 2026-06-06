namespace ShareXMac.Models;

public record SampledColor(byte R, byte G, byte B)
{
    public string Hex => $"#{R:X2}{G:X2}{B:X2}";

    public (double H, double S, double V) ToHsv()
    {
        double rf = R / 255.0, gf = G / 255.0, bf = B / 255.0;
        double max = Math.Max(rf, Math.Max(gf, bf));
        double min = Math.Min(rf, Math.Min(gf, bf));
        double delta = max - min;

        double h = 0;
        if (delta > 0)
        {
            if (max == rf)       h = 60 * (gf - bf) / delta;
            else if (max == gf)  h = 60 * ((bf - rf) / delta + 2);
            else                 h = 60 * ((rf - gf) / delta + 4);
            h = ((h % 360) + 360) % 360;
        }
        double s = max == 0 ? 0 : delta / max;
        return (Math.Round(h, MidpointRounding.AwayFromZero),
                Math.Round(s * 100, MidpointRounding.AwayFromZero),
                Math.Round(max * 100, MidpointRounding.AwayFromZero));
    }
}
