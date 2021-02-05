using System;
namespace snake_game
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine();
      Console.WriteLine("Controls:");
      Console.WriteLine("  Change direction with arrow keys");
      Console.WriteLine();
      Console.WriteLine("Cheats:");
      Console.WriteLine("  Make snake longer - L");
      Console.WriteLine("  Spawn a new fruit - F");
      Console.WriteLine("  Pause the game - Space");
      Console.WriteLine("  Next frame - N");
      Console.WriteLine("  Lower frame time - 1");
      Console.WriteLine("  Lengthen frame time - 2");
      Console.WriteLine();
      Console.WriteLine("Press ENTER to start game");
    ReadKey: int pressedKey = (int)Console.ReadKey(true).Key; // gets pressed key
      if (pressedKey != 13) goto ReadKey; // start game only when pressedKey is "enter"
      Game.Start();
    }
  }
}
