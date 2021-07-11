using Microsoft.CodeAnalysis;

namespace Generated
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource($"Hello", @"
namespace HelloWorld
{
    public static class Hello {         
        public static void Main(string[] args)
        {
            System.Console.WriteLine(""Hello World!"");
        }

        public static void Main2(string[] args)
        {
            System.Console.WriteLine(""Hello World 2!"");
        }

        public static void Main3(string[] args)
        {
            System.Console.WriteLine(""Hello World 3!"");
        }

        public static void Main5(string[] args)
        {
            System.Console.WriteLine(""Hello World 5!"");
        }

        public static void Main8(string[] args)
        {
            System.Console.WriteLine(""Hello World 5!"");
        }
    }
}");
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }
    }    
}
