using Alex.ResourcePackLib.Exceptions;
using Alex.ResourcePackLib.IO.Abstract;
using System.Linq;

namespace Alex.ResourcePackLib.Bedrock
{
	public class MCPackModule
	{
		public virtual string Name
		{
			get
			{
				return Entry.Name;
			}
		}

		protected IFilesystem Entry { get; }

		protected MCPackModule(IFilesystem entry)
		{
			Entry = entry;
		}

		internal virtual bool Load()
		{
			return false;
		}

		protected IFile SearchEntry(string name)
		{
            var foundEntries = Entry.Entries.Where(e => e.Name.ToLower() == name.ToLower());
            //var contentEntry  = archive.GetEntry("content.zipe");

            if (!foundEntries.Any())
            {
                throw new InvalidMCPackException($"No entry of {name} found!");
            }

            if (foundEntries.Count() > 1)
            {
                throw new InvalidMCPackException($"Multiple entries of {name} found!");
            }

            return foundEntries.First();
        }

	}
}