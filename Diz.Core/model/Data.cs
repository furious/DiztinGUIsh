﻿using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using Diz.Core.arch;
using Diz.Core.util;
using DiztinGUIsh;
using IX.Observable;

namespace Diz.Core.model
{
    public partial class Data : DizDataModel
    {
        // TODO: this really shouldn't be in Data, move to an outside 'SNESSystem' class or something that operates on Data
        private readonly Cpu65C816 cpu65C816;

        public Data()
        {
            comments = new ObservableDictionary<int, string>();
            labels = new ObservableDictionary<int, Label>();
            romBytes = new RomBytes();
            cpu65C816 = new Cpu65C816(this);
        }

        public void CreateRomBytesFromRom(IEnumerable<byte> actualRomBytes)
        {
            Debug.Assert(RomBytes.Count == 0);

            var previousNotificationState = RomBytes.SendNotificationChangedEvents;
            RomBytes.SendNotificationChangedEvents = false;

            RomBytes.Clear();
            foreach (var fileByte in actualRomBytes)
            {
                RomBytes.Add(new RomByte
                {
                    Rom = fileByte,
                });
            }

            RomBytes.SendNotificationChangedEvents = previousNotificationState;
        }

        private byte[] GetRomBytes(int pcOffset, int count)
        {
            var output = new byte[count];
            for (var i = 0; i < output.Length; i++)
                output[i] = (byte)GetRomByte(ConvertSnesToPc(pcOffset + i));

            return output;
        }

        public string GetRomNameFromRomBytes()
        {
            return Encoding.UTF8.GetString(GetRomBytes(0xFFC0, 21));
        }

        public int GetRomCheckSumsFromRomBytes()
        {
            return ByteUtil.ByteArrayToInt32(GetRomBytes(0xFFDC, 4));
        }

        public void CopyRomDataIn(IEnumerable<byte> trueRomBytes)
        {
            var previousNotificationState = RomBytes.SendNotificationChangedEvents;
            RomBytes.SendNotificationChangedEvents = false;

            var i = 0;
            foreach (var b in trueRomBytes)
            {
                RomBytes[i].Rom = b;
                ++i;
            }
            Debug.Assert(RomBytes.Count == i);

            RomBytes.SendNotificationChangedEvents = previousNotificationState;
        }

