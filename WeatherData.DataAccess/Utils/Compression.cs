using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace WeatherData.DataAccess.Utils
{
    public static class Compression
    {
        public static async Task<string> ToBrotliAsync(this string value)
        {
            var bytes = Encoding.Unicode.GetBytes(value);
            await using var input = new MemoryStream(bytes);
            await using var output = new MemoryStream();
            await using var stream = new BrotliStream(output, CompressionLevel.Fastest);

            await input.CopyToAsync(stream);
            await stream.FlushAsync();

            var result = output.ToArray();

            return Convert.ToBase64String(result);
        }

        public static async Task<string> FromBrotliAsync(this string value)
        {
            var bytes = Convert.FromBase64String(value);
            await using var input = new MemoryStream(bytes);
            await using var output = new MemoryStream();
            await using var stream = new BrotliStream(input, CompressionMode.Decompress);

            await stream.CopyToAsync(output);
            await output.FlushAsync();

            return Encoding.Unicode.GetString(output.ToArray());
        }
    }
}