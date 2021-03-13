using AsmJitter.Misc;
using AsmJitter.Model.Instruction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AsmJitter.Model
{
    public class Code
    {

        public List<AbstractInstruction> Instructions { get; } = new List<AbstractInstruction>();

        private List<Label> _labels = new List<Label>();
        public IEnumerable<Label> Labels 
        { 
            get
            {
                return _labels;
            }
        }

        public List<byte> GetBytes()
        {
            // Get the bytes for no jumping instructions
            foreach (var instruction in Instructions)
            {
                instruction.InstructionBytes = instruction.GetBytes().ToList();
            }
            GetBytesForJumpingInstrcutions();
            return Instructions.SelectMany(x => x.InstructionBytes).ToList();
        }

        public List<byte> GetBytes(int injectAddress)
        {
            GetBytes();
            for (int i = 0; i < Instructions.Count; i++)
            {
                var instruction = Instructions[i];
                if (instruction is AbstractJumpInstruction)
                {
                    var jumpInstruction = instruction as AbstractJumpInstruction;
                    if (jumpInstruction.Target.TargetAddress != null)
                    {
                        HandleAddressJump(jumpInstruction, i, injectAddress);
                    }
                }
            }
            return Instructions.SelectMany(x => x.InstructionBytes).ToList();
        }

        /// <summary>
        /// Get bytes from jumping instructions
        /// </summary>
        private void GetBytesForJumpingInstrcutions()
        {
            for (int i = 0; i < Instructions.Count; i++)
            {
                var instruction = Instructions[i];
                if (instruction is AbstractJumpInstruction)
                {
                    var jumpInstruction = instruction as AbstractJumpInstruction;
                    if (jumpInstruction.Target.Label != null)
                    {
                        HandleLabelJump(jumpInstruction, i);
                    }
                }
            }
        }

        private void HandleLabelJump(AbstractJumpInstruction jumpInstruction, int currentInstructionIndex)
        {
            var label = jumpInstruction.Target.Label;
            var labelIndex = Instructions.IndexOf(label);
            if (labelIndex > -1)
            {
                int relativeByteCount = 0;
                // if the label is above or at the same address than the jump instruction
                if (labelIndex <= currentInstructionIndex)
                {
                    var instructionsBetween = Instructions.Skip(labelIndex + 1).Take(currentInstructionIndex - labelIndex);
                    relativeByteCount = -instructionsBetween.Sum(x => x.InstructionBytes.Count);
                }
                else
                {
                    var instructionsBetween = Instructions.Skip(currentInstructionIndex + 1).Take(labelIndex - currentInstructionIndex);
                    relativeByteCount = instructionsBetween.Sum(x => x.InstructionBytes.Count);
                }

                jumpInstruction.InstructionBytes = new List<byte>()
                {
                    jumpInstruction.GetOperationByte(),
                    relativeByteCount.ConvertToSingleByte()
                }.ToList();
            }
            else
            {
                throw new ArgumentException($"The jump target label {label.Name} does not exist.");
            }
        }

        private void HandleAddressJump(AbstractJumpInstruction jumpInstruction, int currentInstructionIndex, int injectionAddress)
        {
            // Get the target address of this jump instruction.
            var targetAddress = jumpInstruction.Target.TargetAddress.Value;
            // Calculate the current address of this jump instruction by adding all instruction bytes of the upper commands
            int currentInstructionAddress = injectionAddress + Instructions.Take(currentInstructionIndex).Sum(x => x.InstructionBytes.Count);

            // Calculate the relative address jump byte code by substracting the current address from the target address and 5bytes for the jump instrcution itself
            int relativeAdressValue = Convert.ToInt32(targetAddress - currentInstructionAddress - jumpInstruction.InstructionBytes.Count);
            jumpInstruction.InstructionBytes = new List<byte>()
            {
                jumpInstruction.GetOperationByte(),
            }.Concat(relativeAdressValue.ConvertToByteArray()).ToList();
        }

        /// <summary>
        /// Add a label to the given code scope.
        /// </summary>
        /// <param name="label"></param>
        public void AddLabel(Label label)
        {
            if (!label.Name.All(Char.IsLetterOrDigit) || _labels.Any(x => x.Name.ToLower().Equals(label.Name.ToLower())))
            {
                throw new ArgumentException($"The given label name: {label.Name} has already been used or is not valid.");
            }
        }

    }
}