        public int GetRomSize() => RomBytes?.Count ?? 0;
        public FlagType GetFlag(int i) => RomBytes[i].TypeFlag;
        public void SetFlag(int i, FlagType flag) => RomBytes[i].TypeFlag = flag;
        public Architecture GetArchitecture(int i) => RomBytes[i].Arch;
        public void SetArchitecture(int i, Architecture arch) => RomBytes[i].Arch = arch;
        public InOutPoint GetInOutPoint(int i) => RomBytes[i].Point;
        public void SetInOutPoint(int i, InOutPoint point) => RomBytes[i].Point |= point;
        public void ClearInOutPoint(int i) => RomBytes[i].Point = 0;
        public int GetDataBank(int i) => RomBytes[i].DataBank;
        public void SetDataBank(int i, int dBank) => RomBytes[i].DataBank = (byte)dBank;
        public int GetDirectPage(int i) => RomBytes[i].DirectPage;
        public void SetDirectPage(int i, int dPage) => RomBytes[i].DirectPage = 0xFFFF & dPage;
        public int GetBaseAddr(int i) => RomBytes[i].BaseAddr;
        public void SetBaseAddr(int i, int bAddr) => RomBytes[i].BaseAddr = 0xFFFFFF & bAddr;
        public int GetIndirectAddr(int i) => RomBytes[i].IndirectAddr;
        public void SetIndirectAddr(int i, int iAddr) => RomBytes[i].IndirectAddr = 0xFFFFFF & iAddr;
        public bool GetXFlag(int i) => RomBytes[i].XFlag;
        public void SetConstantType(int i, ConstantType x) => RomBytes[i].TypeConstant = x;
        public ConstantType GetConstantType(int i) => RomBytes[i].TypeConstant;
        public void SetXFlag(int i, bool x) => RomBytes[i].XFlag = x;
        public bool GetMFlag(int i) => RomBytes[i].MFlag;
        public void SetMFlag(int i, bool m) => RomBytes[i].MFlag = m;
        public void ToggleMFlag(int i) => RomBytes[i].MFlag = !RomBytes[i].MFlag;
        public void ToggleXFlag(int i) => RomBytes[i].XFlag = !RomBytes[i].XFlag;
        public int GetMxFlags(int i)
        {
            return (RomBytes[i].MFlag ? 0x20 : 0) | (RomBytes[i].XFlag ? 0x10 : 0);
        }
        public void SetMxFlags(int i, int mx)
        {
            RomBytes[i].MFlag = ((mx & 0x20) != 0);
            RomBytes[i].XFlag = ((mx & 0x10) != 0);
        }
        public string GetLabelName(int i, bool def = false)
        {
            if (Labels.TryGetValue(i, out var val))
                return val?.Name ?? "";
            if (cpu65C816.GetRegisterLabel(i & 0xFFFF) != "")
                return cpu65C816.GetRegisterLabel(i & 0xFFFF);

            return def ? GetDefaultLabel(i) : "";
        }
        public string GetParentLabel(int offset, int snes)
        {
            string label = GetLabelName(snes), prefix = "";
            int address = ConvertSnesToPc(snes);
            if (label != "" && label[0] == '.' && address >= 0 && address < GetRomSize() )
            {
                /*
                 
                900: func
                930: bra .sub
                950: .sub
                990: bra func2.sub2
                ..
              - 1000: func2
                1001: bra func.sub
              a 1010: .sub1
                1020: beq .sub2
                1030: .sub2
              o 1040: bra .sub1
                 
                 */
                for (int x = address > offset ? address : offset; x > 0; x--)
                {
                    // if addr was after offset and we reach our offset, quit
                    if ((address > offset && x == offset) || (address < offset && x == address)) break;

                    if ((GetInOutPoint(x) & InOutPoint.InPoint) != InOutPoint.InPoint) continue;
                    // found a point

                    // has defined label?
                    prefix = GetLabelName(ConvertPCtoSnes(x), true);
                    if (prefix != "" && prefix[0] != '.' && prefix[0] != '-' && prefix[0] != '+')
                    {
                        if ((address > offset && x < address) || (address < offset && x < address))
                            return prefix + label.Replace('.', '_');
                        if (address < offset) x = address;
                    }

                }
            }

            return GetLabelName(snes);
        }
        public string GetLabelComment(int i)
        {
            if (Labels.TryGetValue(i, out var val))
                return val?.Comment ?? "";

            return cpu65C816.GetRegisterComment(i & 0xFFFF);
        }

        public void DeleteAllLabels()
        {
            Labels.Clear();
        }

        public void AddLabel(int offset, Label label, bool overwrite)
        {
            // adding null label removes it
            if (label == null || (label.Name == "" && label.Comment == ""))
            {
                if (Labels.ContainsKey(offset))
                    Labels.Remove(offset);

                return;
            }

            if (overwrite)
            {
                if (Labels.ContainsKey(offset))
                    Labels.Remove(offset);
            }

            if (!Labels.ContainsKey(offset))
            {
                label.CleanUp();
                Labels.Add(offset, label);
            }
        }

        public string GetComment(int i)
        {
            if (Comments.TryGetValue(i, out var val))
                return val;

            return GetLabelComment(i) ?? "";
        }

        public void AddComment(int i, string v, bool overwrite)
        {
            if (v == null)
            {
                if (Comments.ContainsKey(i)) Comments.Remove(i);
            } else
            {
                if (Comments.ContainsKey(i) && overwrite) Comments.Remove(i);
                if (!Comments.ContainsKey(i)) Comments.Add(i, v);
            }
        }

