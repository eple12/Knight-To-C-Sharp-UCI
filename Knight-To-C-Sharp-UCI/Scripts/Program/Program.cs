using System.Runtime.InteropServices;

public class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("##############################");
        Console.WriteLine("#          Welcome!          #");
        Console.WriteLine("#   Launching the engine..   #");
        Console.WriteLine("##############################");

        MainProcess.Start();

        while (true)
        {
            int updateResult = MainProcess.Update();
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