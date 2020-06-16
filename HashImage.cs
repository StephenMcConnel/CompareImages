// Copyright ©  2020 SIL International.  Licensed under GPL 3.0 (so as to use Shipwreck library).
using System;
using System.IO;
using System.Text;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Shipwreck.Phash;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
			ulong imageHash = ComputeHashOfImageFile(_options.ImageFile, hashAlgorithm);
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

		private ulong ComputeHashOfImageFile(string path, IImageHash hashAlgorithm)
		{
			using (var image = (Image<Rgba32>)Image.Load(path))
			{
				// check whether we have R=G=B=0 (ie, black) for all pixels, presumably with A varying.
				var allBlack = true;
				for (int x = 0; allBlack && x < image.Width; ++x)
				{
					for (int y = 0; allBlack && y < image.Height; ++y)
					{
						var pixel = image[x, y];
						if (pixel.R != 0 || pixel.G != 0 || pixel.B != 0)
							allBlack = false;
					}
				}
				if (allBlack)
				{
					// If the pixels all end up the same because A never changes, we're no worse off
					// because the hash result will still be all zero bits.
					for (int x = 0; x < image.Width; ++x)
					{
						for (int y = 0; y < image.Height; ++y)
						{
							var pixel = image[x, y];
							pixel.R = pixel.A;
							pixel.G = pixel.A;
							pixel.B = pixel.A;
							image[x, y] = pixel;
						}
					}
				}
				return hashAlgorithm.Hash(image);
			}
		}

		private void ComputeAndSaveAllHashes()
		{
			var bldr = new StringBuilder();
			bldr.AppendLine(Path.GetFileName(_options.ImageFile));
			var imageHash = ComputeHashOfImageFile(_options.ImageFile, new AverageHash());
			bldr.AppendLine($"CoenM Average Hash: {imageHash:X16}");
			imageHash = ComputeHashOfImageFile(_options.ImageFile, new DifferenceHash());
			bldr.AppendLine($"CoenM Difference Hash: {imageHash:X16}");
			imageHash = ComputeHashOfImageFile(_options.ImageFile, new PerceptualHash());
			bldr.AppendLine($"CoenM Perceptual Hash: {imageHash:X16}");
			// We don't try to handle Alpha-only B&W PNG image files with these hashes.
			using (var bitmap = (System.Drawing.Bitmap) System.Drawing.Image.FromFile(_options.ImageFile))
			{
				var hashDigest = ImagePhash.ComputeDigest(bitmap.ToLuminanceImage());
				bldr.AppendLine($"Shipwreck Perceptual Hash: {hashDigest?.ToString()}");
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
}
