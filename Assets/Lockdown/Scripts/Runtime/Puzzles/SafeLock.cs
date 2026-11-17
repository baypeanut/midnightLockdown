namespace Lockdown
{
    // pure combination logic: a number is locked in when the dial reverses on it
    public class SafeLock
    {
        public enum Result { Tick, Catch, Locked, Reset, Opened }

        readonly int[] combo;
        public int Stage { get; private set; }
        public int Resets { get; private set; }
        public bool Open => Stage >= combo.Length;
        int dir, current = -1;

        public SafeLock(int[] combination) { combo = combination; }

        public int Target => Open ? -1 : combo[Stage];

        // number is the value now under the marker, direction is +1 or -1
        public Result Step(int number, int direction)
        {
            if (Open) return Result.Opened;
            Result r = Result.Tick;
            if (dir != 0 && direction != dir && current >= 0)
            {
                if (current == combo[Stage])
                {
                    Stage++;
                    r = Open ? Result.Opened : Result.Locked;
                }
                else if (Stage > 0)
                {
                    Stage = 0;
                    Resets++;
                    r = Result.Reset;
                }
            }
            dir = direction;
            current = number;
            if (r == Result.Tick && !Open && number == combo[Stage]) r = Result.Catch;
            return r;
        }
    }
}
