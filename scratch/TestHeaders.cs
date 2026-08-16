using System;
using System.Net.Http;

class Program
{
    static void Main()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://bolcko.com");
        request.Headers.TryAddWithoutValidation("company-id", "186");
        request.Headers.TryAddWithoutValidation("company-id", "186");
        
        foreach (var header in request.Headers)
        {
            Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }
    }
}
