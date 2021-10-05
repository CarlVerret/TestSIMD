using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp3
{
 public static class Class1
  {
    public unsafe static uint ParseUint(string text)
    {
      fixed (char* c = text)
      {
        var parsed = Sse3.LoadDquVector128((byte*)c);
        var shift = (8 - text.Length) * 2;
        var shifted = Sse2.ShiftLeftLogical128BitLane(parsed,
          (byte)(shift));

        Vector128<byte> digit0 = Vector128.Create((byte)'0');
        var reduced = Sse2.SubtractSaturate(shifted, digit0);

        var shortMult = Vector128.Create(10, 1, 10, 1, 10, 1, 10, 1);
        var collapsed2 = Sse2.MultiplyAddAdjacent(reduced.As<byte, short>(), shortMult);

        var repack = Sse41.PackUnsignedSaturate(collapsed2, collapsed2);
        var intMult = Vector128.Create((short)0, 0, 0, 0, 100, 1, 100, 1);
        var collapsed3 = Sse2.MultiplyAddAdjacent(repack.As<ushort, short>(), intMult);

        var e1 = collapsed3.GetElement(2);
        var e2 = collapsed3.GetElement(3);
        return (uint)(e1 * 10000 + e2);
      }
    }

    public static unsafe uint ParseUint3(string text)
    {
      fixed (char* c = text)
      {
        Vector128<ushort> raw = Sse3.LoadDquVector128((ushort*)c);
        switch (text.Length)
        {
          case 0: raw = Vector128<ushort>.Zero; break;
          case 1: raw = Sse2.ShiftLeftLogical128BitLane(raw, 14); break;
          case 2: raw = Sse2.ShiftLeftLogical128BitLane(raw, 12); break;
          case 3: raw = Sse2.ShiftLeftLogical128BitLane(raw, 10); break;
          case 4: raw = Sse2.ShiftLeftLogical128BitLane(raw, 8); break;
          case 5: raw = Sse2.ShiftLeftLogical128BitLane(raw, 6); break;
          case 6: raw = Sse2.ShiftLeftLogical128BitLane(raw, 4); break;
          case 7: raw = Sse2.ShiftLeftLogical128BitLane(raw, 2); break;
        };
        Vector128<ushort> digit0 = Vector128.Create('0');
        raw = Sse2.SubtractSaturate(raw, digit0);
        Vector128<short> mul0 = Vector128.Create(10, 1, 10, 1, 10, 1, 10, 1);
        Vector128<int> res = Sse2.MultiplyAddAdjacent(raw.AsInt16(), mul0);
        Vector128<int> mul1 = Vector128.Create(1000000, 10000, 100, 1);
        res = Sse41.MultiplyLow(res, mul1);
        res = Ssse3.HorizontalAdd(res, res);
        res = Ssse3.HorizontalAdd(res, res);
        return (uint)res.GetElement(0);
      }
    }

    public static unsafe uint ParseUint4(string text)
    {
      const string mask = "\xffff\xffff\xffff\xffff\xffff\xffff\xffff\xffff00000000";
      fixed (char* c = text, m = mask)
      {
        Vector128<ushort> raw = Sse3.LoadDquVector128((ushort*)c - 8 + text.Length);
        Vector128<ushort> mask0 = Sse3.LoadDquVector128((ushort*)m + text.Length);
        raw = Sse2.SubtractSaturate(raw, mask0);
        Vector128<short> mul0 = Vector128.Create(10, 1, 10, 1, 10, 1, 10, 1);
        Vector128<int> res = Sse2.MultiplyAddAdjacent(raw.AsInt16(), mul0);
        Vector128<int> mul1 = Vector128.Create(1000000, 10000, 100, 1);
        res = Sse41.MultiplyLow(res, mul1);
        Vector128<int> shuf = Sse2.Shuffle(res, 0x1b); // 0 1 2 3 => 3 2 1 0
        res = Sse2.Add(shuf, res);
        shuf = Sse2.Shuffle(res, 0x41); // 0 1 2 3 => 1 0 3 2
        res = Sse2.Add(shuf, res);
        return (uint)res.GetElement(0);
      }
    }
  }

}
