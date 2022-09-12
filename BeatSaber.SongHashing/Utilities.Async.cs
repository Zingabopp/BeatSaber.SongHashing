#if ASYNC
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaber.SongHashing
{
	public static partial class Utilities
	{
		/// <summary>
		/// Generates a SHA1 hash from a Stream.
		/// </summary>
		/// <param name="input">Stream to hash.</param>
		/// <param name="cancellationToken"></param>
		/// <returns>Sha1 hash of the Stream.</returns>
		public static async Task<string> CreateSha1FromStreamAsync(Stream input, CancellationToken cancellationToken)
		{
			using SHA1 sha1 = SHA1.Create();
			byte[] hashBytes = await sha1.ComputeHashAsync(input, cancellationToken);
			return HexMate.Convert.ToHexString(hashBytes);
		}
	}
}
#endif