namespace DikidiStalker
{
    internal class Writer
    {
        public static bool WriteFile(string content, string path, bool append = false)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(path, append: append))
                {
                    if (content.Length > 0)
                    {
                        writer.Write(content);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool ConsoleWrite(string content, ConsoleColor color = ConsoleColor.Gray)
        {
            try
            {
                Console.ForegroundColor = color;
                Console.Write(content);
                Console.ResetColor();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool ConsoleWriteLine(string content, ConsoleColor color = ConsoleColor.Gray) => ConsoleWrite(content + "\n", color);
    }
}
