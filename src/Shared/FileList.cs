﻿#region Copyright 2008 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;
using System.Collections.Generic;
using System.IO;

namespace CSharpTest.Net.Utils
{
	/// <summary>
	/// A utility class for gathering files
	/// </summary>
	internal class FileList : System.Collections.ObjectModel.KeyedCollection<string, FileInfo>
	{
		bool _recurse = true;
		bool _ignoreDirAttrs = false;
		FileAttributes _prohibitAttrib = FileAttributes.Hidden | FileAttributes.Offline | FileAttributes.System;

		/// <summary>
		/// Constructs a FileList containing the files specified or found within the directories
		/// specified.  See Add(string) for more details.
		/// </summary>
		internal FileList(params string[] filesOrDirectories)
			: base(StringComparer.OrdinalIgnoreCase, 0)
		{
			Add(filesOrDirectories);
		}

		/// <summary>
		/// Constructs a FileList containing the files specified or found within the directories
		/// specified.  See Add(string) for more details.  Files and directories that contain the 
		/// attribtes defined in prohibitedAttributes will be ignored, use '0' for everything.
		/// </summary>
		internal FileList(FileAttributes prohibitedAttributes, params string[] filesOrDirectories)
			: base(StringComparer.OrdinalIgnoreCase, 0)
		{
			_prohibitAttrib = prohibitedAttributes;
			Add(filesOrDirectories);
		}

		#region Public Properties
		/// <summary>
		/// Gets or sets a value that allows traversal of all directories added.
		/// </summary>
		public bool RecurseFolders { get { return _recurse; } set { _recurse = value; } }
		/// <summary>
		/// Setting this will greatly improve performance at the cost of not evaluating filters on directories
		/// </summary>
		public bool IgnoreFolderAttributes { get { return _ignoreDirAttrs; } set { _ignoreDirAttrs = value; } }
		/// <summary>
		/// Set this to the set of attributes that if a directory or file contains should be skipped. For
		/// example when set to FileAttributes.Hidden, hidden files and folders will be ignored.
		/// </summary>
		public FileAttributes ProhibitedAttributes { get { return _prohibitAttrib; } set { _prohibitAttrib = value; } }
		#endregion Public Properties

		/// <summary>
		/// Adds a set of items to the collection, see Add(string) for details.
		/// </summary>
		public void Add(params string[] filesOrDirectories)
		{
			if (filesOrDirectories == null) throw new ArgumentNullException();
			foreach (string fd in filesOrDirectories)
				Add(fd);
		}

		/// <summary>
		/// Adds the specified file to the collection.  If the item specified is a directory
		/// that directory will be crawled for files, and optionally (RecurseFolders) child
		/// directories.  If the filename part of the path contains wild-cards they will be
		/// considered throughout the folder tree, i.e: C:\Temp\*.tmp will yeild all files
		/// having an extension of .tmp.  Again if RecurseFolders is true you will get all
		/// .tmp files anywhere in the C:\Temp folder.
		/// </summary>
		public void Add(string fileOrDirectory)
		{
			if (fileOrDirectory == null) throw new ArgumentNullException();

			if (File.Exists(fileOrDirectory))
				AddFile(new FileInfo(fileOrDirectory));
			else if (Directory.Exists(fileOrDirectory))
				AddDirectory(new DirectoryInfo(fileOrDirectory), "*");
			else
			{
				string filePart = Path.GetFileName(fileOrDirectory);
				string dirPart = Path.GetDirectoryName(fileOrDirectory);

				if (Directory.Exists(dirPart) && filePart.IndexOfAny(new char[] { '?', '*' }) >= 0)
					AddDirectory(new DirectoryInfo(dirPart), filePart);
				else
					throw new FileNotFoundException("File not found.", fileOrDirectory);
			}
		}

		public FileInfo[] ToArray()
		{
			return new List<FileInfo>(base.Items).ToArray();
		}

		#region Private / Protected Implementation

		private void AddFile(FileInfo file)
		{
			if (!Allowed(file) || (base.Dictionary != null && base.Dictionary.ContainsKey(file.FullName)))
				return;

			if (FileFound != null)
			{
				FileFoundEventArgs args = new FileFoundEventArgs(false, file);
				FileFound(this, args);
				if (args.Ignore)
					return;
			}
			base.Add(file);
		}

		private void AddDirectory(DirectoryInfo dir, string match)
		{
			if (!_ignoreDirAttrs && !Allowed(dir))
				return;

			SearchOption deepMatch = SearchOption.TopDirectoryOnly;
			if (_recurse && (_ignoreDirAttrs == true || _prohibitAttrib == 0))
				deepMatch = SearchOption.AllDirectories;

			foreach (FileInfo f in dir.GetFiles(match, deepMatch))
				AddFile(f);

			if (_recurse && deepMatch != SearchOption.AllDirectories)
			{
				foreach (DirectoryInfo child in dir.GetDirectories())
					AddDirectory(child, match);
			}
		}

		private bool Allowed(FileSystemInfo item)
		{
			if ((_prohibitAttrib & item.Attributes) != 0)
				return false;

			return true;
		}

		protected override string GetKeyForItem(FileInfo item)
		{
			return item.FullName;
		}

		#endregion

		#region FileFoundEvent

		/// <summary>
		/// Raised when a new file is about to be added to the collection, set e.Ignore
		/// to true will cancel the addition of this file.
		/// </summary>
		public event EventHandler<FileFoundEventArgs> FileFound;

		public class FileFoundEventArgs : EventArgs
		{
			public bool Ignore;
			public readonly FileInfo File;

			public FileFoundEventArgs(bool ignore, FileInfo file)
			{
				this.Ignore = ignore;
				this.File = file;
			}
		}

		#endregion FileFoundEvent
	}
}
