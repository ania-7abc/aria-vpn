namespace AriaVPN.Tests;

/// <summary>
/// Console self-test runner.
/// Run: dotnet run --project v2rayN.Tests
/// </summary>
internal static partial class Program
{
    private static int _failed;

    private static int Main()
    {
        Console.WriteLine("AriaVPN - self test");
        Console.WriteLine(new string('-', 60));

        AutoSelectTests.Run();
        ThemeCatalogTests.Run();
        ThemePaletteTests.Run();

        Console.WriteLine(new string('-', 60));
        if (_failed == 0)
        {
            Console.WriteLine("ALL TESTS PASSED");
            return 0;
        }

        Console.WriteLine($"FAILED TESTS: {_failed}");
        return 1;
    }

    internal static void Check(string name, bool ok)
    {
        if (ok)
        {
            Console.WriteLine($"  [ OK ] {name}");
        }
        else
        {
            _failed++;
            Console.WriteLine($"  [FAIL] {name}");
        }
    }
}
