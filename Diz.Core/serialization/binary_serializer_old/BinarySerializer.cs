﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Diz.Core.model;
using Diz.Core.util;

namespace Diz.Core.serialization.binary_serializer_old
{
    internal class BinarySerializer : ProjectSerializer
    {
        public const int HeaderSize = 0x100;
        private const int LatestFileFormatVersion = 2;

        public static bool IsBinaryFileFormat(byte[] data)
        {
            for (var i = 0; i < Watermark.Length; i++) {
                if (data[i + 1] != (byte) Watermark[i])
                    return false;
            }
            return true;
        }

        public override byte[] Save(Project project)
        {
            const int versionToSave = LatestFileFormatVersion;
            var data = SaveVersion(project, versionToSave);

            var everything = new byte[HeaderSize + data.Length];
            everything[0] = versionToSave;
            ByteUtil.StringToByteArray(Watermark).CopyTo(everything, 1);
            data.CopyTo(everything, HeaderSize);

            return data;
        }

        public override (Project project, string warning) Load(byte[] data)
        {
            if (!IsBinaryFileFormat(data))
                throw new InvalidDataException($"This is not a binary serialized project file!");

            byte version = data[0];
            ValidateProjectFileVersion(version);

            var project = new Project
            {
                Data = new Data()
            };

            // version 0 needs to convert PC to SNES for some addresses
            ByteUtil.AddressConverter converter = address => address;
            if (version == 0)
                converter = project.Data.ConvertPCtoSnes;

            // read mode, speed, size
            project.Data.RomMapMode = (RomMapMode)data[HeaderSize];
            project.Data.RomSpeed = (RomSpeed)data[HeaderSize + 1];
            var size = ByteUtil.ByteArrayToInt32(data, HeaderSize + 2);

            // read internal title
            var pointer = HeaderSize + 6;
            project.InternalRomGameName = RomUtil.ReadStringFromByteArray(data, RomUtil.LengthOfTitleName, pointer);
            pointer += RomUtil.LengthOfTitleName;

            // read checksums
            project.InternalCheckSum = ByteUtil.ByteArrayToInt32(data, pointer);
            pointer += 4;

            // read full filepath to the ROM .sfc file
            while (data[pointer] != 0)
                project.AttachedRomFilename += (char)data[pointer++];
            pointer++;

            project.Data.RomBytes.Create(size);

            for (int i = 0; i < size; i++) project.Data.SetDataBank(i, data[pointer + i]); pointer++;
            for (int i = 0; i < size; i++) project.Data.SetDirectPage(i, data[pointer + size + i] | (data[pointer + 1 * size + i] << 8)); pointer += 2;
            for (int i = 0; i < size; i++) project.Data.SetXFlag(i, data[pointer * size + i] != 0); pointer++;
            for (int i = 0; i < size; i++) project.Data.SetMFlag(i, data[pointer * size + i] != 0); pointer++;
            for (int i = 0; i < size; i++) project.Data.SetFlag(i, (Data.FlagType)data[pointer * size + i]); pointer++;
            for (int i = 0; i < size; i++) project.Data.SetArchitecture(i, (Data.Architecture)data[pointer * size + i]); pointer++;
            for (int i = 0; i < size; i++) project.Data.SetInOutPoint(i, (Data.InOutPoint)data[pointer * size + i]); pointer++;
            //for (int i = 0; i < size; i++) project.Data.SetBaseAddr(i, (Data.InOutPoint)data[pointer * size + i]); pointer++;

            ReadLabels(project, data, ref pointer, converter, version >= 2);
            ReadComments(project, data, ref pointer, converter);

            project.UnsavedChanges = false;

            var warning = "";
            if (version != LatestFileFormatVersion)
            {
                warning = "This project file is in an older format.\n" +
                              "You may want to back up your work or 'Save As' in case the conversion goes wrong.\n" +
                              "The project file will be untouched until it is saved again.";
            }

            return (project, warning);
        }

        private static void SaveStringToBytes(string str, ICollection<byte> bytes)
        {
            // TODO: combine with Util.StringToByteArray() probably.
            if (str != null) {
                foreach (var c in str) {
                    bytes.Add((byte)c);
                }
            }
            bytes.Add(0);
        }

