using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Rendering;

public class StackDebugger {
    // Search Move Stack to track the current search line
    // Todo: implement stack tracing
    bool DebugPushing = false;
    bool DebugPoping = false;

    public void TurnPushingOn() {
        DebugPushing = true;
    }

    public void TurnPopingOn() {
        DebugPoping = true;
    }

    Stack<Move> moveStack = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Push(Move m) {
        moveStack.Push(m);

        if (DebugPushing) {
            if (Check()) {
                Console.WriteLine($"Stack Debugger caught a case while Pushing: {Current}");
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Pop() {
        moveStack.Pop();

        if (DebugPoping) {
            if (Check()) {
                Console.WriteLine($"Stack Debugger caught a case while Poping: {Current}");
                return true;
            }
        }

        return false;
    }

    public string Current {
        get {
            return string.Join(" ", moveStack.Reverse().Select(a => a.San));
        }
    }

    List<List<Move>> checkers = new();

    public void Add(List<string> moves) {
        List<Move> m = moves.Select(a => MoveHelper.OnlySquares(a)).ToList();
        checkers.Add(m);
    }

    public void Add(string moves) {
        Add(moves.Split(' ').ToList());
    }

    public StackDebugger() {

    }

    bool Check() {
        foreach (var item in checkers) {
            if (item.Select(a => a.SquareRepresentation).SequenceEqual(moveStack.Reverse().Select(a => a.SquareRepresentation))) {
                return true;
            }
        }

        return false;
    }
}