        public int ConvertPCtoSnes(int offset)
        {
            return RomUtil.ConvertPCtoSnes(offset, RomMapMode, RomSpeed);
        }
        public int GetRomByte(int i) => RomBytes[i].Rom;
        public int GetRomWord(int offset)
        {
            if (offset + 1 < GetRomSize())
                return GetRomByte(offset) + (GetRomByte(offset + 1) << 8);
            return -1;
        }
        public int GetRomLong(int offset)
        {
            if (offset + 2 < GetRomSize())
                return GetRomByte(offset) + (GetRomByte(offset + 1) << 8) + (GetRomByte(offset + 2) << 16);
            return -1;
        }
        public int GetRomDoubleWord(int offset)
        {
            if (offset + 3 < GetRomSize())
                return GetRomByte(offset) + (GetRomByte(offset + 1) << 8) + (GetRomByte(offset + 2) << 16) + (GetRomByte(offset + 3) << 24);
            return -1;
        }
        public int GetIntermediateAddressOrPointer(int offset, bool original = false)
        {
            switch (GetFlag(offset))
            {
                case FlagType.Unreached:
                case FlagType.Opcode:
                case FlagType.Operand:
                    if (GetFlag(offset) == FlagType.Operand && GetIndirectAddr(offset) <= 0) break;
                    return GetIntermediateAddress(offset, true, original);
                case FlagType.Pointer16Bit:
                    int bank = GetDataBank(offset);
                    return (bank << 16) | GetRomWord(offset);
                case FlagType.Pointer24Bit:
                case FlagType.Pointer32Bit:
                    return GetRomLong(offset);
            }
            return -1;
        }

        public int GetBankSize()
        {
            return RomUtil.GetBankSize(RomMapMode);
        }

        public int OpcodeByteLength(int offset)
        {
            return GetArchitecture(offset) switch
            {
                Architecture.Cpu65C816 => cpu65C816.GetInstructionLength(offset),
                Architecture.Apuspc700 => 1,
                Architecture.GpuSuperFx => 1,
                _ => 1
            };
        }

        private int UnmirroredOffset(int offset)
        {
            return RomUtil.UnmirroredOffset(offset, GetRomSize());
        }

        public string GetFormattedBytes(int offset, int step, int bytes = 0)
        {
            if (bytes == 0) bytes = GetSectionSize(offset);
            var res = step switch
            {
                1 => "db ",
                2 => "dw ",
                3 => "dl ",
                4 => "dd ",
                _ => ""
            };

            for (var i = 0; i < bytes; i += step)
            {
                if (i > 0) res += ",";

                Util.NumberBase type = GetConstantType(offset) switch
                {
                    Data.ConstantType.Decimal => Util.NumberBase.Decimal,
                    Data.ConstantType.Binary => Util.NumberBase.Binary,
                    Data.ConstantType.Color => Util.NumberBase.Color,
                    _ => Util.NumberBase.Hexadecimal
                };

                switch (step)
                {
                    case 1: res += Util.NumberToBaseString(GetRomByte(offset + i), type, (int)type * 1, true); break;
                    case 2: res += Util.NumberToBaseString(GetRomWord(offset + i), type, (int)type * 2, true); break;
                    case 3: res += Util.NumberToBaseString(GetRomLong(offset + i), type, (int)type * 3, true); break;
                    case 4: res += Util.NumberToBaseString(GetRomDoubleWord(offset + i), type, (int)type * 4, true); break;
                }
            }

            return res;
        }

        public int ConvertSnesToPc(int address)
        {
            return RomUtil.ConvertSnesToPc(address, RomMapMode, GetRomSize());
        }

