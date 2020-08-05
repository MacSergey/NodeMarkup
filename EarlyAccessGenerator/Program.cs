using System;

namespace NodeMarkup
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                var id = Console.ReadLine();
                var sign = Crypt.Sign(id);
                Console.WriteLine(sign);
            }
        }
    }
}
