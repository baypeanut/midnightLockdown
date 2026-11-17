using UnityEngine;

namespace Lockdown
{
    // per run randomized answers so replays differ
    public class RunSecrets
    {
        public readonly int clockHour, clockMinute;
        public readonly int[] safe = new int[3];
        public readonly int[] fuseSlots = new int[3];
        public const int SafeNumbers = 40;
        static readonly int[] Ratings = { 10, 20, 30 };

        public RunSecrets(int seed)
        {
            var r = new System.Random(seed);
            clockHour = r.Next(1, 13);
            do clockMinute = r.Next(1, 12) * 5; while (clockMinute == 15 && clockHour == 12);
            safe[0] = r.Next(4, SafeNumbers);
            do safe[1] = r.Next(0, SafeNumbers); while (Gap(safe[1], safe[0]) < 6);
            do safe[2] = r.Next(0, SafeNumbers); while (Gap(safe[2], safe[1]) < 6);
            // shuffle which slot wants which rating
            for (int i = 0; i < 3; i++) fuseSlots[i] = Ratings[i];
            for (int i = 2; i > 0; i--)
            {
                int j = r.Next(0, i + 1);
                (fuseSlots[i], fuseSlots[j]) = (fuseSlots[j], fuseSlots[i]);
            }
        }

        static int Gap(int a, int b)
        {
            int d = Mathf.Abs(a - b);
            return Mathf.Min(d, SafeNumbers - d);
        }

        public string ClockText => clockHour + ":" + clockMinute.ToString("00");
        public string SafeText => safe[0] + ", " + safe[1] + ", " + safe[2];
        public string FuseOrderText => "left " + fuseSlots[0] + "A, middle " + fuseSlots[1] + "A, right " + fuseSlots[2] + "A";
    }
}
