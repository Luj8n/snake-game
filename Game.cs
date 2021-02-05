using System;
using System.Threading;

namespace snake_game
{
  class Game
  {
    // Game settings --------------------------------------------------------
    // change anything here
    static char HEAD_CHAR = 'X';
    static char BODY_CHAR = 'o';
    static char FRUIT_CHAR = '$';
    static char NOTHING_CHAR = '.';

    static int DEFAULT_SNAKE_LENGTH = 2; // starting snake length
    static bool WRAP = true; // if set to true, the snake won't die if it touches the wall, it will go to the other side
    static bool FRUITS_SHOULD_DESPAWN = false; // if set to true, fruits will despawn after some random time, and then generate a new fruit
    static int FRAME_TIME = 150; // in milliseconds

    // this is the size of the map (min is 5 x 5)
    static int HEIGHT = 20;
    static int WIDTH = 20;

    // don't change anything below ------------------------------------------

    // Values:
    // 0 - there is nothing there
    // -1 - snake's head
    // <-1 - fruit, and how many updates it should stay in that place; (lower number, means more time left)
    // >0 - snake's body part; how many updates should snake's body part last in that square; (the higher number, more time left)
    static int[,] MAP;

    static Random random = new Random();

    static int SNAKE_LENGTH = DEFAULT_SNAKE_LENGTH; // starting snake length
    static bool NO_FRUITS_ON_MAP = true; // if set to false, no fruits will spawn at start
    static bool NOT_DEAD = true; // is snake dead
    static bool GAME_IS_PAUSED = false;
    static bool LISTENING_FOR_INPUT = false; // if set to true, it is already listening for input

    //   0
    // 3   1  =>  up is 0, right is 1, down is 2, left is 3
    //   2
    static int DIR = 1; // snake's direction
    static int NEW_DIR = 1; // snake's temporary direction

    static public void Start()
    {
      if (!LISTENING_FOR_INPUT) StartListenForKey(); // starts listening for inputs, if not already listening

      NewMap(); // create the default map
      DisplayMap();

      Thread.Sleep(500); // wait for a bit, let the player react
      while (NOT_DEAD)
      {
        if (!GAME_IS_PAUSED)
        {
          Thread.Sleep(FRAME_TIME);
          Game.Update();
        }
      }
      // snake is dead
      Console.WriteLine("You died. Press ENTER to end or R to restart");
    }

    static void StartListenForKey()
    {
      Thread listeningT = new Thread(() => // listening for inputs on another thread, because the main thread is sleeping
      {
        while (true)
        {
          int pressedKey = (int)Console.ReadKey(true).Key; // gets pressed key
          if (!NOT_DEAD && pressedKey == 13) break; // if snake is dead, end game only when pressedKey is "enter"
          if (!NOT_DEAD && pressedKey == 82) Restart(); // if snake is dead, restart game only when pressedKey is "r"
          if (pressedKey == 38 && DIR != 2) NEW_DIR = 0; // up-arrow
          if (pressedKey == 39 && DIR != 3) NEW_DIR = 1; // right-arrow
          if (pressedKey == 40 && DIR != 0) NEW_DIR = 2; // down-arrow
          if (pressedKey == 37 && DIR != 1) NEW_DIR = 3; // left-arrow
          if (pressedKey == 70) NO_FRUITS_ON_MAP = true; // press "f" to add new fruit
          if (pressedKey == 76) SNAKE_LENGTH += 1; // press "a" to make snake longer
          if (pressedKey == 32) GAME_IS_PAUSED = !GAME_IS_PAUSED; // change game's paused state (space)
          if (pressedKey == 78) Update(); // go to the next frame (update the game) (n)
          if (pressedKey == 49 && FRAME_TIME > 10) FRAME_TIME -= 10; // lower frame time (1)
          if (pressedKey == 50) FRAME_TIME += 10; // lengthen frame time (2)
        }
      });
      listeningT.Start();
    }

