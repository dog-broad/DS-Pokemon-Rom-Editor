﻿using DSPRE.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DSPRE.ROMFiles {
    public class CommandContainer {
        public List<ScriptCommand> commands;
        public int manualUserID;
        public int useScript;

        #region Constructors (2)
        public CommandContainer(int scriptNumber, int useScript = -1, List<ScriptCommand> commandList = null) {
            manualUserID = scriptNumber;
            this.useScript = useScript;
            commands = commandList;
        }
        #endregion
    }
    public class ScriptCommand {
        #region Fields (4)
        public ushort id;
        public List<byte[]> commandParameters;
        public string name;
        #endregion

        #region Constructors (2)
        public ScriptCommand(ushort id, List<byte[]> commandParameters) {
            this.id = id;
            this.commandParameters = commandParameters;

            Dictionary<ushort, string> commandNamesDatabase;
            commandNamesDatabase = RomInfo.scriptCommandNamesDict;

            try {
                name = commandNamesDatabase[id];
            } catch (KeyNotFoundException) {
                name = id.ToString("X4");
            }

            switch (id) {
                case 0x16:      // Jump
                case 0x1A:      // Call
                    this.name += " " + "Function_#" + (1 + BitConverter.ToInt32(commandParameters[0], 0)).ToString("D");
                    break;
                case 0x17:      // JumpIfObjID
                case 0x18:      // JumpIfBgID
                case 0x19:      // JumpIfPlayerDir
                    this.name += " " + (BitConverter.ToInt32(commandParameters[0], 0)).ToString("D") + " " + "Function_#" + (1 + (BitConverter.ToInt32(commandParameters[1], 0))).ToString("D");
                    break;
                case 0x1C:      // CompareLastResultJump
                case 0x1D:      // CompareLastResultCall
                    byte opcode = commandParameters[0][0];
                    this.name += " " + PokeDatabase.ScriptEditor.comparisonOperators[opcode] + " " + "Function_#" + (1 + (BitConverter.ToInt32(commandParameters[1], 0))).ToString("D");
                    break;
                case 0x5E:      // ApplyMovement
                    ushort flexID = BitConverter.ToUInt16(commandParameters[0], 0);
                    this.name += ScriptFile.OverworldFlexDecode(flexID);
                    this.name += " " + "Action_#" + (1 + (BitConverter.ToInt32(commandParameters[1], 0))).ToString("D");
                    break;
                case 0x62:      // Lock
                case 0x63:      // Release
                case 0x64:      // AddPeople
                case 0x65:      // RemoveOW
                    flexID = BitConverter.ToUInt16(commandParameters[0], 0);
                    name += ScriptFile.OverworldFlexDecode(flexID);
                    break;
                default:
                    for (int i = 0; i < commandParameters.Count; i++) {
                        if (commandParameters[i].Length == 1)
                            this.name += " " + "0x" + (commandParameters[i][0]).ToString("X1");
                        else if (commandParameters[i].Length == 2)
                            this.name += " " + "0x" + (BitConverter.ToInt16(commandParameters[i], 0)).ToString("X1");
                        else if (commandParameters[i].Length == 4)
                            this.name += " " + "0x" + (BitConverter.ToInt32(commandParameters[i], 0)).ToString("X1");
                    }
                    break;
                
            }
        }
        public ScriptCommand(string wholeLine, int lineNumber) {
            name = wholeLine;
            commandParameters = new List<byte[]>();

            string[] nameParts = wholeLine.Split(' '); // Separate command code from parameters
            /* Get command id, which is always first in the description */

            try {
                id = RomInfo.scriptCommandNamesDict.First(x => x.Value == nameParts[0]).Key;
            } catch (InvalidOperationException) {
                try {
                    id = UInt16.Parse(nameParts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                } catch (FormatException) {
                    MessageBox.Show("This Script file could not be saved." +
                        Environment.NewLine + "Parser failed to interpret line " + lineNumber +  ": \"" + wholeLine + "\"." +
                        Environment.NewLine + "\nAre you sure it's a proper Script Command?", "Parser error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    id = UInt16.MaxValue;
                    return;
                }
            }

            /* Read parameters from remainder of the description */
            Console.WriteLine("ID = " + id.ToString("X4"));

            byte[] parametersArr = RomInfo.commandParametersDict[id];
            
            int paramLength = 0;
            if (parametersArr.Length == 1 && parametersArr.First() == 0) {
                paramLength = 0;
            } else {
                paramLength = parametersArr.Length;
            }

            if (nameParts.Length - 1 == paramLength) {
                for (int i = 0; i < paramLength; i++) {
                    Console.WriteLine("Parameter #" + i.ToString() + ": " + nameParts[i + 1]);
                    try {
                        ushort comparisonOperator = PokeDatabase.ScriptEditor.comparisonOperators.First(x => x.Value == nameParts[i + 1]).Key;
                        commandParameters.Add(new byte[] { (byte)comparisonOperator });
                    } catch { //Not a comparison
                        int indexOfSpecialCharacter = nameParts[i + 1].IndexOfAny(new char[] { 'x', '#' });

                        /* If number is preceded by 0x parse it as hex, otherwise as decimal */
                        NumberStyles style;
                        if (nameParts[i + 1].StartsWith("0x"))
                            style = NumberStyles.HexNumber;
                        else
                            style = NumberStyles.Integer;

                        /* Convert strings of parameters to the correct datatypes */
                        switch (parametersArr[i]) {
                            case 1:
                                commandParameters.Add(new byte[] { Byte.Parse(nameParts[i + 1].Substring(indexOfSpecialCharacter + 1), style) });
                                break;
                            case 2:
                                switch (nameParts[i + 1]) {
                                    case "Player":
                                        commandParameters.Add(BitConverter.GetBytes((ushort)255));
                                        break;
                                    case "Following":
                                        commandParameters.Add(BitConverter.GetBytes((ushort)253));
                                        break;
                                    case "Cam":
                                        commandParameters.Add(BitConverter.GetBytes((ushort)241));
                                        break;
                                    default:
                                        commandParameters.Add(BitConverter.GetBytes(Int16.Parse(nameParts[i + 1].Substring(indexOfSpecialCharacter + 1), style)));
                                        break;
                                }
                                break;
                            case 4:
                                commandParameters.Add(BitConverter.GetBytes(Int32.Parse(nameParts[i + 1].Substring(indexOfSpecialCharacter + 1), style)));
                                break;
                        }
                    }
                }
            } else {
                MessageBox.Show("Wrong number of parameters for command " + nameParts[0] + " at line " + lineNumber + "." + Environment.NewLine + 
                    "Received: " + (nameParts.Length - 1) + Environment.NewLine + "Expected: " + paramLength
                    + Environment.NewLine + "\nThis Script File can not be saved.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                id = ushort.MaxValue; //ERROR VALUE
            }
        }
        #endregion

        #region Utilities
        public override string ToString() {
            return name + " (" + id.ToString("X") + ")";
        }
        #endregion
    }
}