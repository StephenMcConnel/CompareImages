using System;
using System.Globalization;
using System.IO;
using CommandLine;
using String = System.String;

namespace CompareImages
{
	class Program
	{
		static int Main(string[] args)
		{
			//// See https://github.com/commandlineparser/commandline for documentation about CommandLine.Parser
			return Parser.Default.ParseArguments<HashOptions, CompareOptions>(args)
				.MapResult(
					(HashOptions opts) => HashAndReturnExitCode(opts),
					(CompareOptions opts) => CompareAndReturnExitCode(opts),
					errs => 1);
		}

		private static int HashAndReturnExitCode(HashOptions opts)
		{
			return new HashImage(opts).Run();
		}

		private static int CompareAndReturnExitCode(CompareOptions opts)
		{
			if (opts.ValidateOptions())
				return new CompareImageHashes(opts).Run();
			else
				return 1;
		}
	}

	public enum HashType
	{
		Perceptual,
		Difference,
		Average,
		Shipwreck,
		All
	}

	[Verb("hash", HelpText = "Compute a hash of the given image.")]
	public class HashOptions
	{
		[Option('t', "type", Default = HashType.Perceptual, Required = false, HelpText = "Type of hash to perform: perceptual, difference, or average.")]
		public HashType Type { get; set; }

		[Option('o', "output", Required = false, HelpText = "Output file for storing the computed hash.  Stdout is used by default.")]
		public string OutputFile { get; set; }

		[Value(0, Required = true, HelpText = "Image file to hash.")]
		public string ImageFile { get; set; }
	}

	[Verb("compare", HelpText = "Compare two images or hashes.")]
	public class CompareOptions
	{
		[Option('t', "type", Default = HashType.Perceptual, Required = false, HelpText = "Type of hash to perform: perceptual, difference, or average.")]
		public HashType Type { get; set; }

		[Option('o', "output", Required = false, HelpText = "Output file for storing the hash comparison metrics.  Stdout is used by default.")]
		public string OutputFile { get; set; }

		[Value(0, Required = true, HelpText = "Image file to hash, or hash to compare.")]
		public string Image1 { get; set; }

		[Value(1, Required = true, HelpText = "Image file to hash, or hash to compare.")]
		public string Image2 { get; set; }

		public bool ValidateOptions()
		{
			if (!IsValidUlong(Image1) && !IsValidFile(Image1))
			{
				Console.WriteLine("The first argument must be either a value 16-digit hex hash value or a valid image filename.");
				return false;
			}
			if (!IsValidUlong(Image2) && !IsValidFile(Image2))
			{
				Console.WriteLine("The second argument must be either a value 16-digit hex hash value or a valid image filename.");
				return false;
			}

			if (Type == HashType.All && (IsValidUlong(Image1) || IsValidUlong(Image2)))
			{
				Console.WriteLine("If type is All, then both arguments must be image file paths.");
				return false;
			}
			return true;
		}

		internal bool IsValidUlong(string hash)
		{
			return !String.IsNullOrEmpty(hash) && ulong.TryParse(hash, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
		}

		internal bool IsValidFile(string path)
		{
			return !String.IsNullOrEmpty(path) && File.Exists(path);
		}
	}
}
