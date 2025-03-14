using System.Diagnostics;

public class Program
{
    public static int Main()
    {
        Console.WriteLine("##############################");
        Console.WriteLine("#          Welcome!          #");
        Console.WriteLine("#   Launching the engine..   #");
        Console.WriteLine("##############################");

        // Set the Process Priority
        Process currentProcess = Process.GetCurrentProcess();
        currentProcess.PriorityClass = ProcessPriorityClass.High;

        while (true)
        {
            int updateResult = MainProcess.CommandUpdate();
            if (updateResult != 0)
            {
                Console.WriteLine("\n===== Process Ended with Code " + updateResult + " =====\n");
                break;
            }
        }

        Console.WriteLine("##############################");
        Console.WriteLine("#          Goodbye!          #");
        Console.WriteLine("#   Finishing the engine..   #");
        Console.WriteLine("##############################");
        
        return 0;
    }
}