namespace FlexRadioServices.Utils;

public static class BandConverter
{
    /// <summary>
    /// Converts frequency in hz to band
    /// </summary>
    /// <param name="freq"></param>
    /// <returns></returns>
    public static int ConvertToBand(double freq)
    {
        if (freq is >= 135 and <= 138)
            return 2190;
        if (freq is >= 1800 and <= 2000)
            return 160;
        if (freq is >= 3500 and <= 4000)
            return 80;
        if (freq is >= 5000 and <= 5500)
            return 60;
        if (freq is >= 7000 and <= 7300)
            return 40;
        if (freq is >= 10100 and <= 10150)
            return 30;
        if (freq is >= 14000 and <= 14350)
            return 20;
        if (freq is >= 18068 and <= 18268)
            return 17;
        if (freq is >= 21000 and <= 21450)
            return 15;
        if (freq is >= 24890 and <= 24990)
            return 12;
        if (freq is >= 28000 and <= 29700)
            return 10;
        if (freq is >= 50000 and <= 54000)
            return 6;
        if (freq is >= 70000 and <= 71000)
            return 4;
        if (freq is >= 144000 and <= 148000)
            return 2;

        return 0;
    }
}