        public string GetPointer(int offset, int bytes)
        {
            var ia = -1;
            string format = "", param = "";
            switch (bytes)
            {
                case 2:
                    ia = (GetDataBank(offset) << 16) | GetRomWord(offset);
                    format = "dw {0}";
                    param = Util.NumberToBaseString(GetRomWord(offset), Util.NumberBase.Hexadecimal, 4, true);
                    break;
                case 3:
                    ia = GetRomLong(offset);
                    format = "dl {0}";
                    param = Util.NumberToBaseString(GetRomLong(offset), Util.NumberBase.Hexadecimal, 6, true);
                    break;
                case 4:
                    ia = GetRomLong(offset);
                    format = "dl {0}" +
                             $" : db {Util.NumberToBaseString(GetRomByte(offset + 3), Util.NumberBase.Hexadecimal, 2, true)}";
                    param = Util.NumberToBaseString(GetRomLong(offset), Util.NumberBase.Hexadecimal, 6, true);
                    break;
            }

            var pc = ConvertSnesToPc(ia);
            if (pc >= 0 && GetLabelName(ia) != "") param = GetParentLabel(offset, ia);
            return string.Format(format, param);
        }

        public int GetSectionSize(int offset)
        {
            int bytes = 1;
            while (offset + bytes < GetRomSize() && GetFlag(offset + bytes) == GetFlag(offset) && GetLabelName(ConvertPCtoSnes(offset + bytes)) == "")
                bytes++;
            return bytes;
        }
        public int CalculateBaseAddr(int offset)
        {
            int pos = offset, baddr = GetBaseAddr(offset), bank = offset / GetBankSize();
            if (baddr <= 0) return -1;

            while (--pos >= 0 && baddr == GetBaseAddr(pos) && bank == (pos / GetBankSize()))// && GetLabelName(ConvertPCtoSnes(offset - pos)) == "")
                continue;
            return offset - pos + baddr - 1;
        }

        public string GetFormattedText(int offset, int bytes = 0)
        {
            List<string> texts = new List<string>();
            if (bytes == 0) bytes = GetSectionSize(offset);

            int byt = 0; var text = "";
            for (var i = 0; i < bytes; i++)
            {
                byt = GetRomByte(offset + i);
                if (byt < 0x20 || byt > 0x80)
                {
                    if(text != "") texts.Add($"\"{ text }\""); text = "";
                    texts.Add(Util.NumberToBaseString(byt, Util.NumberBase.Hexadecimal, 2, true));                    
                } else {
                   text += (char) byt;
                }

            }
            if (text != "") texts.Add($"\"{ text }\"");
            return "db " + String.Join(", ", texts.ToArray());
        }

        public string GetDefaultLabel(int snes)
        {
            var pcoffset = ConvertSnesToPc(snes);
            var prefix = pcoffset >= 0 && pcoffset < GetRomSize() ? RomUtil.TypeToLabel(GetFlag(pcoffset)) : "RAM";
            var labelAddress = Util.NumberToBaseString(snes, Util.NumberBase.Hexadecimal, 6);

            return $"{prefix}_{labelAddress}";
        }

        public int Step(int offset, bool branch, bool force, int prevOffset)
        {
            return GetArchitecture(offset) switch
            {
                Architecture.Cpu65C816 => cpu65C816.Step(offset, branch, force, prevOffset),
                Architecture.Apuspc700 => offset,
                Architecture.GpuSuperFx => offset,
                _ => offset
            };
        }

