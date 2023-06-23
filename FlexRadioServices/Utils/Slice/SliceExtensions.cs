namespace FlexRadioServices.Utils.Slice;

public static class SliceExtensions
{
    public static int GetClosestPresetFilterIndex(this Flex.Smoothlake.FlexLib.Slice slice)
    {
      if (slice.DemodMode == "FM" || slice.DemodMode == "NFM")
        return 7;
      Filter[] array;
      switch (slice.DemodMode)
      {
        case "AM":
        case "DSB":
        case "SAM":
          array = PresetFilters.PresetFiltersAmDsb;
          break;
        case "CW":
          array = PresetFilters.PresetFiltersCw;
          break;
        case "DFM":
        case "DSTR":
          array = PresetFilters.PresetFiltersDfm;
          break;
        case "DIGL":
          array = PresetFilters.PresetFiltersDigl;
          break;
        case "DIGU":
        case "FDV":
          array = PresetFilters.PresetFiltersDigu;
          break;
        case "LSB":
          array = PresetFilters.PresetFiltersLsb;
          break;
        case "RTTY":
          array = PresetFilters.PresetFiltersRtty;
          break;
        default:
          array = PresetFilters.PresetFiltersUsb;
          break;
      }
      int presetFilterIndex;
      try
      {
        if (slice.DemodMode == "CW" || slice.DemodMode.Contains("DIG") || slice.DemodMode == "RTTY" || slice.DemodMode == "FDV")
        {
          int current_bandwidth = Math.Abs(slice.FilterHigh - slice.FilterLow);
          presetFilterIndex = Array.FindIndex(array, (Predicate<Filter>) 
            (x => Math.Abs(Math.Abs(x.LowCut - x.HighCut) - current_bandwidth) <= 2));
        }
        else
          presetFilterIndex = Array.FindIndex(array, (Predicate<Filter>) 
            (x => Math.Abs(x.LowCut - slice.FilterLow) <= 2 && Math.Abs(x.HighCut - slice.FilterHigh) <= 2));
      }
      catch
      {
        presetFilterIndex = -1;
      }
      if (presetFilterIndex < 0)
      {
        int num1 = Math.Abs(slice.FilterHigh - slice.FilterLow);
        int num2 = array[0].HighCut - array[0].LowCut;
        int num3 = array[array.Length - 1].HighCut - array[array.Length - 1].LowCut;
        if (num1 <= num2)
          presetFilterIndex = 0;
        else if (num1 >= num3)
        {
          presetFilterIndex = array.Length - 1;
        }
        else
        {
          for (int index = 0; index < array.Length - 1; ++index)
          {
            int num4 = array[index].HighCut - array[index].LowCut;
            int num5 = array[index + 1].HighCut - array[index + 1].LowCut;
            if (num1 >= num4 && num1 < num5)
            {
              presetFilterIndex = Math.Abs(num1 - num4) >= Math.Abs(num1 - num5) ? index + 1 : index;
              break;
            }
          }
        }
      }
      return presetFilterIndex;
    }
}