        private byte[] SaveVersion(Project project, int version)
        {
            ValidateSaveVersion(version);

            int size = project.Data.GetRomSize();
            byte[] romSettings = new byte[31];

            // save these two
            romSettings[0] = (byte)project.Data.RomMapMode;
            romSettings[1] = (byte)project.Data.RomSpeed;

            // save the size, 4 bytes
            ByteUtil.IntegerIntoByteArray(size, romSettings, 2);

            var romName = project.Data.GetRomNameFromRomBytes();
            romName.ToCharArray().CopyTo(romSettings, 6);

            var romChecksum = project.Data.GetRomCheckSumsFromRomBytes();
            BitConverter.GetBytes(romChecksum).CopyTo(romSettings, 27);

            // TODO put selected offset in save file

            // save all labels ad comments
            List<byte> label = new List<byte>(), comment = new List<byte>();
            var allLabels = project.Data.Labels;
            var allComments = project.Data.Comments;

            ByteUtil.IntegerIntoByteList(allLabels.Count, label);
            foreach (var pair in allLabels)
            {
                ByteUtil.IntegerIntoByteList(pair.Key, label);

                SaveStringToBytes(pair.Value.Name, label);
                if (version >= 2)
                {
                    SaveStringToBytes(pair.Value.Comment, label);
                }
            }

            ByteUtil.IntegerIntoByteList(allComments.Count, comment);
            foreach (KeyValuePair<int, string> pair in allComments)
            {
                ByteUtil.IntegerIntoByteList(pair.Key, comment);
                SaveStringToBytes(pair.Value, comment);
            }

            // save current Rom full path - "c:\whatever\someRom.sfc"
            var romLocation = ByteUtil.StringToByteArray(project.AttachedRomFilename);

            var data = new byte[romSettings.Length + romLocation.Length + 8 * size + label.Count + comment.Count];
            romSettings.CopyTo(data, 0);
            for (int i = 0; i < romLocation.Length; i++) data[romSettings.Length + i] = romLocation[i];

            var readOps = new Func<int, byte>[]
            {
                i => (byte)project.Data.GetDataBank(i),
                i => (byte)project.Data.GetDataBank(i),
                i => (byte)project.Data.GetDirectPage(i),
                i => (byte)(project.Data.GetDirectPage(i) >> 8),
                i => (byte)(project.Data.GetXFlag(i) ? 1 : 0),
                i => (byte)(project.Data.GetMFlag(i) ? 1 : 0),
                i => (byte)project.Data.GetFlag(i),
                i => (byte)project.Data.GetArchitecture(i),
                i => (byte)project.Data.GetInOutPoint(i),
                //i => (byte)project.Data.GetBaseAddr(i),
                //i => (byte)(project.Data.GetBaseAddr(i) >> 8),
                //i => (byte)(project.Data.GetBaseAddr(i) >> 16),
            };

            void ReadOperation(int startIdx, int whichOp)
            {
                if (whichOp <= 0 || whichOp > readOps.Length)
                    throw new ArgumentOutOfRangeException(nameof(whichOp));

                var baseidx = startIdx + whichOp * size;
                var op = readOps[whichOp];
                for (var i = 0; i < size; i++)
                {
                    data[baseidx + i] = (byte)op(i);
                }
            }

            for (var i = 0; i < readOps.Length; ++i)
            {
                var start = romSettings.Length + romLocation.Length;
                ReadOperation(start, i);
            }

            Console.WriteLine(readOps.Count());
            // ???
            label.CopyTo(data, romSettings.Length + romLocation.Length + 8 * size);
            comment.CopyTo(data, romSettings.Length + romLocation.Length + 8 * size + label.Count);
            // ???

            return data;
        }

        private static void ValidateSaveVersion(int version) {
            if (version < 1 || version > LatestFileFormatVersion) {
                throw new ArgumentException($"Saving: Invalid save version requested for saving: {version}.");
            }
        }

        private static void ValidateProjectFileVersion(int version)
        {
            if (version > LatestFileFormatVersion)
            {
                throw new ArgumentException(
                    "This DiztinGUIsh file uses a newer file format! You'll need to download the newest version of DiztinGUIsh to open it.");
            }

            if (version < 0)
            {
                throw new ArgumentException($"Invalid project file version detected: {version}.");
            }
        }

        private void ReadComments(Project project, byte[] bytes, ref int pointer, ByteUtil.AddressConverter converter)
        {
            const int stringsPerEntry = 1;
            pointer += ByteUtil.ReadStringsTable(bytes, pointer, stringsPerEntry, converter, 
                (int offset, string[] strings) =>
            {
                Debug.Assert(strings.Length == 1);
                project.Data.AddComment(offset, strings[0], true);
            });
        }

        private void ReadLabels(Project project, byte[] bytes, ref int pointer, ByteUtil.AddressConverter converter, bool readAliasComments)
        {
            var stringsPerEntry = readAliasComments ? 2 : 1;
            pointer += ByteUtil.ReadStringsTable(bytes, pointer, stringsPerEntry, converter,
                (int offset, string[] strings) =>
                {
                    Debug.Assert(strings.Length == stringsPerEntry);
                    var label = new Label
                    {
                        Name = strings[0],
                        Comment = strings.ElementAtOrDefault(1)
                    };
                    label.CleanUp();
                    project.Data.AddLabel(offset, label, true);
                });
        }
    }
}
