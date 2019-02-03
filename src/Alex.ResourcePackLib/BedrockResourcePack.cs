﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.AccessControl;
using Alex.ResourcePackLib.Json;
using Alex.ResourcePackLib.Json.Converters;
using Alex.ResourcePackLib.Json.Models;
using Alex.ResourcePackLib.Json.Models.Blocks;
using Alex.ResourcePackLib.Json.Models.Entities;
using Alex.ResourcePackLib.Json.Textures;
using ICSharpCode.SharpZipLib.Zip;

using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Alex.ResourcePackLib
{
	public class BedrockResourcePack : IDisposable
	{
		private static readonly Logger Log = LogManager.GetCurrentClassLogger(typeof(BedrockResourcePack));
		private Dictionary<string, EntityModel> _processedModels = new Dictionary<string, EntityModel>();
		public IReadOnlyDictionary<string, EntityModel> EntityModels => _processedModels;

        public IReadOnlyDictionary<string, Bitmap> Textures { get; private set; } = new ConcurrentDictionary<string, Bitmap>();
		public IReadOnlyDictionary<string, TextureInfoJson> TextureJsons { get; private set; } = new ConcurrentDictionary<string, TextureInfoJson>();

		public IReadOnlyDictionary<string, EntityDefinition> EntityDefinitions { get; private set; } = new ConcurrentDictionary<string, EntityDefinition>();

		private readonly DirectoryInfo _workingDir;

		public BedrockResourcePack(DirectoryInfo directory)
		{
			_workingDir = directory;

			Load();
		}

		public bool TryGetTexture(string name, out Bitmap texture)
		{
			return Textures.TryGetValue(NormalisePath(name), out texture);
		}

		public bool TryGetTextureJson(string name, out TextureInfoJson textureJson)
		{
			return TextureJsons.TryGetValue(NormalisePath(name), out textureJson);
		}

		private string NormalisePath(string path)
		{
			return path.Replace('\\', '/').ToLowerInvariant();
		}

		private void Load()
		{
            DirectoryInfo entityDirectory = null;
            DirectoryInfo entityDirectory2 = null;
            DirectoryInfo entityDefinitionsDir = null;
            FileInfo mobsFile = null;

            Dictionary<string, FileInfo> entityGeometry = new Dictionary<string, FileInfo>();
            foreach (var dir in _workingDir.EnumerateDirectories())
            {
                if (entityDirectory2 == null && dir.Name.Equals("entity"))
                {
                    entityDirectory2 = dir;
                    foreach (var file in dir.EnumerateFiles())
                    {
                        if (!entityGeometry.TryAdd(file.Name, file))
                        {
	                        if (entityGeometry.TryGetValue(file.Name, out var current))
	                        {
		                        if (current.LastWriteTimeUtc < file.LastWriteTimeUtc)
		                        {
			                        entityGeometry[file.Name] = file;
									continue;
		                        }
	                        }
                            Log.Warn($"Failed to add to entity geo dictionary (0)! {file.Name}");
                        }
                    }
                    continue;
                }

                if (entityDefinitionsDir == null && dir.Name.Equals("definitions"))
                {
                    foreach (var d in dir.EnumerateDirectories())
                    {
                        if (d.Name.Equals("entity"))
                        {
                            entityDefinitionsDir = d;

                            foreach (var file in d.EnumerateFiles())
                            {
                                if (!entityGeometry.TryAdd(file.Name, file))
                                {
	                                if (entityGeometry.TryGetValue(file.Name, out var current))
	                                {
		                                if (current.LastWriteTimeUtc < file.LastWriteTimeUtc)
		                                {
			                                entityGeometry[file.Name] = file;
			                                continue;
		                                }
	                                }
                                    Log.Warn($"Failed to add to entity geo dictionary (1)! {file.Name}");
                                }
                            }

                            break;
                        }
                    }
                }

                if (dir.Name.Equals("models"))
                {
                    if (entityDirectory == null)
                    {
                        foreach (var d in dir.EnumerateDirectories())
                        {
                            if (d.Name.Equals("entity"))
                            {
                                entityDirectory = dir;

                                foreach (var file in d.EnumerateFiles())
                                {
                                    if (!entityGeometry.TryAdd(file.Name, file))
                                    {
	                                    if (entityGeometry.TryGetValue(file.Name, out var current))
	                                    {
		                                    if (current.LastWriteTimeUtc < file.LastWriteTimeUtc)
		                                    {
			                                    entityGeometry[file.Name] = file;
			                                    continue;
		                                    }
	                                    }
                                        Log.Warn($"Failed to add to entity geo dictionary (2)! {file.Name}");
                                    }
                                }

                                break;
                            }
                        }
                    }

                    if (mobsFile == null)
                    {
                        foreach (var file in dir.EnumerateFiles())
                        {
                            if (file.Name.Equals("mobs.json"))
                            {
                                mobsFile = file;
                                break;
                            }
                        }
                    }
                }

                if (entityDirectory != null && mobsFile != null && entityDefinitionsDir != null && entityDirectory2 != null)
                    break;
            }

            if (entityDirectory == null || !entityDirectory.Exists)
            {
                Log.Warn("Could not find entity folder!");
                return;
            }

            if (entityDefinitionsDir == null || !entityDefinitionsDir.Exists)
            {
                Log.Warn("Could not find entity definitions folder!");
                return;
            }

            if (mobsFile == null || !mobsFile.Exists)
            {
                Log.Warn("Could not find mob entity definitions! ('mobs.json')");
                return;
            }

            Dictionary<string, EntityDefinition> entityDefinitions = new Dictionary<string, EntityDefinition>();
            foreach (var def in entityDefinitionsDir.EnumerateFiles())
            {
                LoadEntityDefinition(def, entityDefinitions);
            }

            EntityDefinitions = entityDefinitions;

            var res = new Dictionary<string, EntityModel>();
            GetEntries(mobsFile, res);

            int missed1 = LoadMobs(res);

            res.Clear();

            foreach (var file in entityGeometry.Values)
            {
                GetEntries(file, res);
            }

            int missed2 = LoadMobs(res);

            if (missed1 > 0 || missed2 > 0)
            {
                Log.Warn($"Failed to process {missed1 + missed2} entity models");
            }

            Log.Info($"Processed {EntityModels.Count} entity models!");
            Log.Info($"Processed {EntityDefinitions.Count} entity definitions");
        }

		private void LoadEntityDefinition(FileInfo entry, Dictionary<string, EntityDefinition> entityDefinitions)
		{
			using (var open = entry.OpenText())
			{
				var json = open.ReadToEnd();

				string fileName = Path.GetFileNameWithoutExtension(entry.Name);

				Dictionary<string, EntityDefinition> definitions = JsonConvert.DeserializeObject<Dictionary<string, EntityDefinition>>(json);
				foreach (var def in definitions)
				{
					def.Value.Filename = fileName;
					if (!entityDefinitions.ContainsKey(def.Key))
					{
						entityDefinitions.Add(def.Key, def.Value);
					}
				}
			}
		}
		private void GetEntries(FileInfo file, Dictionary<string, EntityModel> entries)
		{
			using (var open = file.OpenText())
			{
				var json = open.ReadToEnd();
				JObject obj = JObject.Parse(json, new JsonLoadSettings());

				foreach (var e in obj)
				{
					if (e.Key == "format_version") continue;
					//if (e.Key == "minecraft:client_entity") continue;
					//if (e.Key.Contains("zombie")) Console.WriteLine(e.Key);
					entries.TryAdd(e.Key, e.Value.ToObject<EntityModel>(new JsonSerializer()
					{
						Converters = { new Vector3Converter(), new Vector2Converter() }
					}));
				}
			}
		}

       /* private void LoadTexture(ZipArchiveEntry entry)
		{
			var stream = new StreamReader(entry.Open());
			var json = stream.ReadToEnd();

			Dictionary<string, Bitmap> textures = new Dictionary<string, Bitmap>();
			Dictionary<string, TextureInfoJson> textureJsons = new Dictionary<string, TextureInfoJson>();

			string[] definitions = JsonConvert.DeserializeObject<string[]>(json);
			foreach (string def in definitions)
			{
				if (textures.ContainsKey(def))
					continue;
				
				var e = _archive.GetEntry(def + ".png");
				if (e != null && e.IsFile())
				{
                    Bitmap bmp = new Bitmap(e.Open());
					textures.Add(NormalisePath(def), bmp);
				}

				e = _archive.GetEntry(def + ".json");
				if (e != null && e.IsFile())
				{
					using(var eStream = e.Open())
					using (var sr = new StreamReader(eStream))
					{
						var textureJson = sr.ReadToEnd();
						var textureInfo = MCJsonConvert.DeserializeObject<TextureInfoJson>(textureJson);
						textureJsons.Add(NormalisePath(def), textureInfo);
					}
				}
			}	

			Textures = textures;
			TextureJsons = textureJsons;
			Log.Info($"Loaded {textures.Count} textures and {textureJsons.Count} texture definitions");
		}*/

        /*private class MobsDefFile
		{
			[JsonProperty("format_version")]
            public string FormatVersion { get; set; }

			public Dictionary<string, EntityModel> Entries { get; }
		}
	}*/

        private Dictionary<string, TValue> OrderByLength<TValue>(Dictionary<string, TValue> dictionary)
        {
            return dictionary.OrderBy(obj => obj.Key.Length).ToDictionary(obj => obj.Key, obj => obj.Value);
        }

        private Dictionary<string, TValue> OrderByChild<TValue>(Dictionary<string, TValue> dictionary)
        {
            return dictionary.OrderBy(obj => obj.Key.Contains(":")).ToDictionary(obj => obj.Key, obj => obj.Value);
        }

        private int LoadMobs(Dictionary<string, EntityModel> entries)
        {
            int c = 0;

            List<string> laterStages = new List<string>();
            Dictionary<string, EntityModel> orderedDict = new Dictionary<string, EntityModel>();
            Dictionary<string, EntityModel> failedToProcess = new Dictionary<string, EntityModel>();

            foreach (var (key, value) in entries)
            {
                if (!key.Contains(":"))
                {
                    if (!orderedDict.TryAdd(key, value))
                    {
                        Log.Warn($"Failed to add to dictionary! {key}");
                    }
                }
                else
                {
                    if (!laterStages.Contains(key))
                        laterStages.Add(key);
                }
            }

            orderedDict = OrderByLength(orderedDict);


            foreach (var late in laterStages.ToArray())
            {
                var split = late.Split(':');
                string parent = split[1];
                string kid = split[0];

                if (orderedDict.TryGetValue(parent, out EntityModel _))
                {
                    if (orderedDict.TryAdd(late, entries[late]))
                    {
                        laterStages.Remove(late);
                    }
                    else
                    {
                        Log.Warn($"Could not add to ordered dictionary!");
                    }
                }
                else
                {
                    Log.Warn($"Unresolved entity: {late}");
                }
            }

            orderedDict = OrderByChild(orderedDict);

            foreach (var (key, value) in orderedDict)
            {
                value.Name = key;

                if (_processedModels.ContainsKey(key))
                    continue;

                ProcessEntityModel(value, entries, failedToProcess, false);
            }

            var retryCopy = new Dictionary<string, EntityModel>(failedToProcess.ToArray());

            int fix = 0;
            foreach (var e in retryCopy)
            {
                if (ProcessEntityModel(e.Value, entries, failedToProcess, true))
                {
                    fix++;
                }
            }

            c = retryCopy.Count;

            return failedToProcess.Count - fix;
        }

        private bool ProcessEntityModel(EntityModel model, Dictionary<string, EntityModel> models,
            Dictionary<string, EntityModel> failedToProcess, bool isRetry = false)
        {
            string modelName = model.Name;
            if (model.Name.Contains(":")) //This model inherits from another model.
            {
                string[] split = model.Name.Split(':');
                string parent = split[1];

                if (!_processedModels.TryGetValue(parent, out var parentModel))
                {
                    if (!isRetry)
                    {
                        failedToProcess.TryAdd(modelName, model);

                        Log.Warn($"No parent model for {modelName}");
                    }

                    return false;
                }

                modelName = split[0];

                if (model.Bones == null)
                {
                    model.Bones = new EntityModelBone[0];
                }

                if (parentModel == null)
                {
                    Log.Warn($"Pass 1 fail... {modelName}");
                    return false;
                }

                if (parentModel.Bones == null || parentModel.Bones.Length == 0)
                {
                    Log.Warn($"Parent models contains no bones! {modelName}");
                    return false;
                }

                Dictionary<string, EntityModelBone> parentBones =
                    parentModel.Bones.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name))
                        .ToDictionary(x => x.Name, e => e);

                Dictionary<string, EntityModelBone> bones =
                    model.Bones.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name))
                        .ToDictionary(x => x.Name, e => e);

                foreach (var bone in parentBones)
                {
                    var parentBone = bone.Value;
                    if (bones.TryGetValue(bone.Key, out EntityModelBone val))
                    {
                        if (!val.Reset)
                        {
                            if (val.Cubes != null)
                            {
                                val.Cubes = val.Cubes.Concat(parentBone.Cubes).ToArray();
                            }
                            else
                            {
                                val.Cubes = parentBone.Cubes;
                            }

                            //val.Cubes.Concat(parentBone.Cubes);
                        }


                        bones[bone.Key] = val;
                    }
                    else
                    {
                        bones.Add(bone.Key, parentBone);
                    }
                }

                model.Bones = bones.Values.ToArray();
            }

            return _processedModels.TryAdd(modelName, model);
        }

        private void ProcessBlockModel(BedrockBlockModel blockModel, Dictionary<string, BedrockBlockModel> blockModels,
			List<string> processedModels)
		{

		}

		public class EntityDefinition
		{
			[JsonIgnore] public string Filename { get; set; } = string.Empty;

			public Dictionary<string, string> Textures;
			public Dictionary<string, string> Geometry;
		}

		public void Dispose()
		{
			//_archive?.Dispose();
		}
	}
}