    static void Restart()
    {
      // reset global values
      SNAKE_LENGTH = DEFAULT_SNAKE_LENGTH;
      GAME_IS_PAUSED = false;
      NOT_DEAD = true;
      DIR = 1;
      NEW_DIR = 1;
      Start(); // start new game
    }

    static public void Update()
    {
      DIR = NEW_DIR;
      Move();
      if (NOT_DEAD) // continue if not snake is not dead
      {
        NewFruit();
        DisplayMap();
      }
    }

    static void NewFruit()
    {
      if (NO_FRUITS_ON_MAP) // generate a new fruit, if there aren't any fruits on the map already
      {
        bool spotFound = false;
        while (!spotFound) // find a empty spot
        {
          int randomI = random.Next(0, HEIGHT);
          int randomJ = random.Next(0, WIDTH);

          int randomDuration = random.Next(-WIDTH, -10); // the lower value, the longer it will last
          if (MAP[randomI, randomJ] == 0) // check if the square is empty
          {
            MAP[randomI, randomJ] = randomDuration;
            spotFound = true;
          }
        }
        NO_FRUITS_ON_MAP = false;
      }
    }

    static void Move()
    {
      int[,] newMap = (int[,])MAP.Clone();

      int[] newHeadPos = new int[2]; // i is first, j is second

      for (int i = 0; i < HEIGHT; i++)
      {
        for (int j = 0; j < WIDTH; j++)
        {
          int newVal = 0;
          switch (MAP[i, j])
          {
            case 0: // nothing
              newVal = 0; // don't change
              break;
            case -1: // snake's head
              if (WillDie(i, j)) { NOT_DEAD = false; goto MapUpdated; } // check if snake will die and skip to the end
              if (WillEatFruit(i, j)) // check if snake will eat a fruit. if true, make the snake longer
              {
                SNAKE_LENGTH += 1;
                NO_FRUITS_ON_MAP = true; // generate a new fruit later this update
              }

              // calculates snake head's new position
              newHeadPos = NewHeadPosition(i, j);

              newVal = SNAKE_LENGTH; // make the new body part
              break;
            case -2: // fruit
              if (FRUITS_SHOULD_DESPAWN) // remove the fruit, if setting is on
              {
                newVal = 0;
                NO_FRUITS_ON_MAP = true; // generate a new fruit later this update
              }
              else newVal = -2; // keep the fruit
              break;
            default:
              if (MAP[i, j] < -2) newVal = MAP[i, j] + 1; // lower fruit's left time
              else newVal = MAP[i, j] - 1; // lower snake part's left time
              break;
          }
          newMap[i, j] = newVal;
        }
      }

      newMap[newHeadPos[0], newHeadPos[1]] = -1; // place snake's head into the map

      MAP = newMap; // update the old map with the new map

    MapUpdated:; // label for skipping the map update
    }

    static int[] NewHeadPosition(int i, int j)
    {
      int newI = 0;
      int newJ = 0;
      if (WRAP && WillHitWall(i, j)) // if wrap is enabled and the snake would hit a wall, go to other side (wrap)
      {
        if (DIR == 0) { newI = HEIGHT - 1; newJ = j; } // if dir is top
        if (DIR == 1) { newI = i; newJ = 0; } // if dir is right
        if (DIR == 2) { newI = 0; newJ = j; } // if dir is bottom
        if (DIR == 3) { newI = i; newJ = WIDTH - 1; } // if dir is left
      }
      else
      {
        if (DIR == 0) { newI = i - 1; newJ = j; } // if dir is top
        if (DIR == 1) { newI = i; newJ = j + 1; } // if dir is right
        if (DIR == 2) { newI = i + 1; newJ = j; } // if dir is bottom
        if (DIR == 3) { newI = i; newJ = j - 1; } // if dir is left
      }
      return new int[] { newI, newJ };
    }

