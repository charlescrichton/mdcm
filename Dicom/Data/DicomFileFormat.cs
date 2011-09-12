// mDCM: A C# DICOM library
//
// Copyright (c) 2006-2008  Colby Dillion
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Author:
//    Colby Dillion (colby.dillion@gmail.com)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Dicom.Codec;
using Dicom.IO;
using Dicom.Utility;

namespace Dicom.Data {
	/// <summary>
	/// User class for loading and saving DICOM files
	/// </summary>
	public class DicomFileFormat {
		#region Private Members
		private DcmFileMetaInfo _metainfo;
		private DcmDataset _dataset;
		#endregion

		/// <summary>
		/// Initializes new DICOM file format
		/// </summary>
		public DicomFileFormat() {
		}

		/// <summary>
		/// Initializes new DICOM file format from dataset
		/// </summary>
		/// <param name="dataset">Dataset</param>
		public DicomFileFormat(DcmDataset dataset) {
			_metainfo = new DcmFileMetaInfo();
			_metainfo.FileMetaInformationVersion = DcmFileMetaInfo.Version;
			_metainfo.MediaStorageSOPClassUID = dataset.GetUID(DicomTags.SOPClassUID);
			_metainfo.MediaStorageSOPInstanceUID = dataset.GetUID(DicomTags.SOPInstanceUID);
			_metainfo.TransferSyntax = dataset.InternalTransferSyntax;
			_metainfo.ImplementationClassUID = Implementation.ClassUID;
			_metainfo.ImplementationVersionName = Implementation.Version;
			_metainfo.SourceApplicationEntityTitle = "";
			_dataset = dataset;
		}

		/// <summary>
		/// File Meta Information
		/// </summary>
		public DcmFileMetaInfo FileMetaInfo {
			get {
				if (_metainfo == null)
					_metainfo = new DcmFileMetaInfo();
				return _metainfo;
			}
		}

		/// <summary>
		/// DICOM Dataset
		/// </summary>
		public DcmDataset Dataset {
			get { return _dataset; }
		}

		/// <summary>
		/// Changes transfer syntax of dataset and updates file meta information
		/// </summary>
		/// <param name="ts">New transfer syntax</param>
		/// <param name="parameters">Encode/Decode params</param>
		public void ChangeTransferSytnax(DicomTransferSyntax ts, DcmCodecParameters parameters) {
			Dataset.ChangeTransferSyntax(ts, parameters);
			FileMetaInfo.TransferSyntax = ts;
		}

        /// <summary>
        /// Gets the file meta information from a DICOM file
        /// </summary>
        /// <param name="file">Filename</param>
        /// <returns>File meta information</returns>
        public static DcmFileMetaInfo LoadFileMetaInfo(String file)
        {
            using (FileStream fs = File.OpenRead(file))
            {
                DcmFileMetaInfo metainfo = LoadFileMetaInfo(fs);
                fs.Close();
                return metainfo;
            }
        }

        /// <summary>
        /// Loads the meta information from a DICOM file in a stream.
        /// Note that the caller is expected to dispose of the stream after usage.
        /// </summary>
        /// <param name="stream">The stream fontaining the DICOM file</param>
        /// <returns>File meta information</returns>
        public static DcmFileMetaInfo LoadFileMetaInfo(Stream stream)
        {
            stream.Seek(128, SeekOrigin.Begin);
            CheckFileHeader(stream);
            DicomStreamReader dsr = new DicomStreamReader(stream);
            DcmFileMetaInfo metainfo = new DcmFileMetaInfo();
            dsr.Dataset = metainfo;
            dsr.Read(DcmFileMetaInfo.StopTag, DicomReadOptions.Default | DicomReadOptions.FileMetaInfoOnly);
            return metainfo;
        }

        /// <summary>
        /// Loads a dicom file
        /// </summary>
        /// <param name="file">Filename</param>
        /// <param name="options">DICOM read options</param>
        public DicomReadStatus Load(String file, DicomReadOptions options)
        {
            return Load(file, null, options);
        }

        /// <summary>
        /// Loads a dicom file from a stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="options">DICOM read options</param>
        public DicomReadStatus Load(Stream stream, DicomReadOptions options)
        {
            return Load(stream, null, options);
        }

        /// <summary>
        /// Loads a dicom file, stopping at a certain tag
        /// </summary>
        /// <param name="file">Filename</param>
        /// <param name="stopTag">Tag to stop parsing at</param>
        /// <param name="options">DICOM read options</param>
        public DicomReadStatus Load(String file, DicomTag stopTag, DicomReadOptions options)
        {
            using (FileStream fs = File.OpenRead(file))
            {
                DicomReadStatus status = Load(fs, stopTag, options);

                fs.Close();

                return status;
            }
        }