        public int AutoStep(int offset, bool harsh, int amount)
        {
            int newOffset = offset, prevOffset = offset - 1, nextOffset;
            if (harsh)
            {
                while (newOffset < offset + amount)
                {
                    nextOffset = Step(newOffset, false, true, prevOffset);
                    prevOffset = newOffset;
                    newOffset = nextOffset;
                }
            }
            else
            {
                var stack = new Stack<int>();
                var seenBranches = new List<int>();
                var keepGoing = true;

                while (keepGoing)
                {
                    switch (GetArchitecture(newOffset))
                    {
                        case Architecture.Cpu65C816:
                            if (seenBranches.Contains(newOffset))
                            {
                                keepGoing = false;
                                break;
                            }

                            var opcode = GetRomByte(newOffset);

                            nextOffset = Step(newOffset, false, false, prevOffset);
                            var jumpOffset = Step(newOffset, true, false, prevOffset);

                            if (opcode == 0x40 || opcode == 0xCB || opcode == 0xDB || opcode == 0xF8 // RTI WAI STP SED
                                || opcode == 0xFB || opcode == 0x00 || opcode == 0x02 || opcode == 0x42 // XCE BRK COP WDM
                                || opcode == 0x6C || opcode == 0x7C || opcode == 0xDC || opcode == 0xFC // JMP JMP JML JSR
                            ) keepGoing = false;

                            if (opcode == 0x4C || opcode == 0x5C || opcode == 0x80 || opcode == 0x82 // JMP JML BRA BRL
                                || opcode == 0x10 || opcode == 0x30 || opcode == 0x50 || opcode == 0x70 // BPL BMI BVC BVS
                                || opcode == 0x90 || opcode == 0xB0 || opcode == 0xD0 || opcode == 0xF0 // BCC BCS BNE BEQ
                            ) seenBranches.Add(newOffset);

                            if (opcode == 0x08) // PHP
                            {
                                stack.Push(GetMxFlags(newOffset));
                            }
                            else if (opcode == 0x28) // PLP
                            {
                                if (stack.Count == 0)
                                {
                                    keepGoing = false; break;
                                }
                                else
                                {
                                    SetMxFlags(newOffset, stack.Pop());
                                }
                            }

                            if (opcode == 0x60 || opcode == 0x6B) // RTS RTL
                            {
                                if (stack.Count == 0)
                                {
                                    keepGoing = false;
                                    break;
                                }
                                else
                                {
                                    prevOffset = newOffset;
                                    newOffset = stack.Pop();
                                }
                            }
                            else if (opcode == 0x20 || opcode == 0x22) // JSR JSL
                            {
                                stack.Push(nextOffset);
                                prevOffset = newOffset;
                                newOffset = jumpOffset;
                            }
                            else
                            {
                                prevOffset = newOffset;
                                newOffset = nextOffset;
                            }
                            break;
                        case Architecture.Apuspc700:
                        case Architecture.GpuSuperFx:
                            nextOffset = Step(newOffset, false, true, prevOffset);
                            prevOffset = newOffset;
                            newOffset = nextOffset;
                            break;
                    }

                    var flag = GetFlag(newOffset);
                    if (!(flag == FlagType.Unreached || flag == FlagType.Opcode || flag == FlagType.Operand)) keepGoing = false;
                }
            }
            return newOffset;
        }

        public int Mark(Action<int> MarkAction, int offset, int count, bool unreachedonly = false)
        {
            int i, size = GetRomSize();
            for (i = 0; i < count && offset + i < size; i++) 
                if(!unreachedonly || unreachedonly && GetFlag(offset + i) == FlagType.Unreached)
                    MarkAction(offset + i);
            RescanInOutPoints();
            return offset + i < size ? offset + i : size - 1;
        }

        public int MarkTypeFlag(int offset, FlagType type, int count, bool unreachedonly = false) => Mark(i => SetFlag(i, type), offset, count, unreachedonly);
        public int MarkDataBank(int offset, int db, int count, bool unreachedonly = false) => Mark(i => SetDataBank(i, db), offset, count, unreachedonly);
        public int MarkDirectPage(int offset, int dp, int count, bool unreachedonly = false) => Mark(i => SetDirectPage(i, dp), offset, count, unreachedonly);
        public int MarkBaseAddr(int offset, int addr, int count, bool unreachedonly = false) => Mark(i => SetBaseAddr(i, addr), offset, count, unreachedonly);
        public int MarkXFlag(int offset, bool x, int count, bool unreachedonly = false) => Mark(i => SetXFlag(i, x), offset, count, unreachedonly);
        public int MarkMFlag(int offset, bool m, int count, bool unreachedonly = false) => Mark(i => SetMFlag(i, m), offset, count, unreachedonly);
        public int MarkArchitecture(int offset, Architecture arch, int count, bool unreachedonly = false) => Mark(i => SetArchitecture(i, arch), offset, count, unreachedonly);

