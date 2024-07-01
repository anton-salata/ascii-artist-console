using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace AsciiArtist.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IAsciiSymbolsService, BasicAsciiSymbolsService>()
                .AddSingleton<IAsciiSymbolsService, ExtendedAsciiSymbolsService>()
                .AddSingleton<IAsciiSymbolsService, ComplexAsciiSymbolsService>()
                .AddSingleton<IColorizingService, BasicColorMappingService>()
                .AddSingleton<IColorizingService, ClosestColorMatchingService>()
                .AddSingleton<IColorizingService, DitheringColorizingService>()
                .AddSingleton<IColorizingService, ColorAveragingService>()
                .AddSingleton<IColorizingService, DynamicRangeAdjustmentService>()
                .AddSingleton<IColorizingService, PaletteReductionService>()
                .AddSingleton<IColorizingService, RandomColorPalettesService>()

                  .AddSingleton<IColorizingService, RandomColorPalettesService>()
                    .AddSingleton<IColorizingService, RandomColorPalettesService>()
                      .AddSingleton<IColorizingService, RandomColorPalettesService>()
                        .AddSingleton<IColorizingService, RandomColorPalettesService>()
                          .AddSingleton<IColorizingService, RandomColorPalettesService>()
                .BuildServiceProvider();

            var asciiServices = serviceProvider.GetServices<IAsciiSymbolsService>().ToArray();
            var colorizingServices = serviceProvider.GetServices<IColorizingService>().ToArray();

            Console.WriteLine("Enter a search term:");
            string searchTerm = Console.ReadLine();
            Console.WriteLine("Enter the number of unique images to retrieve:");
            if (!int.TryParse(Console.ReadLine(), out int numberOfImages) || numberOfImages <= 0)
            {
                Console.WriteLine("Invalid number of images. Defaulting to 1.");
                numberOfImages = 1;
            }

            string encodedQuery = System.Web.HttpUtility.UrlEncode(searchTerm);
            string url = $"https://www.google.com/search?tbm=isch&q={encodedQuery}";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                var pageContents = await response.Content.ReadAsStringAsync();

                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(pageContents);

                var imgTags = htmlDoc.DocumentNode.SelectNodes("//img[starts-with(@src, 'https://encrypted-')]");
                if (imgTags != null)
                {
                    int imageCount = 0;
                    foreach (var imgTag in imgTags)
                    {
                        if (imageCount >= numberOfImages) break;
                        string src = imgTag.GetAttributeValue("src", "");

                        byte[] imageBytes = await httpClient.GetByteArrayAsync(src);
                        using (var ms = new MemoryStream(imageBytes))
                        {
                            using (var image = Image.FromStream(ms))
                            {
                                foreach (var asciiService in asciiServices)
                                {
                                    foreach (var colorizingService in colorizingServices)
                                    {
                                        var asciiArt = asciiService.ConvertToAsciiArt(image, colorizingService);
                                        PrintColorAsciiArt(asciiArt);
                                    }
                                }
                            }
                        }
                        imageCount++;
                    }
                }
                else
                {
                    Console.WriteLine("No image result found.");
                }
            }
        }

        private static void PrintColorAsciiArt((char, ConsoleColor)[][] asciiArt)
        {
            foreach (var row in asciiArt)
            {
                foreach (var (character, color) in row)
                {
                    Console.ForegroundColor = color;
                    Console.Write(character);
                }
                Console.WriteLine();
            }
            Console.ResetColor();
        }
    }

    public interface IAsciiSymbolsService
    {
        (char, ConsoleColor)[][] ConvertToAsciiArt(Image image, IColorizingService colorizingService);
    }

    public class BasicAsciiSymbolsService : IAsciiSymbolsService
    {
        private static readonly char[] AsciiChars = { '#', 'A', '@', '%', 'S', '+', '<', '*', ':', '-', '.', ' ' };

        public (char, ConsoleColor)[][] ConvertToAsciiArt(Image image, IColorizingService colorizingService)
        {
            return ConvertToAsciiArtInternal(image, colorizingService, AsciiChars);
        }

        protected (char, ConsoleColor)[][] ConvertToAsciiArtInternal(Image image, IColorizingService colorizingService, char[] asciiChars)
        {
            // Print the type of IColorizingService
            Console.WriteLine($"Colorizing Service Type: {colorizingService.GetType().Name}");

            // Print the contents of asciiChars array
            Console.WriteLine($"ASCII Characters: [{string.Join(", ", asciiChars)}]");

            int width = 150;
            int height = (int)(image.Height / (image.Width / (double)width) * 0.55);

            using (var resizedImage = new Bitmap(width, height))
            {
                using (var graphics = Graphics.FromImage(resizedImage))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(image, 0, 0, width, height);
                }

                var asciiArt = new (char, ConsoleColor)[height][];
                for (int y = 0; y < height; y++)
                {
                    asciiArt[y] = new (char, ConsoleColor)[width];
                    for (int x = 0; x < width; x++)
                    {
                        Color pixelColor = resizedImage.GetPixel(x, y);
                        ConsoleColor color = colorizingService.GetConsoleColor(pixelColor);
                        int grayValue = (int)((pixelColor.R * 0.3) + (pixelColor.G * 0.59) + (pixelColor.B * 0.11));
                        int asciiIndex = grayValue * (asciiChars.Length - 1) / 255;
                        asciiArt[y][x] = (asciiChars[asciiIndex], color);
                    }
                }

                return asciiArt;
            }
        }
    }

    public class ExtendedAsciiSymbolsService : BasicAsciiSymbolsService
    {
        private static readonly char[] AsciiChars = { '@', '%', '#', '*', '+', '=', '-', ':', '.', ' ' };

        public new (char, ConsoleColor)[][] ConvertToAsciiArt(Image image, IColorizingService colorizingService)
        {
            return ConvertToAsciiArtInternal(image, colorizingService, AsciiChars);
        }
    }

    public class ComplexAsciiSymbolsService : BasicAsciiSymbolsService
    {
        private static readonly char[] AsciiChars = { 'W', 'M', 'X', 'D', '0', 'K', 'H', 'U', 'A', 'V', 'i', 'n', 'w', 'm', 'r', '1', 'c', 'x', 'v', 'u', '!', '~', '(', '|', '^', '*', ':', ',', '"', '.' };

        public new (char, ConsoleColor)[][] ConvertToAsciiArt(Image image, IColorizingService colorizingService)
        {
            return ConvertToAsciiArtInternal(image, colorizingService, AsciiChars);
        }
    }

    public interface IColorizingService
    {
        ConsoleColor GetConsoleColor(Color pixelColor);
    }

    public static class ColorUtilities
    {
        // Predefined console colors
        public static readonly ConsoleColor[] AsciiColors = (ConsoleColor[])Enum.GetValues(typeof(ConsoleColor));

        // Method to find the nearest console color based on RGB values
        public static ConsoleColor FindNearestConsoleColor(int r, int g, int b)
        {
            ConsoleColor nearestColor = ConsoleColor.Black; // Default value
            double smallestDistance = double.MaxValue;

            foreach (var consoleColor in AsciiColors)
            {
                Color color = ConsoleColorToColor(consoleColor);

                // Calculate distance using RGB components
                double distance = Math.Sqrt(
                    (r - color.R) * (r - color.R) +
                    (g - color.G) * (g - color.G) +
                    (b - color.B) * (b - color.B));

                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    nearestColor = consoleColor;
                }
            }

            return nearestColor;
        }

        // Helper method to convert ConsoleColor to Color (approximation)
        private static Color ConsoleColorToColor(ConsoleColor consoleColor)
        {
            switch (consoleColor)
            {
                case ConsoleColor.Black: return Color.FromArgb(0, 0, 0);
                case ConsoleColor.DarkBlue: return Color.FromArgb(0, 0, 128);
                case ConsoleColor.DarkGreen: return Color.FromArgb(0, 128, 0);
                case ConsoleColor.DarkCyan: return Color.FromArgb(0, 128, 128);
                case ConsoleColor.DarkRed: return Color.FromArgb(128, 0, 0);
                case ConsoleColor.DarkMagenta: return Color.FromArgb(128, 0, 128);
                case ConsoleColor.DarkYellow: return Color.FromArgb(128, 128, 0);
                case ConsoleColor.Gray: return Color.FromArgb(192, 192, 192);
                case ConsoleColor.DarkGray: return Color.FromArgb(128, 128, 128);
                case ConsoleColor.Blue: return Color.FromArgb(0, 0, 255);
                case ConsoleColor.Green: return Color.FromArgb(0, 255, 0);
                case ConsoleColor.Cyan: return Color.FromArgb(0, 255, 255);
                case ConsoleColor.Red: return Color.FromArgb(255, 0, 0);
                case ConsoleColor.Magenta: return Color.FromArgb(255, 0, 255);
                case ConsoleColor.Yellow: return Color.FromArgb(255, 255, 0);
                case ConsoleColor.White: return Color.FromArgb(255, 255, 255);
                default: return Color.FromArgb(0, 0, 0); // Default to Black for undefined colors
            }
        }
    }

    public class BasicColorMappingService : IColorizingService
    {
        protected static readonly ConsoleColor[] AsciiColors = (ConsoleColor[])Enum.GetValues(typeof(ConsoleColor));

        public ConsoleColor GetConsoleColor(Color pixelColor)
        {
            int grayValue = (int)((pixelColor.R * 0.3) + (pixelColor.G * 0.59) + (pixelColor.B * 0.11));
            int colorIndex = grayValue * (AsciiColors.Length - 1) / 255;
            return AsciiColors[colorIndex];
        }
    }

    public class ClosestColorMatchingService : IColorizingService
    {
        public ConsoleColor GetConsoleColor(Color pixelColor)
        {
            ConsoleColor closestColor = ConsoleColor.Black;
            double minDistance = double.MaxValue;

            foreach (ConsoleColor consoleColor in Enum.GetValues(typeof(ConsoleColor)))
            {
                Color consoleRgb = Color.FromName(consoleColor.ToString());
                double distance = Math.Sqrt(Math.Pow(pixelColor.R - consoleRgb.R, 2) +
                                            Math.Pow(pixelColor.G - consoleRgb.G, 2) +
                                            Math.Pow(pixelColor.B - consoleRgb.B, 2));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestColor = consoleColor;
                }
            }

            return closestColor;
        }
    }

    public class DitheringColorizingService : IColorizingService
    {
        public ConsoleColor GetConsoleColor(Color pixelColor)
        {
            int r = pixelColor.R;
            int g = pixelColor.G;
            int b = pixelColor.B;

            return ColorUtilities.FindNearestConsoleColor(r, g, b);
        }
    }


    public class ColorAveragingService : IColorizingService
    {
        public ConsoleColor GetConsoleColor(Color pixelColor)
        {
            int r = pixelColor.R;
            int g = pixelColor.G;
            int b = pixelColor.B;

            // Calculate the average of RGB values
            int averageRgb = (r + g + b) / 3;

            // Find the nearest console color based on the average RGB value
            ConsoleColor nearestColor = FindNearestConsoleColor(averageRgb, averageRgb, averageRgb);

            return nearestColor;
        }

        private static ConsoleColor FindNearestConsoleColor(int r, int g, int b)
        {
            // Use ColorUtilities to find the nearest console color
            return ColorUtilities.FindNearestConsoleColor(r, g, b);
        }
    }


    public class DynamicRangeAdjustmentService : IColorizingService
    {
        public ConsoleColor GetConsoleColor(Color pixelColor)
        {
            int r = pixelColor.R;
            int g = pixelColor.G;
            int b = pixelColor.B;

            // Adjust the dynamic range of RGB values
            int adjustedR = (r * 3) / 2;
            int adjustedG = (g * 3) / 2;
            int adjustedB = (b * 3) / 2;

            // Find the nearest console color based on adjusted RGB values
            ConsoleColor nearestColor = FindNearestConsoleColor(adjustedR, adjustedG, adjustedB);

            return nearestColor;
        }

        private static ConsoleColor FindNearestConsoleColor(int r, int g, int b)
        {
            // Use ColorUtilities to find the nearest console color
            return ColorUtilities.FindNearestConsoleColor(r, g, b);
        }
    }



    public class PaletteReductionService : IColorizingService
    {
        public ConsoleColor GetConsoleColor(Color pixelColor)
        {
            int r = pixelColor.R;
            int g = pixelColor.G;
            int b = pixelColor.B;

            // Reduce the palette using some technique (example: k-means clustering)
            int[] reducedPaletteRgb = ReducePalette(r, g, b);

            // Find the nearest console color based on reduced palette RGB values
            ConsoleColor nearestColor = FindNearestConsoleColor(reducedPaletteRgb[0], reducedPaletteRgb[1], reducedPaletteRgb[2]);

            return nearestColor;
        }

        private static int[] ReducePalette(int r, int g, int b)
        {
            // Placeholder for palette reduction logic, could use k-means or other algorithm
            int[] reducedPaletteRgb = { r, g, b }; // Placeholder, actual implementation would reduce colors
            return reducedPaletteRgb;
        }

        private static ConsoleColor FindNearestConsoleColor(int r, int g, int b)
        {
            // Use ColorUtilities to find the nearest console color
            return ColorUtilities.FindNearestConsoleColor(r, g, b);
        }
    }


    public class RandomColorPalettesService : IColorizingService
    {
        private List<ConsoleColor> customPalette;

        public RandomColorPalettesService()
        {
            customPalette = GenerateRandomColors();
        }

        public ConsoleColor GetConsoleColor(Color pixelColor)
        {
            int r = pixelColor.R;
            int g = pixelColor.G;
            int b = pixelColor.B;

            // Map RGB ranges to custom palette colors
            ConsoleColor color = MapCustomColor(r, g, b);

            return color;
        }

        private ConsoleColor MapCustomColor(int r, int g, int b)
        {
            // Define RGB ranges and corresponding custom colors
            // Example ranges and colors - adjust these according to your preference
            if (r <= 85 && g <= 85 && b <= 85)
            {
                return customPalette[0]; // Dark color
            }
            else if (r <= 170 && g <= 170 && b <= 170)
            {
                return customPalette[1]; // Medium color
            }
            else
            {
                return customPalette[2]; // Bright color
            }
        }

        private List<ConsoleColor> GenerateRandomColors()
        {
            var random = new Random();
            var colors = new List<ConsoleColor>();

            Array consoleColors = Enum.GetValues(typeof(ConsoleColor));

            foreach (var color in consoleColors)
            {
                ConsoleColor consoleColor = (ConsoleColor)color;
                // Exclude dark and neutral colors
                if (consoleColor != ConsoleColor.Black &&
                    consoleColor != ConsoleColor.DarkGray &&
                    consoleColor != ConsoleColor.Gray)
                {
                    colors.Add(consoleColor);
                }
            }

            // Shuffle the list to get a random order of colors
            for (int i = 0; i < colors.Count; i++)
            {
                int r = random.Next(i, colors.Count);
                ConsoleColor temp = colors[r];
                colors[r] = colors[i];
                colors[i] = temp;
            }

            return colors;
        }
    }
}