using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

class DrawingProgram
{
    static char activeChar = '█';
    static ConsoleColor activeColor = ConsoleColor.White;
    static int cursorX = Console.WindowWidth / 2;
    static int cursorY = Console.WindowHeight / 2;
    static bool isExiting = false;
    static bool isDrawing = false;

    static string[] menuItems = { "New Drawing", "Edit Drawing", "Delete Drawing", "Exit" };
    static int currentMenuIndex = 0;

    static List<Point> currentDrawing = new List<Point>();

    static void Main()
    {
        Console.CursorVisible = false;

        using (var dbContext = new DrawingDbContext())
        {
            dbContext.Database.Migrate();
        }

        DisplayMenu();
        while (!isExiting)
        {
            if (isDrawing)
            {
                EnterDrawingMode();
            }
            else
            {
                DisplayMenu();
            }
        }
    }

    static void DisplayMenu()
    {
        RenderMenuFrame();
        RenderMenuOptions(menuItems, currentMenuIndex);

        while (!isDrawing && !isExiting)
        {
            var input = Console.ReadKey(true);

            if (input.Key == ConsoleKey.UpArrow && currentMenuIndex > 0)
                currentMenuIndex--;
            else if (input.Key == ConsoleKey.DownArrow && currentMenuIndex < menuItems.Length - 1)
                currentMenuIndex++;
            else if (input.Key == ConsoleKey.Enter)
            {
                switch (currentMenuIndex)
                {
                    case 0:
                        StartNewDrawing();
                        break;
                    case 1:
                        LoadAndEditDrawing();
                        break;
                    case 2:
                        DeleteExistingDrawing();
                        break;
                    case 3:
                        isExiting = true;
                        break;
                }
            }
            RenderMenuFrame();
            RenderMenuOptions(menuItems, currentMenuIndex);
        }
    }

    static void StartNewDrawing()
    {
        currentDrawing.Clear();
        cursorX = Console.WindowWidth / 2;
        cursorY = Console.WindowHeight / 2;
        isDrawing = true;
        Console.Clear();
    }

    static void LoadAndEditDrawing()
    {
        using (var dbContext = new DrawingDbContext())
        {
            var availableDrawings = dbContext.Drawings.Include(d => d.Points).ToArray();
            if (availableDrawings.Length == 0)
            {
                Console.Clear();
                Console.WriteLine("No drawings available to edit.");
                Console.ReadKey();
                return;
            }

            var selectedDrawing = ChooseDrawing(availableDrawings);
            LoadDrawingData(selectedDrawing);
            isDrawing = true;
            Console.Clear();
        }
    }

    static void DeleteExistingDrawing()
    {
        using (var dbContext = new DrawingDbContext())
        {
            var availableDrawings = dbContext.Drawings.ToArray();
            if (availableDrawings.Length == 0)
            {
                Console.Clear();
                Console.WriteLine("No drawings available to delete.");
                Console.ReadKey();
                return;
            }

            var selectedDrawing = ChooseDrawing(availableDrawings);
            dbContext.Drawings.Remove(selectedDrawing);
            dbContext.SaveChanges();
            Console.Clear();
            Console.WriteLine("Drawing deleted successfully.");
            Console.ReadKey();
        }
    }

    static Drawing ChooseDrawing(Drawing[] drawings)
    {
        int selectionIndex = 0;

        while (true)
        {
            Console.Clear();
            Console.WriteLine("Select a drawing:");
            for (int i = 0; i < drawings.Length; i++)
            {
                if (i == selectionIndex)
                    Console.ForegroundColor = ConsoleColor.Green;
                else
                    Console.ResetColor();

                Console.WriteLine($"{i + 1}. {drawings[i].Name}");
            }

            var input = Console.ReadKey(true);
            if (input.Key == ConsoleKey.UpArrow && selectionIndex > 0)
                selectionIndex--;
            else if (input.Key == ConsoleKey.DownArrow && selectionIndex < drawings.Length - 1)
                selectionIndex++;
            else if (input.Key == ConsoleKey.Enter)
                return drawings[selectionIndex];
        }
    }

    static void LoadDrawingData(Drawing drawing)
    {
        currentDrawing.Clear();
        currentDrawing.AddRange(drawing.Points);
        RedrawCanvas();
    }