        public int GetInstructionLength(int offset)
        {
            return GetArchitecture(offset) switch
            {
                Architecture.Cpu65C816 => cpu65C816.GetInstructionLength(offset),
                Architecture.Apuspc700 => 1,
                Architecture.GpuSuperFx => 1,
                _ => 1
            };
        }

        public int FixMisalignedFlags()
        {
            int count = 0, size = GetRomSize();

            for (var i = 0; i < size; i++)
            {
                var flag = GetFlag(i);

                switch (flag)
                {
                    case FlagType.Opcode:
                    {
                        int len = GetInstructionLength(i);
                        for (var j = 1; j < len && i + j < size; j++)
                        {
                            if (GetFlag(i + j) != FlagType.Operand)
                            {
                                SetFlag(i + j, FlagType.Operand);
                                count++;
                            }
                        }
                        i += len - 1;
                        break;
                    }
                    case FlagType.Operand:
                        SetFlag(i, FlagType.Opcode);
                        count++;
                        i--;
                        break;
                    default:
                    {
                        if (RomUtil.GetByteLengthForFlag(flag) > 1)
                        {
                            int step = RomUtil.GetByteLengthForFlag(flag);
                            for (int j = 1; j < step; j++)
                            {
                                if (GetFlag(i + j) == flag) 
                                    continue;
                                SetFlag(i + j, flag);
                                count++;
                            }
                            i += step - 1;
                        }

                        break;
                    }
                }
            }

            return count;
        }

        public void RescanInOutPoints()
        {
            for (var i = 0; i < GetRomSize(); i++) ClearInOutPoint(i);
            int next = 1;
            for (var i = 0; i < GetRomSize(); i+=next)
            {
                FlagType flag = GetFlag(i);
                switch (flag)
                {
                    case FlagType.Opcode:
                    case FlagType.Pointer16Bit:
                        switch (GetArchitecture(i))
                        {
                            case Architecture.Cpu65C816: cpu65C816.MarkInOutPoints(i); break;
                            case Architecture.Apuspc700: break;
                            case Architecture.GpuSuperFx: break;
                        }
                        break;
                }

                next = RomUtil.GetByteLengthForFlag(flag);
                if (next > 1)
                {
                    SetInOutPoint(i, InOutPoint.ReadPoint);
                }

            }
        }

        public int GetIntermediateAddress(int offset, bool resolve = false, bool original = false)
        {
            // FIX ME: log and generation of dp opcodes. search references
            return GetArchitecture(offset) switch
            {
                Architecture.Cpu65C816 => cpu65C816.GetIntermediateAddress(offset, resolve, original),
                Architecture.Apuspc700 => -1,
                Architecture.GpuSuperFx => -1,
                _ => -1
            };
        }

        public string GetInstruction(int offset, bool lowercase)
        {
            return GetArchitecture(offset) switch
            {
                Architecture.Cpu65C816 => cpu65C816.GetInstruction(offset, lowercase),
                Architecture.Apuspc700 => "",
                Architecture.GpuSuperFx => "",
                _ => ""
            };
        }
        public Cpu65C816.AddressMode GetAddressMode(int offset)
        {
            return cpu65C816.GetAddressMode(offset);
        }

        public int GetNumberOfBanks()
        {
            return RomBytes.Count / GetBankSize();
        }

        public string GetBankName(int bankIndex)
        {
            var bankSnesByte = GetSnesBankByte(bankIndex);
            return Util.NumberToBaseString(bankSnesByte, Util.NumberBase.Hexadecimal, 2);
        }

        private int GetSnesBankByte(int bankIndex)
        {
            var bankStartingPcOffset = bankIndex << 16;
            var bankSnesNumber = ConvertPCtoSnes(bankStartingPcOffset) >> 16;
            return bankSnesNumber;
        }

        // get the actual ROM file bytes (i.e. the contents of the SMC file on the disk)
        // note: don't save these anywhere permanent because ROM data is usually copyrighted.
        public IEnumerable<byte> GetFileBytes()
        {
            return RomBytes.Select(b => b.Rom);
        }
    }
}