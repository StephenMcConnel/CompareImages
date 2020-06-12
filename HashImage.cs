using System;
using System.IO;
using System.Text;
using System.Xml.Schema;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Shipwreck.Phash;
using Shipwreck.Phash.Bitmaps;

namespace CompareImages
{
	internal class HashImage
	{
		private HashOptions _options;

		public HashImage(HashOptions options)
		{
			this._options = options;
		}

		internal int Run()
		{
			try
			{
				var startTime = DateTime.Now;
				switch (_options.Type)
				{
					case HashType.Average:
						ComputeAndSaveImageHash(new AverageHash());
						break;
					case HashType.Difference:
						ComputeAndSaveImageHash(new DifferenceHash());
						break;
					case HashType.Perceptual:
					default:
						ComputeAndSaveImageHash(new PerceptualHash());
						break;
					case HashType.All:
						ComputeAndSaveAllHashes();
						break;
					case HashType.Shipwreck:
						var bitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(_options.ImageFile);
						var hash = ImagePhash.ComputeDigest(bitmap.ToLuminanceImage());
						if (String.IsNullOrEmpty(_options.OutputFile))
							Console.Out.WriteLine(hash.ToString());
						else
							File.WriteAllText(_options.OutputFile, hash?.ToString(), Encoding.ASCII);
						break;
				}
				var endTime = DateTime.Now;
				Console.WriteLine("Computing hash took {0}", endTime - startTime);
				return 0;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Console.WriteLine(e.StackTrace);
			}
			return 1;
		}

		private void ComputeAndSaveImageHash(IImageHash hashAlgorithm)
		{
			ulong imageHash;
			using (var stream = File.OpenRead(_options.ImageFile))
			{
				imageHash = hashAlgorithm.Hash(stream);
			}

			var hashString = $"{imageHash:X16}";
			if (String.IsNullOrEmpty(_options.OutputFile))
			{
				Console.Out.WriteLine(hashString);
			}
			else
			{
				File.WriteAllText(_options.OutputFile, hashString, Encoding.ASCII);
			}
		}

		private void ComputeAndSaveAllHashes()
		{
			var bldr = new StringBuilder();
			bldr.AppendLine(Path.GetFileName(_options.ImageFile));
			IImageHash hashAlgorithm = new AverageHash();
			using (var stream = File.OpenRead(_options.ImageFile))
			{
				var imageHash = hashAlgorithm.Hash(stream);
				bldr.AppendLine($"Average Hash: {imageHash:X16}");
			}
			hashAlgorithm = new DifferenceHash();
			using (var stream = File.OpenRead(_options.ImageFile))
			{
				var imageHash = hashAlgorithm.Hash(stream);
				bldr.AppendLine($"Difference Hash: {imageHash:X16}");
			}
			hashAlgorithm = new PerceptualHash();
			using (var stream = File.OpenRead(_options.ImageFile))
			{
				var imageHash = hashAlgorithm.Hash(stream);
				bldr.AppendLine($"Perceptual Hash: {imageHash:X16}");
			}
			var bitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(_options.ImageFile);
			var hash = ImagePhash.ComputeDigest(bitmap.ToLuminanceImage());
			bldr.AppendLine($"Shipwreck Perceptual hash: {hash?.ToString()}");
			var hashString = bldr.ToString();
			if (String.IsNullOrEmpty(_options.OutputFile))
			{
				Console.Out.WriteLine(hashString);
			}
			else
			{
				File.WriteAllText(_options.OutputFile, hashString, Encoding.UTF8);
			}

		}
	}
}