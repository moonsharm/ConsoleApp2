using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

class Program
{
    private static Dictionary<int, (int eatCount, int thinkTime)> stats = new Dictionary<int, (int, int)>();
    private static DateTime startTime;
    private static int sessionDuration = 20;

    static void Main(string[] args)
    {
        startTime = DateTime.Now;

        // Чтение количества философов из XML
        int philosophersCount = ReadPhilosophersCountFromXml("philosophers_config.xml");

        // Инициализация статистики
        for (int i = 0; i < philosophersCount; i++)
        {
            stats[i] = (0, 0);
        }

        // Вилки (мьютексы для синхронизации)
        Mutex[] forks = new Mutex[philosophersCount];
        for (int i = 0; i < philosophersCount; i++)
        {
            forks[i] = new Mutex();
        }

        // Запуск потоков-философов
        Thread[] philosophers = new Thread[philosophersCount];
        for (int i = 0; i < philosophersCount; i++)
        {
            int id = i;
            philosophers[i] = new Thread(() => PhilosopherLife(id, forks, philosophersCount));
            philosophers[i].Start();
        }

        // Таймер для завершения работы
        Thread timerThread = new Thread(() =>
        {
            while ((DateTime.Now - startTime).TotalSeconds < sessionDuration)
            {
                Thread.Sleep(100);
            }

            Console.WriteLine("\n--- Сессия завершена ---");
            PrintStatistics();

            Environment.Exit(0);
        });
        timerThread.Start();

        Console.ReadLine();
    }

    static int ReadPhilosophersCountFromXml(string filePath)
    {
        try
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlNode? countNode = doc.SelectSingleNode("//PhilosophersCount");
            if (countNode == null)
            {
                throw new Exception("Не найден тег PhilosophersCount в XML.");
            }

            if (!int.TryParse(countNode.InnerText, out int count) || count <= 0)
            {
                throw new Exception("Некорректное количество философов (должно быть > 0).");
            }

            Console.WriteLine($"Загружено количество философов: {count}");
            return count;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении XML: {ex.Message}");
            return 5;
        }
    }

    static void PhilosopherLife(int id, Mutex[] forks, int forksCount)
    {
        int leftFork = id;
        int rightFork = (id + 1) % forksCount;

        while (true)
        {
            // Философ думает
            Console.WriteLine($"Философ {id} размышляет...");
            var thinkStart = DateTime.Now;
            Thread.Sleep(1000);
            stats[id] = (stats[id].eatCount, stats[id].thinkTime + (int)(DateTime.Now - thinkStart).TotalMilliseconds);

            // Пытается взять вилки
            if (id % 2 == 0)
            {
                forks[leftFork].WaitOne();
                forks[rightFork].WaitOne();
            }
            else
            {
                forks[rightFork].WaitOne();
                forks[leftFork].WaitOne();
            }

            // Ест
            Console.WriteLine($"Философ {id} ест!");
            var eatStart = DateTime.Now;
            Thread.Sleep(1500);
            stats[id] = (stats[id].eatCount + 1, stats[id].thinkTime);

            // Освобождает вилки
            forks[leftFork].ReleaseMutex();
            forks[rightFork].ReleaseMutex();
        }
    }

    static void PrintStatistics()
    {
        Console.WriteLine("\n=== Статистика за сессию ===");
        Console.WriteLine($"Длительность сессии: {sessionDuration} секунд");
        Console.WriteLine("ID | Раз поел | Время без еды (мс) | % времени без еды");

        foreach (var stat in stats)
        {
            int philosopherId = stat.Key;
            int eatCount = stat.Value.eatCount;
            int thinkTime = stat.Value.thinkTime;
            double thinkPercentage = (double)thinkTime / (sessionDuration * 1000) * 100;

            Console.WriteLine($"{philosopherId,2} | {eatCount,8} | {thinkTime,17} | {thinkPercentage,15:F1}%");
        }
    }
}