using System;
using System.Collections.Generic;
using System.Linq;
using Alex.Graphics.Models.Blocks;

namespace Alex.Blocks.State
{
    public sealed class BlockStateVariantMapper
    {
        private static NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger(typeof(BlockStateVariantMapper));
        private HashSet<BlockState> Variants { get; } = new HashSet<BlockState>();
        
        public  bool       IsMultiPart { get; set; } = false;
        
        private BlockModel _model = null;
        public BlockModel Model
        {
            get
            {
                return _model ?? new MissingBlockModel();
            }
            set
            {
                _model = value;
            }
        }
        
        public BlockStateVariantMapper(List<BlockState> variants)
        {
            Variants = new HashSet<BlockState>(variants);

            foreach (var variant in variants)
            {
                variant.VariantMapper = this;
            }
        }
		
        public bool TryResolve(BlockState source, string property, string value, out BlockState result, params string[] requiredMatches)
        {
            //var clone = source.WithPropertyNoResolve(property, value, true);

            var clone = source.Clone();
            
            List<StateProperty> properties = new List<StateProperty>();
            foreach (var prop in clone.States)
            {
                var p = prop;

                if (p.Name.Equals(property, StringComparison.InvariantCultureIgnoreCase))
                {
                    p = p.WithValue(value);
                }
                
                properties.Add(p);
            }
            
            clone.States = properties;
            
            if (Variants.TryGetValue(clone, out var actualValue))
            {
                result = actualValue;
                return true;
            }

            result = source;
            return false;
            /*
            //  property = property.ToLowerInvariant();
          //  value = value;

            int highestMatch = 0;
            BlockState highest = null;

            List<BlockState> list = new List<BlockState>(Variants.Count);

            foreach (var x in Variants)
            {
                if (x.TryGetValue(property, out var v) && string.Equals(v, value, StringComparison.Ordinal))
                {
                    list.Add(x);
                }
            }

            var matching = list;

            if (matching.Count == 1)
            {
                result = matching.FirstOrDefault();
                return true;
            }
            else if (matching.Count == 0)
            {
                result = source;

                return false;
            }
            
            var copiedProperties = new Dictionary<string, string>(source, StringComparer.OrdinalIgnoreCase);
            copiedProperties[property] = value.ToString();

            foreach (var variant in matching)
            {
                bool valid = true;
                foreach (var requiredMatch in requiredMatches)
                {
                    if (!(copiedProperties.TryGetValue(requiredMatch, out string copyValue) 
                          && variant.TryGetValue(requiredMatch, out string variantValue) && copyValue == variantValue))
                    {
                        valid = false;
                        break;
                    }
                }
				
                if (!valid)
                    continue;
				
                int matches = 0;
                foreach (var copy in copiedProperties)
                {
                    if (copy.Key.Equals(property))
                        continue;
                    
                    //Check if variant value matches copy value.
                    if (variant.TryGetValue(copy.Key, out string val) && copy.Value.Equals(val, StringComparison.OrdinalIgnoreCase))
                    {
                        matches++;
                    }
                }

                /*foreach (var variantProp in variant)
                {
                    if (!source.Contains(variantProp.Key))
                    {
                        matches--;
                    }
                }*\/

                if (matches == source.Count)
                {
                    highestMatch = matches;
                    highest = variant;

                    break;
                }
                
                if (matches > highestMatch)
                {
                    highestMatch = matches;
                    highest = variant;
                }
            }

            if (highest != null)
            {
                result = highest;
                return true;
            }

            result = null;
            return false;*/
        }

        public BlockState[] GetVariants()
        {
            return Variants.ToArray();
        }

        public BlockState GetDefaultState()
        {
            return Variants.FirstOrDefault(x => x.Default);
        }
    }
}