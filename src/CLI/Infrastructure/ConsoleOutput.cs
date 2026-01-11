using System;

namespace Typewriter.CLI.Infrastructure
{
    public static class ConsoleOutput
    {
        private const string Green = "\u001b[32m";
        private const string Red = "\u001b[31m";
        private const string Yellow = "\u001b[33m";
        private const string Reset = "\u001b[0m";

        public static void Success(string message)
        {
            Console.WriteLine(string.Concat(Green, message, Reset));
        }

        public static void Error(string message)
        {
            Console.Error.WriteLine(string.Concat(Red, message, Reset));
        }

        public static void Warning(string message)
        {
            Console.WriteLine(string.Concat(Yellow, message, Reset));
        }

        public static void Info(string message)
        {
            Console.WriteLine(message);
        }
    }
}
