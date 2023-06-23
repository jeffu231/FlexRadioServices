namespace FlexRadioServices.Utils.Slice;

public class Filter
{
    public string Name { get; set; }

    public int LowCut { get; set; }

    public int HighCut { get; set; }

    public bool IsFavorite { get; set; }

    public Filter(string name, int low, int high)
    {
        Name = name;
        LowCut = low;
        HighCut = high;
    }
}