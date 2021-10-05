using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

//using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System;
using System.Globalization;


using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace csFastFloat.Benchmark
{

  //[MemoryDiagnoser]
  [Config(typeof(Config))]
  public class FFBencmark
  {
    private string[] _lines;
    private byte[][] _linesUtf8;

    private class Config : ManualConfig
    {
      public Config()
      {
      }
    }


    [Benchmark(Description = "IsEightDigit SIMD")]
    unsafe public double IsEightDigit_SIMD()
    {
      double max = double.MinValue;

      foreach (var l in _lines)
      {
        fixed (char* start = l)
        {
          double d = is_made_of_eight_digits_fast_simd(start) ? 1 : 0;
          max = d > max ? d : max;
        }
      }
      return max;
    }
   
    
    [Benchmark( Baseline = true, Description = "IsEightDigit Loop")]
    unsafe public double IsEightDigit_Loop()
    {
      double max = double.MinValue;

      foreach (var l in _lines)
      {
        fixed (char* start = l)
        {
          double d = is_made_of_eight_digits_loop(start) ? 1 : 0;
          max = d > max ? d : max;
        }
      }
      return max;
    }


    [Benchmark(Description = "IsEightDigit UTF8")]
    unsafe public double IsEightDigit_UTF8()
    {
      double max = double.MinValue;

      foreach (var l in _linesUtf8)
      {
        fixed (byte* start = l)
        {
          double d = is_made_of_eight_digits_fast(start) ? 1 : 0;
          max = d > max ? d : max;
        }
      }
      return max;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    unsafe internal static bool is_made_of_eight_digits_fast_simd(char* chars)
    {
      // We only enable paths depending on this function on little endian
      // platforms (it happens to be effectively nearly everywhere).


      Vector128<short> ascii0 = Vector128.Create((short)48);
      Vector128<short> after_ascii9 = Vector128.Create((short)58);

      Vector128<short> raw = Sse3.LoadDquVector128((short*)chars);


      var a = Sse2.CompareGreaterThan(raw, ascii0);
      var b = Sse2.CompareLessThan(raw, after_ascii9);
      var c = Sse2.AndNot(a, b);

      return Sse2.Equals(c, Vector128<short>.Zero);


      //var a = (val & 0xF0F0F0F0F0F0F0F0);
      //var b = (val + 0x0606060606060606);
      //var c = (b & 0xF0F0F0F0F0F0F0F0) >> 4;

      //return BitConverter.IsLittleEndian &&
      //  ((a | c) == 0x3333333333333333);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    unsafe internal static bool is_made_of_eight_digits_loop(char* chars)
    {

      char* p = chars;
      for (int i = 0; i < 8; i++)
      {
        if (!is_integer(*p, out _))
          return false;

        ++p;
      }

      return true;

    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool is_integer(char c, out uint cMinus0)
    {
      uint cc = (uint)(c - '0');
      bool res = cc <= '9' - '0';
      cMinus0 = cc;
      return res;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    unsafe internal static bool is_made_of_eight_digits_fast(byte* chars)
    {
      ulong val = Unsafe.ReadUnaligned<ulong>(chars);
      var a = (val & 0xF0F0F0F0F0F0F0F0);
      var b = (val + 0x0606060606060606);
      var c = (b & 0xF0F0F0F0F0F0F0F0) >> 4;

      return BitConverter.IsLittleEndian && ((a | c) == 0x3333333333333333);
    }


    
   [Params(@"data/synthetic.txt" )]
   public string FileName;


    [GlobalSetup]
    public void Setup()
    {
      Console.WriteLine("reading data");
      _lines = System.IO.File.ReadAllLines(FileName);


      for (int i = 0; i != _lines.Count(); i++)
      {
        _lines[i] = _lines[i].Substring(2, 10);
      }

       _linesUtf8 = Array.ConvertAll(_lines, System.Text.Encoding.UTF8.GetBytes);
    }
  }

public class Program
{
  public static void Main(string[] args)
  {

    var summary = BenchmarkRunner.Run<FFBencmark>();

  }
}
}