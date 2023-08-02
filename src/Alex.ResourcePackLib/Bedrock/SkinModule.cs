using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using Alex.Interfaces.Resources;
using Alex.ResourcePackLib.Abstraction;
using Alex.ResourcePackLib.IO;
using Alex.ResourcePackLib.IO.Abstract;
using Alex.ResourcePackLib.Json;
using Alex.ResourcePackLib.Json.Bedrock;
using Alex.ResourcePackLib.Json.Models.Entities;
using Alex.ResourcePackLib.Json.Textures;
using NLog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Alex.ResourcePackLib.Bedrock
{
	public class SkinModule : MCPackModule, ITextureProvider
	{
		private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

		private static PngDecoder PngDecoder { get; } = new PngDecoder() { IgnoreMetadata = true };

		/// <inheritdoc />
		public override string Name
		{
			get
			{
				return Info?.LocalizationName ?? "Unknown";
			}
		}


		public MCPackSkins Info { get; private set; }
		//public LoadedSkin[] Skins { get; private set; }

		public IReadOnlyDictionary<string, EntityModel> EntityModels { get; private set; }

		/// <inheritdoc />
		internal SkinModule(IFilesystem entry) : base(entry) { }

		/// <inheritdoc />
		internal override bool Load()
		{
			try
			{
				var archive = Entry;

				//using (var archive = new ZipFileSystem(Entry.Open(), Entry.Name))
				{
					var skinsEntry = base.SearchEntry("skins.json");

					if (skinsEntry == null)
						return false;

					Info = MCJsonConvert.DeserializeObject<MCPackSkins>(skinsEntry.ReadAsString());

					// check for language file
                    var textEntry = base.SearchEntry("en_US.lang");

                    if (textEntry != null)
					{
						string[] skinNames = textEntry.ReadAsString().Split(Environment.NewLine);
						Dictionary<string, string> skinDict = new Dictionary<string, string>();
						foreach(var skinName in skinNames)
						{
							try
                            {
                                string[] stringDict = skinName.Split("=");
                                skinDict.Add(stringDict[0], stringDict[1]);
                            }
							catch (Exception e) { }
                        }

						
						foreach (SkinEntry skinEntry in Info.Skins)
						{
							if (skinDict.TryGetValue($"skin.{Name}.{skinEntry.LocalizationName}", out var skin))
							{
								skinEntry.LocalizationName = skin;
							}
							else
							{
								skinEntry.LocalizationName = skinEntry.LocalizationName.Replace("_", " ");
							}
						}
					}


                    IFile geometryEntry = null;

                    try
					{
						geometryEntry = base.SearchEntry("geometry.json");
                    }
					catch (Exception ex)
					{
						if (!ex.Message.Contains("No entry"))
						{
							throw ex;
						}
					}

					if (geometryEntry != null)
					{
						ProcessGeometryJson(geometryEntry);
					}
					else
					{
						EntityModels = new Dictionary<string, EntityModel>();
					}
				}

				//Skins = skins.ToArray();

				return true;
			}
			catch (InvalidDataException ex)
			{
				Log.Debug(ex, $"Could not load module.");
			}

			return false;
		}

		private void ProcessGeometryJson(IFile entry)
		{
			try
			{
				Dictionary<string, EntityModel> entityModels = new Dictionary<string, EntityModel>();
				MCBedrockResourcePack.LoadEntityModel(entry.ReadAsString(), entityModels);
				entityModels = MCBedrockResourcePack.ProcessEntityModels(entityModels);

				EntityModels = entityModels;
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Could not process skinpack geometry.");
			}
		}

		/// <inheritdoc />
		public bool TryGetBitmap(ResourceLocation textureName, out Image<Rgba32> bitmap)
		{
			bitmap = null;
			var textureEntry = base.SearchEntry(textureName.Path);


			if (textureEntry == null)
				return false;

			Image<Rgba32> img = null;

			try
			{
                using (Stream s = textureEntry.Open())
                {
                    //img = new Bitmap(s);
                    img = Image.Load<Rgba32>(s.ReadToSpan(textureEntry.Length), PngDecoder);
                }
			}
			catch (Exception e)
            {
                /*using (var archive = new ZipFileSystem(File.Open(archivePath, FileMode.Open, FileAccess.Read), textureName.Path))
                {
                    using (Stream s = textureEntry.Open())
                    {
                        //img = new Bitmap(s);
                        img = Image.Load<Rgba32>(s.ReadToSpan(textureEntry.Length), PngDecoder);
                    }
                }*/
            }
			
			if (img == null)
			{
				return false;
			}
			bitmap = img;

			return true;
		}

		/// <inheritdoc />
		public bool TryGetTextureMeta(ResourceLocation textureName, out TextureMeta meta)
		{
			throw new System.NotImplementedException();
		}
	}

	public class LoadedSkin
	{
		public string Name { get; }
		public EntityModel Model { get; }
		public Image<Rgba32> Texture { get; }

		public LoadedSkin(string name, EntityModel model, Image<Rgba32> texture)
		{
			Name = name;
			Model = model;
			Texture = texture;
		}
	}
}