    static bool WillDie(int i, int j)
    {
      if (WillHitWall(i, j))
      {
        if (!WRAP) return true; // if snake hits a wall and wrap is disabled, snake will die
        // check if snake hits itself on the other side
        if (DIR == 0 && MAP[HEIGHT - 1, j] > 0) return true; // check bottom
        if (DIR == 1 && MAP[i, 0] > 0) return true; // check left
        if (DIR == 2 && MAP[0, j] > 0) return true; // check top
        if (DIR == 3 && MAP[i, WIDTH - 1] > 0) return true; // check right
      }
      else
      {
        // check if snake hits itself
        if (DIR == 0 && MAP[i - 1, j] > 0) return true; // check top
        if (DIR == 1 && MAP[i, j + 1] > 0) return true; // check right
        if (DIR == 2 && MAP[i + 1, j] > 0) return true; // check bottom
        if (DIR == 3 && MAP[i, j - 1] > 0) return true; // check left
      }
      return false; // snake doesn't die
    }

    static bool WillHitWall(int i, int j)
    {
      if (DIR == 0 && i == 0) return true; // check if hits top wall
      if (DIR == 1 && j == WIDTH - 1) return true; // check if hits right wall
      if (DIR == 2 && i == HEIGHT - 1) return true; // check if hits bottom wall
      if (DIR == 3 && j == 0) return true; // check if hits left wall
      return false; // snake hits nothing, that will kill it
    }

    static bool WillEatFruit(int i, int j)
    {
      if (WRAP && WillHitWall(i, j)) // if wrap is enabled and the snake would hit a wall, check other side (wrap)
      {
        // check if snake will eat a fruit
        if (DIR == 0 && MAP[HEIGHT - 1, j] < -1) return true; // check bottom
        if (DIR == 1 && MAP[i, 0] < -1) return true; // check left
        if (DIR == 2 && MAP[0, j] < -1) return true; // check top
        if (DIR == 3 && MAP[i, WIDTH - 1] < -1) return true; // check right
      }
      else
      {
        // check if snake will eat a fruit
        if (DIR == 0 && MAP[i - 1, j] < -1) return true; // check to the top
        if (DIR == 1 && MAP[i, j + 1] < -1) return true; // check to the right
        if (DIR == 2 && MAP[i + 1, j] < -1) return true; // check to the bottom
        if (DIR == 3 && MAP[i, j - 1] < -1) return true; // check to the left
      }
      return false; // snake will not eat a fruit
    }

    static void DisplayMap()
    {
      string textToPrint = "";
      for (int i = 0; i < HEIGHT; i++)
      {
        for (int j = 0; j < WIDTH; j++)
        {
          char charToPrint;
          switch (MAP[i, j])
          {
            case 0: // nothing
              charToPrint = NOTHING_CHAR;
              break;
            case -1: // snake's head
              charToPrint = HEAD_CHAR;
              break;
            default:
              if (MAP[i, j] < -1) charToPrint = FRUIT_CHAR; // fruit
              else charToPrint = BODY_CHAR; // snake's body part
              break;
          }
          textToPrint += charToPrint; // adds the character to the string, that will be printed later
        }
        textToPrint += "\n";
      }

      Console.Clear();
      Console.Write(textToPrint);
    }

    static void NewMap()
    {
      // sets every value in the map to 0
      if (HEIGHT < 5 || WIDTH < 5) throw new Exception("Height or width can't be less than 5");
      int[,] map = new int[HEIGHT, WIDTH];
      for (int i = 0; i < HEIGHT; i++)
      {
        for (int j = 0; j < WIDTH; j++)
        {
          map[i, j] = 0;
        }
      }
      // starting position
      map[HEIGHT / 2, WIDTH / 2] = -1;
      NO_FRUITS_ON_MAP = true; // a fruit should be generated, because there are no fruits
      MAP = map;
    }
  }
}
