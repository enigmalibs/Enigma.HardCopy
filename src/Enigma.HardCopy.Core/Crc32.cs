using System;

namespace Enigma.HardCopy.Core;

/// <summary>
/// CRC-32 (IEEE 802.3, reflected polynomial <c>0xEDB88320</c>) over a chunk's bytes.
/// </summary>
/// <remarks>
/// Every code header carries the CRC-32 of its own decoded bytes, so a single mistyped or misread
/// character is caught at the code that contains it rather than surfacing as a corrupt file at the end of
/// a recovery. This is the same CRC-32 as <c>gzip</c>, PNG and <c>zlib</c> — a hand-recovery can therefore
/// verify a chunk with ordinary tools, which is why it is implemented here rather than pulled in as a
/// package dependency.
/// </remarks>
public static class Crc32
{
    private const uint Polynomial = 0xEDB88320u;
    private const uint Seed = 0xFFFFFFFFu;

    private static readonly uint[] _table = CreateTable();

    /// <summary>Computes the CRC-32 of <paramref name="bytes"/>.</summary>
    /// <param name="bytes">The bytes to checksum. May be empty.</param>
    /// <returns>The CRC-32; <c>0x00000000</c> for empty input.</returns>
    public static uint Compute(ReadOnlySpan<byte> bytes)
    {
        uint crc = Seed;
        foreach (byte value in bytes)
        {
            crc = _table[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ Seed;
    }

    private static uint[] CreateTable()
    {
        uint[] table = new uint[256];
        for (uint index = 0; index < table.Length; index++)
        {
            uint entry = index;
            for (int bit = 0; bit < 8; bit++)
            {
                entry = (entry & 1) != 0 ? Polynomial ^ (entry >> 1) : entry >> 1;
            }

            table[index] = entry;
        }

        return table;
    }
}
