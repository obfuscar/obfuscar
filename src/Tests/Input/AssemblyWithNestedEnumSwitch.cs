using System.Reflection;

namespace Api
{
    public class MapMode
    {
        public static Flag Mode = Flag.Standard;

        public enum Flag : int
        {
            Standard = 0,
            Trial = 1,
            BasicGuide = 2,
            ThreeVsThreeGuide = 3,
            Fate = 4,
            Christmas = 5
        }

        public static string ModeStr
        {
            get
            {
                switch (Mode)
                {
                    case Flag.Standard: return "Standard";
                    case Flag.Trial: return "Trial";
                    case Flag.BasicGuide: return "BasicGuide";
                    case Flag.ThreeVsThreeGuide: return "ThreeVsThreeGuide";
                    case Flag.Fate: return "Fate";
                    case Flag.Christmas: return "Christmas";
                    default: break;
                }

                return "Standard";
            }
        }
    }

    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public static class Entry
    {
        public static string Execute()
        {
            MapMode.Mode = MapMode.Flag.Trial;
            return MapMode.ModeStr;
        }
    }
}
