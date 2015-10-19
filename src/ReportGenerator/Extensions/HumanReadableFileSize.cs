using System;
using System.Diagnostics.CodeAnalysis;

namespace ReportGenerator.Extensions
{
    internal static class HumanReadableFileSize
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        enum Sizes : ulong
        {
            B = 0x0u,
            KB = 0x400u,
            MB = 0x100000u,
            GB = 0x40000000u,
            TB = 0x10000000000u
        }

        private static readonly Func<decimal, Sizes, string> MakeFileSizeString =
            (value, size) => $"{value/(size <= Sizes.KB ? 1 : 1024):0.#} {size}";

        private static readonly Func<ulong, Sizes, bool> Is = (value, size) => value >= (ulong) size; 

        public static string AsReadableFileSize(this ulong bytes)
        {
            if (bytes == 0) return $"0 {Sizes.B}";
            if (Is(bytes, Sizes.TB))
                return MakeFileSizeString(bytes >> 30, Sizes.TB);
            if (Is(bytes, Sizes.GB))
                return MakeFileSizeString(bytes >> 20, Sizes.GB);
            if (Is(bytes, Sizes.MB))
                return MakeFileSizeString(bytes >> 10, Sizes.MB);
            if (Is(bytes, Sizes.KB))
                return MakeFileSizeString(bytes, Sizes.KB);
            return MakeFileSizeString(bytes, Sizes.B);
        }

        public static string AsReadableFileSize(this uint bytes)
        {
            return AsReadableFileSize((ulong) bytes);
        }

        public static string AsReadableFileSize(this ushort bytes)
        {
            return AsReadableFileSize((ulong)bytes);
        }
    }
}
