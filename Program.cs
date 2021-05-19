using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CobWeb
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Player> Players = new List<Player>();
            SetValues(ref Players, numOfPlayers: -1);

            Game game = new Game(Players);
            game.PlayGame();
        }

        static void SetValues(ref List<Player> Players, int numOfPlayers = -1, int currentIndex = 0)
        {
            if (numOfPlayers == -1)
            {
                Console.WriteLine("Choose number of players (from 2 to 8).");
            }

            if (numOfPlayers > 0 || int.TryParse(Console.ReadLine(), out numOfPlayers) && numOfPlayers > 1 && numOfPlayers < 9)
            {
                int type = 0;
                string name = "";
                Console.WriteLine("Player types: 1 -  Random player, 2 -  Memory player, 3 - Thorough player, 4 - Cheater player, 5 - Thorough Cheater player");
                for (int i = currentIndex; i < numOfPlayers; i++)
                {
                    Console.WriteLine($"Choose player {i + 1} name.");
                    name = Console.ReadLine();
                    Console.WriteLine($"Choose player {i + 1} type.");
                    if (int.TryParse(Console.ReadLine(), out type) && type > 0 && type < 6)
                    {
                        Players.Add(new Player(type, name, i));
                    }
                    else
                    {
                        Console.WriteLine("Wrong type");
                        SetValues(ref Players, numOfPlayers, i);
                    }
                }
            }
            else
            {
                Console.WriteLine("Choose number of players (from 2 to 8).");
                SetValues(ref Players, numOfPlayers);
            }
        }
    }

    public class Game
    {
        const int minWeight = 40;
        const int maxWeight = 140;
        ManualResetEvent[] DoneEvents;
        List<Player> Players;
        DateTime previousTime;
        Random r = new Random();
        public int RealWeight;
        public int winnerIndex = -1;
        bool GameOver = false;

        public Game()
        {
            RealWeight = GetRandomNum();
            Players = new List<Player>();
        }

        public Game(List<Player> players)
        {
            RealWeight = GetRandomNum();
            Players = players;
            DoneEvents = new ManualResetEvent[players.Count];
        }

        public string GetWinner
        {
            get
            {
                Player winner;
                if (winnerIndex > -1)
                {
                    winner = Players[winnerIndex];
                    return $"The winner is {winner.Name}, total amount of attempts: {winner.Attempts.Count}";
                }
                winner = Players.OrderBy(p => p.MinDelta).ThenBy(g => g.Attempts.Count).FirstOrDefault();
                return $"Player {winner.Name} has closest attempt of {winner.ClosestAttempt}.";
            }
        }

        public void PlayGame()
        {
            previousTime = DateTime.Now;
            for (int i = 0; i < Players.Count; i++)
            {
                DoneEvents[i] = new ManualResetEvent(false);
                Players[i].doneEvent = DoneEvents[i];
                ThreadPool.QueueUserWorkItem(GuessNumber, Players[i]);
            }
            WaitHandle.WaitAll(DoneEvents);
            Console.WriteLine($"The real weight of the basket is {RealWeight}");
            Console.WriteLine(GetWinner);
        }

        void GuessNumber(Object threadContext)
        {
            Player player = (Player)threadContext;
            int attempt = 0, delta = 0;
            for (int i = 0; i < 100; i++)
            {
                if ((DateTime.Now - previousTime).TotalMilliseconds >= 1500)
                {
                    GameOver = true;
                }
                if (GameOver) { player.doneEvent.Set(); return; }

                attempt = GetAttempt(player);
                player.Attempts.Add(attempt);
                if (attempt == RealWeight)
                {
                    GameOver = true;
                    winnerIndex = player.PlayerIndex;
                    player.doneEvent.Set();
                    return;
                }
                else
                {
                    delta = Math.Abs(RealWeight - attempt);
                    player.UpdateFields(delta, i, attempt);
                    Thread.Sleep(delta);
                    //Console.WriteLine($"Player {player.Name} is waiting for {delta} milleseconds...");
                }
            }
        }

        int GetAttempt(Player player)
        {
            int attempt = minWeight;
            if (player.Type == (int)Player.PlayerType.RandomPlayer)
            {
                attempt = GetRandomNum();
            }
            else if (player.Type == (int)Player.PlayerType.MemoryPlayer)
            {
                attempt = GetRandomNum(player.Attempts);
            }
            else if (player.Type == (int)Player.PlayerType.ThoroughPlayer)
            {
                attempt = GetNextNum(player.CurrentIndex);
            }
            else if (player.Type == (int)Player.PlayerType.CheaterPlayer)
            {
                attempt = GetRandomNum(Players.SelectMany(p => p.Attempts).ToList());
            }
            else if (player.Type == (int)Player.PlayerType.ThoroughPlayer)
            {
                attempt = GetNextNum(player.CurrentIndex, Players.SelectMany(p => p.Attempts).ToList());
            }
            //Console.WriteLine($"Player's {player.Name} attempt is {attempt}");
            return attempt;
        }

        int GetNextNum(int currentIndex, List<int> attempts = null)
        {
            int nextInt = minWeight + currentIndex + 1;
            if (nextInt != RealWeight && attempts?.Count > 0 && attempts.Contains(nextInt))
            {
                return GetNextNum(nextInt, attempts);
            }
            return nextInt;
        }

        int GetRandomNum(List<int> attempts = null)
        {
            int rInt = r.Next(minWeight, maxWeight);
            if (rInt != RealWeight && attempts?.Count > 0 && attempts.Contains(rInt))
            {
                return GetRandomNum(attempts);
            }
            return rInt;
        }
    }

    public class Player
    {
        public List<int> Attempts { get; set; }
        public int Type { get; set; }
        public int ClosestAttempt { get; set; } = -1;
        public int MinDelta { get; set; } = 100;
        public int CurrentIndex { get; set; } = 0;
        public int PlayerIndex { get; set; }
        public string Name { get; set; }
        public ManualResetEvent doneEvent { get; set; }

        public Player()
        {
            Type = 1;
            Attempts = new List<int>();
        }

        public Player(int type)
        {
            Type = type;
            Attempts = new List<int>();
        }

        public Player(int type, string name)
        {
            Name = name;
            Type = type;
            Attempts = new List<int>();
        }

        public Player(int type, string name, int index)
        {
            Name = name;
            Type = type;
            PlayerIndex = index;
            Attempts = new List<int>();
        }
        public void UpdateFields(int delta, int index, int attempt)
        {
            CurrentIndex = index;
            if (delta < MinDelta)
            {
                MinDelta = delta;
                ClosestAttempt = attempt;
            }
        }

        public enum PlayerType
        {
            RandomPlayer = 1,
            MemoryPlayer = 2,
            ThoroughPlayer = 3,
            CheaterPlayer = 4,
            ThoroughCheaterPlayer = 5
        }
    }
}
