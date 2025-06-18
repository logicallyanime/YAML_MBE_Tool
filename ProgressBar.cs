using System;

namespace ConsoleProgress
{
    public class ProgressBar
    {
        private readonly int total;
        private int progress;
        private readonly object lockObj = new();

        public ProgressBar(int total)
        {
            if (total <= 0)
                throw new ArgumentException("Total must be greater than zero.", nameof(total));
            this.total = total;
            this.progress = 0;
            Draw(); // Initial draw
        }

        public void Report(int value)
        {
            lock (lockObj)
            {
                progress = Math.Clamp(value, 0, total);
                Draw();
            }
        }

        private void Draw()
        {
            int percent = (int)((double)progress / total * 100);
            int barWidth = Console.WindowWidth - 20;
            int filledBar = (int)((double)barWidth * progress / total);
            string bar = new string('#', filledBar) + new string('-', barWidth - filledBar);

            // Save current cursor position
            int currentLeft = Console.CursorLeft;
            int currentTop = Console.CursorTop;
            // Set cursor to the bottom line
            int bottomLine = Console.WindowHeight - 1;
            Console.SetCursorPosition(0, bottomLine);
            Console.Write($"[{bar}] {percent,3}%");
            // Clear the rest of the line to remove artifacts
            Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));
            // Restore cursor position if it is not on the bottom line
            if (currentTop < bottomLine)
                Console.SetCursorPosition(currentLeft, currentTop);
        }
    }
}
