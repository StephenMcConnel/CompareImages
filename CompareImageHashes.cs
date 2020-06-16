// Copyright ©  2020 SIL International.  Licensed under GPL 3.0 (so as to use Shipwreck library).
using System;
using System.Globalization;
using System.IO;
using System.Text;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Shipwreck.Phash;
using Shipwreck.Phash.Bitmaps;

namespace CompareImages
{
	internal class CompareImageHashes
	{
		private CompareOptions _options;

		public CompareImageHashes(CompareOptions options)
		{
			this._options = options;
		}

		internal int Run()
		{
			try
			{
				var startTime = DateTime.Now;
				var bldr = new StringBuilder();
				string file1 = null;
				string file2 = null;
				if (!ulong.TryParse(_options.Image1, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
					out var hash1))
					file1 = _options.Image1;
				if (!ulong.TryParse(_options.Image2, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
					out var hash2))
					file2 = _options.Image2;
				bldr.AppendLine(file1 != null ? Path.GetFileName(file1) : _options.Image1);
				bldr.AppendLine(file2 != null ? Path.GetFileName(file2) : _options.Image2);
				if (file1 != null || file2 != null)
				{
					switch (_options.Type)
					{
						case HashType.Average:
							ComputeHashesIfNeeded(new AverageHash(), file1, file2, ref hash1, ref hash2);
							ComputeAndSaveSimilarity("Average hashes similarity: ", hash1, hash2, bldr);
							break;
						case HashType.Difference:
							ComputeHashesIfNeeded(new DifferenceHash(), file1, file2, ref hash1, ref hash2);
							ComputeAndSaveSimilarity("Difference hashes similarity: ", hash1, hash2, bldr);
							break;
						case HashType.Perceptual:
						default:
							ComputeHashesIfNeeded(new PerceptualHash(), file1, file2, ref hash1, ref hash2);
							ComputeAndSaveSimilarity("Perceptual hashes similarity: ", hash1, hash2, bldr);
							break;
						case HashType.Shipwreck:
							ComputeShipwreckDigests(out var digest1, out var digest2);
							ComputeAndSaveCrossCorrelation(digest1, digest2, bldr);
							break;
						case HashType.All:
							ComputeHashesIfNeeded(new AverageHash(), file1, file2, ref hash1, ref hash2);
							ComputeAndSaveSimilarity("Average hashes similarity: ", hash1, hash2, bldr);
							ComputeHashesIfNeeded(new DifferenceHash(), file1, file2, ref hash1, ref hash2);
							ComputeAndSaveSimilarity("Difference hashes similarity: ", hash1, hash2, bldr);
							ComputeHashesIfNeeded(new PerceptualHash(), file1, file2, ref hash1, ref hash2);
							ComputeAndSaveSimilarity("Perceptual hashes similarity: ", hash1, hash2, bldr);
							ComputeShipwreckDigests(out var digest1a, out var digest2a);
							ComputeAndSaveCrossCorrelation(digest1a, digest2a, bldr);
							break;
					}
				}
				else
				{
					ComputeAndSaveSimilarity("Hashes similarity: ", hash1, hash2, bldr);
				}

				if (String.IsNullOrEmpty(_options.OutputFile))
				{
					Console.Out.WriteLine(bldr.ToString());
				}
				else
				{
					File.WriteAllText(_options.OutputFile, bldr.ToString(), Encoding.UTF8);
				}
				var endTime = DateTime.Now;
				Console.WriteLine("Comparing hashes took {0}", endTime - startTime);
				return 0;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Console.WriteLine(e.StackTrace);
			}
			return 1;
		}

		private static void ComputeHashesIfNeeded(IImageHash hashAlgorithm, string file1, string file2, ref ulong hash1, ref ulong hash2)
		{
			if (file1 != null)
			{
				using (var stream = File.OpenRead(file1))
				{
					hash1 = hashAlgorithm.Hash(stream);
				}
			}
			if (file2 != null)
			{
				using (var stream = File.OpenRead(file2))
				{
					hash2 = hashAlgorithm.Hash(stream);
				}
			}

		}

		private static void ComputeAndSaveSimilarity(string header, ulong hash1, ulong hash2, StringBuilder bldr)
		{
			double percentageImageSimilarity = CompareHash.Similarity(hash1, hash2);
			bldr.AppendLine($"{header}{percentageImageSimilarity}");
		}

		private void ComputeShipwreckDigests(out Digest digest1, out Digest digest2)
		{
			using (var bitmap1 = (System.Drawing.Bitmap) System.Drawing.Image.FromFile(_options.Image1))
			{
				digest1 = ImagePhash.ComputeDigest(bitmap1.ToLuminanceImage());
			}
			using (var bitmap2 = (System.Drawing.Bitmap) System.Drawing.Image.FromFile(_options.Image2))
			{
				digest2 = ImagePhash.ComputeDigest(bitmap2.ToLuminanceImage());
			}
		}

		private static void ComputeAndSaveCrossCorrelation(Digest digest1, Digest digest2, StringBuilder bldr)
		{
			var score = ImagePhash.GetCrossCorrelation(digest1, digest2);
			bldr.AppendLine($"Shipwreck digests similarity: {100.0F * score}");
		}
	}
}