        /// <summary>
        /// Loads a dicom file from a stream, stopping at a certain tag
        /// </summary>
        /// <param name="stream">Stream containing DICOM file</param>
        /// <param name="stopTag">Tag to stop parsing at</param>
        /// <param name="options">DICOM read options</param>
        public DicomReadStatus Load(Stream stream, DicomTag stopTag, DicomReadOptions options)
        {
            stream.Seek(128, SeekOrigin.Begin);
            CheckFileHeader(stream);
            DicomStreamReader dsr = new DicomStreamReader(stream);

            _metainfo = new DcmFileMetaInfo();
            dsr.Dataset = _metainfo;
            dsr.Read(DcmFileMetaInfo.StopTag, options | DicomReadOptions.FileMetaInfoOnly);

            if (_metainfo.TransferSyntax.IsDeflate)
            {
                MemoryStream ms = StreamUtility.Deflate(stream, false);
                dsr = new DicomStreamReader(ms);
            }

            _dataset = new DcmDataset(_metainfo.TransferSyntax);
            dsr.Dataset = _dataset;
            DicomReadStatus status = dsr.Read(stopTag, options);
            return status;
        }

        /// <summary>
        /// Determine if the file is a DICOM file.
        /// </summary>
        /// <param name="file">File to inspect.</param>
        /// <returns></returns>
        public static bool IsDicomFile(string file)
        {
            bool isDicom = false;
            using (FileStream fs = File.OpenRead(file))
            {
                isDicom = IsDicomStream(fs);
                fs.Close();
            }
            return isDicom;
        }

        /// <summary>
        /// Determine if a stream contains a DICOM file.
        /// </summary>
        /// <param name="stream">The stream to inspect.</param>
        /// <returns></returns>
        public static bool IsDicomStream(Stream stream)
        {
            bool isDicom = false;
            stream.Seek(128, SeekOrigin.Begin);
            if (stream.ReadByte() == (byte)'D' ||
                stream.ReadByte() == (byte)'I' ||
                stream.ReadByte() == (byte)'C' ||
                stream.ReadByte() == (byte)'M')
                isDicom = true;

            return isDicom;
        }

        private static void CheckFileHeader(Stream stream)
        {
            if (stream.ReadByte() != (byte)'D' ||
                stream.ReadByte() != (byte)'I' ||
                stream.ReadByte() != (byte)'C' ||
                stream.ReadByte() != (byte)'M')
            {
                FileStream fs = stream as FileStream;
                if (fs != null)
                {
                    throw new DicomDataException("Invalid DICOM file: " + fs.Name);
                }
                else
                {
                    throw new DicomDataException("Invalid DICOM file in stream.");
                }
            }
        }

        /// <summary>
        /// Gets file stream starting at DICOM dataset
        /// </summary>
        /// <param name="file">Filename</param>
        /// <returns>File stream</returns>
        public static FileStream GetDatasetStream(String file)
        {
            FileStream fs = File.OpenRead(file);
            fs.Seek(128, SeekOrigin.Begin);
            CheckFileHeader(fs);
            DicomStreamReader dsr = new DicomStreamReader(fs);
            DcmFileMetaInfo metainfo = new DcmFileMetaInfo();
            dsr.Dataset = metainfo;
            if (dsr.Read(DcmFileMetaInfo.StopTag, DicomReadOptions.Default | DicomReadOptions.FileMetaInfoOnly) == DicomReadStatus.Success && fs.Position < fs.Length)
            {
                return fs;
            }
            fs.Close();
            return null;
        }

		/// <summary>
		/// Saves a DICOM file
		/// </summary>
		/// <param name="file">Filename</param>
		/// <param name="options">DICOM write options</param>
        /// <summary>
        /// Saves a DICOM file to a file
        /// </summary>
        /// <param name="file">Filename</param>
        /// <param name="options">DICOM write options</param>
        public void Save(String file, DicomWriteOptions options)
        {
            // expand to full path
            file = Path.GetFullPath(file);

            string dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            using (FileStream fs = File.Create(file))
            {
                Save(fs, options);
                fs.Close();
            }
        }

        /// <summary>
        /// Saves a DICOM file to a stream. <br/><i>Note that the caller is expected to dispose of the stream.</i>
        /// </summary>
        /// <param name="stream">Stream to place the DICOM file in</param>
        /// <param name="options">DICOM write options</param>
        public void Save(Stream stream, DicomWriteOptions options)
        {
            stream.Seek(128, SeekOrigin.Begin);
            stream.WriteByte((byte)'D');
            stream.WriteByte((byte)'I');
            stream.WriteByte((byte)'C');
            stream.WriteByte((byte)'M');

            DicomStreamWriter dsw = new DicomStreamWriter(stream);
            dsw.Write(_metainfo, options | DicomWriteOptions.CalculateGroupLengths);
            if (_dataset != null)
                dsw.Write(_dataset, options);
        }
	}
}