    static void SaveCurrentDrawing(string name)
    {
        using (var dbContext = new DrawingDbContext())
        {
            var newDrawing = new Drawing
            {
                Name = name,
                Points = new List<Point>(currentDrawing)
            };

            dbContext.Drawings.Add(newDrawing);
            dbContext.SaveChanges();
        }
    }

    static void EnterDrawingMode()
    {
        Console.Clear();

        while (isDrawing)
        {
            RedrawCanvas();
            var input = Console.ReadKey(true);

            if (input.Key == ConsoleKey.Spacebar)
            {
                Console.SetCursorPosition(cursorX, cursorY);
                Console.ForegroundColor = activeColor;
                Console.Write(activeChar);
                Console.ResetColor();
                currentDrawing.Add(new Point { X = cursorX, Y = cursorY, Character = activeChar, Color = activeColor });
            }
            else if (input.Key == ConsoleKey.Escape)
            {
                Console.Clear();
                Console.Write("Enter a name to save the drawing: ");
                string fileName = Console.ReadLine();
                SaveCurrentDrawing(fileName);
                isDrawing = false;
                Console.Clear();
            }
            else if (input.Key == ConsoleKey.F1) activeChar = '█';
            else if (input.Key == ConsoleKey.F2) activeChar = '▓';
            else if (input.Key == ConsoleKey.F3) activeChar = '▒';
            else if (input.Key == ConsoleKey.F4) activeChar = '░';
            else if (input.Key >= ConsoleKey.D0 && input.Key <= ConsoleKey.D9)
                activeColor = (ConsoleColor)(input.Key - ConsoleKey.D0);
            else
                AdjustCursor(input);
        }
    }

    static void RedrawCanvas()
    {
        Console.Clear();
        foreach (var point in currentDrawing)
        {
            Console.SetCursorPosition(point.X, point.Y);
            Console.ForegroundColor = point.Color;
            Console.Write(point.Character);
        }
        Console.ResetColor();
    }

    static void AdjustCursor(ConsoleKeyInfo input)
    {
        switch (input.Key)
        {
            case ConsoleKey.LeftArrow: if (cursorX > 0) cursorX--; break;
            case ConsoleKey.RightArrow: if (cursorX < Console.WindowWidth - 1) cursorX++; break;
            case ConsoleKey.UpArrow: if (cursorY > 0) cursorY--; break;
            case ConsoleKey.DownArrow: if (cursorY < Console.WindowHeight - 1) cursorY++; break;
        }
    }

    static void RenderMenuOptions(string[] options, int highlightedIndex)
    {
        int menuStartY = Console.WindowHeight / 2 - options.Length / 2;

        for (int i = 0; i < options.Length; i++)
        {
            Console.SetCursorPosition(Console.WindowWidth / 2 - options[i].Length / 2, menuStartY + i);

            if (i == highlightedIndex)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(options[i]);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine(options[i]);
            }
        }
    }

    static void RenderMenuFrame()
    {
        int menuWidth = 24;
        int menuHeight = menuItems.Length + 2;
        int startX = Console.WindowWidth / 2 - menuWidth / 2;
        int startY = Console.WindowHeight / 2 - menuHeight / 2;

        Console.SetCursorPosition(startX, startY);
        Console.Write("╔");
        for (int i = 0; i < menuWidth - 2; i++) Console.Write("═");
        Console.Write("╗");

        for (int i = 1; i < menuHeight - 1; i++)
        {
            Console.SetCursorPosition(startX, startY + i);
            Console.Write("║");
            Console.SetCursorPosition(startX + menuWidth - 1, startY + i);
            Console.Write("║");
        }

        Console.SetCursorPosition(startX, startY + menuHeight - 1);
        Console.Write("╚");
        for (int i = 0; i < menuWidth - 2; i++) Console.Write("═");
        Console.Write("╝");
    }
}

public class Drawing
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Point> Points { get; set; } = new List<Point>();
}

public class Point
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public char Character { get; set; }
    public ConsoleColor Color { get; set; }
    public int DrawingId { get; set; } // Foreign key
}

public class DrawingDbContext : DbContext
{
    public DbSet<Drawing> Drawings { get; set; }
    public DbSet<Point> Points { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=drawings.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Drawing>()
            .HasMany(d => d.Points)
            .WithOne()
            .HasForeignKey(p => p.DrawingId);
    }
}
