﻿using System;
using System.Collections.Generic;
using System.Linq;
using Diz.Core.export;
using Diz.Core.model;
using Diz.Core.util;
using Diz.Test.Utils;
using IX.Observable;
using Xunit;

namespace Diz.Test
{
    public sealed class LogCreatorTests
    {
        private const string ExpectedRaw =
            //          label:       instructions                         ;PC    |rawbytes|ia
            "                        lorom                                ;      |        |      ;  \r\n" +
            "                                                             ;      |        |      ;  \r\n" +
            "                                                             ;      |        |      ;  \r\n" +
            "                        ORG $808000                          ;      |        |      ;  \r\n" +
            "                                                             ;      |        |      ;  \r\n" +
            "           CODE_808000: LDA.W Test_Data,X                    ;808000|BD5B80  |80805B;  \r\n" +
            "                        STA.W $0100,X                        ;808003|9D0001  |800100;  \r\n" +
            "           Test22:      DEX                                  ;808006|CA      |      ;  \r\n" +
            "                        BPL CODE_808000                      ;808007|10F7    |808000;  \r\n" +
            "                                                             ;      |        |      ;  \r\n" +
            "                        Test_Data = $80805B                  ;      |        |      ;  \r\n";

        readonly Data InputRom = new Data
            {
                Labels = new ObservableDictionary<int, Label>
                {
                    {0x808000 + 0x06, new Label {Name = "Test22"}},
                    {0x808000 + 0x5B, new Label {Name = "Test_Data", Comment = "Pretty cool huh?"}},
                    // the CODE_XXXXXX labels are autogenerated
                },
                RomMapMode = RomMapMode.LoRom,
                RomSpeed = RomSpeed.FastRom,
                RomBytes =
                {
                    // --------------------------
                    // highlighting a particular section here
                    // we will use this for unit tests as well.

                    // CODE_808000: LDA.W Test_Data,X
                    new RomByte {Rom = 0xBD, TypeFlag = Data.FlagType.Opcode, MFlag = true, Point = Data.InOutPoint.InPoint, DataBank = 0x80, DirectPage = 0x2100},
                    new RomByte {Rom = 0x5B, TypeFlag = Data.FlagType.Operand, DataBank = 0x80, DirectPage = 0x2100}, // Test_Data
                    new RomByte {Rom = 0x80, TypeFlag = Data.FlagType.Operand, DataBank = 0x80, DirectPage = 0x2100}, // Test_Data
                
                    // STA.W $0100,X
                    new RomByte {Rom = 0x9D, TypeFlag = Data.FlagType.Opcode, MFlag = true, DataBank = 0x80, DirectPage = 0x2100},
                    new RomByte {Rom = 0x00, TypeFlag = Data.FlagType.Operand, DataBank = 0x80, DirectPage = 0x2100},
                    new RomByte {Rom = 0x01, TypeFlag = Data.FlagType.Operand, DataBank = 0x80, DirectPage = 0x2100},
                
                    // DEX
                    new RomByte {Rom = 0xCA, TypeFlag = Data.FlagType.Opcode, MFlag = true, DataBank = 0x80, DirectPage = 0x2100},

                    // BPL CODE_808000
                    new RomByte {Rom = 0x10, TypeFlag = Data.FlagType.Opcode, MFlag = true, Point = Data.InOutPoint.OutPoint, DataBank = 0x80, DirectPage = 0x2100},
                    new RomByte {Rom = 0xF7, TypeFlag = Data.FlagType.Operand, DataBank = 0x80, DirectPage = 0x2100},
                
                    // ------------------------------------
                }
            };
        
        [Fact]
        public void TestAFewLines()
        {
            LogWriterHelper.AssertAssemblyOutputEquals(ExpectedRaw, LogWriterHelper.ExportAssembly(InputRom));
        }
    }
}