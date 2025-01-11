global using System;
global using System.Runtime.CompilerServices;

global using static GlobalUtils;

global using Square = int;
global using Piece = int;
global using PieceIndexer = int;
global using Bitboard = ulong;

public static class GlobalUtils
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class Inline : Attribute
    {
        public Inline()
        {
            MethodImplOptions = MethodImplOptions.AggressiveInlining;
        }

        public MethodImplOptions MethodImplOptions { get; }
    }

    static GlobalUtils()
    {

    }
}