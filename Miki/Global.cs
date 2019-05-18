﻿using Amazon.S3;
using Newtonsoft.Json;
using System.IO;

namespace Miki
{
	/// <summary>
	/// Global data for constant folder structures and versioning.
	/// </summary>
	public class Global
	{
		public static Config Config
		{
			get
			{
				if (config == null)
				{
					if (File.Exists("./miki/settings.json"))
					{
						config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("./miki/settings.json"));
					}
					else
					{
						if (!Directory.Exists("./miki"))
						{
							Directory.CreateDirectory("./miki");
						}
						config = new Config();
						File.WriteAllText("./miki/settings.json", JsonConvert.SerializeObject(config, Formatting.Indented));
					}
				}
				return config;
			}
		}

		public static AmazonS3Client CdnClient
		{
			get
			{
				if (cdnClient == null)
				{
					cdnClient = new AmazonS3Client(Config.CdnAccessKey, Config.CdnSecretKey, new AmazonS3Config()
					{
						ServiceURL = Config.CdnRegionEndpoint
					});
				}
				return cdnClient;
			}
		}

        private static Config config = null;
		private static AmazonS3Client cdnClient;
	}

	public class Constants
	{
		public const string NotDefined = "$not-defined";
	}
}