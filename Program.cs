using Roll;

class Prog
{
    static void Main(string[] args)
    {
        // Handle Command Line arguments
        MyRoll program = new();
        program.Run(args);
    }
}