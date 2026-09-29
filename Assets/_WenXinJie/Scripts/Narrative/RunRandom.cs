using System;

namespace WenXinJie.Narrative
{
    internal sealed class RunRandom
    {
        private const int FallbackSeed = unchecked((int)0x6D2B79F5);

        public RunRandom(int seed)
        {
            State = seed == 0 ? FallbackSeed : seed;
        }

        public int State { get; private set; }

        public int Next(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
            }

            uint value = unchecked((uint)State);
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            State = unchecked((int)value);
            return (int)(value % (uint)exclusiveMaximum);
        }
    }
}