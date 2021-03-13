using AsmJitter;
using AsmJitter.Model;
using AsmJitter.Model.Instruction;
using AsmJitter.Model.Operand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace FunExecuter
{
    internal static class GameManager
    {        

        private static IntPtr _handle;

        internal static void Init()
        {
            _handle = MemoryManager.GetProcHandle();
            if (_handle == IntPtr.Zero)
            {
                throw new Exception("Could not find the game process, please start the game at first...");
            }
            LoadAlternativeHealthReductionCode();
            //LoadAlternativePositionCode();
            //LoadUnlimitedAmmo();
        }

        private static void LoadAlternativeHealthReductionCode()
        {
            var sentryOnlyCode = GetSentryOnlyCode();
            //var sentryOnlyCode = GetSentryAndBothPlayersCode();
            var codebytes = sentryOnlyCode.GetBytes();
            var injectAddress = MemoryManager.AllocSpace(_handle, (uint)codebytes.Count).ToInt32();
            MemoryManager.WriteByteArrayFromMemory(_handle, injectAddress, codebytes.ToArray());

            var callCode = new Code();
            callCode.Call(new FourBytesConst(injectAddress))
                .Nop()
                .Nop()
                .Nop();

            var callBytes = callCode.GetBytes(Constants.HEALTH_SUBSTRACTION_INSTRUCTION);
            MemoryManager.WriteByteArrayFromMemory(_handle, Constants.HEALTH_SUBSTRACTION_INSTRUCTION, callBytes.ToArray());
        }

        private static void LoadAlternativePositionCode()
        {
            var posCode = GetPositionCode();            
            var codebytes = posCode.GetBytes();
            var injectAddress = MemoryManager.AllocSpace(_handle, (uint)codebytes.Count).ToInt32();
            codebytes = posCode.GetBytes(injectAddress);
            MemoryManager.WriteByteArrayFromMemory(_handle, injectAddress, codebytes.ToArray());

            var callCode = new Code();
            callCode.Call(new FourBytesConst(injectAddress));

            var callBytes = callCode.GetBytes(Constants.POSITION_X_FLOATLOAD_INSTRUCTION);
            MemoryManager.WriteByteArrayFromMemory(_handle, Constants.POSITION_X_FLOATLOAD_INSTRUCTION, callBytes.ToArray());
        }

        private static void LoadUnlimitedAmmo()
        {
            var ammoCode = GetAmmoCode();
            var codebytes = ammoCode.GetBytes();
            var injectAddress = Constants.BULLET_REDUCTION_CODE;
            MemoryManager.WriteByteArrayFromMemory(_handle, injectAddress, codebytes.ToArray());
        }

        private static Code GetAmmoCode()
        {
            var shellcode = new Code();
            shellcode.Nop()
                .Nop()
                .Nop()
                .Nop();
            return shellcode;
        }

        private static Code GetSentryOnlyCode()
        {
            var shellcode = new Code();
            var returnLabel = new Label("return", shellcode);
            shellcode.Cmp(new RegisterMemory(RegisterEnum.ESI_XMM6, 0x150), new FourBytesConst(Constants.HEALTH_SENTRY_VALUE))
                .Je(returnLabel)
                .Sub(new Register(RegisterEnum.ECX_XMM1), new Register(RegisterEnum.EBP_DS32_XMM5))
                .Mov(new RegisterMemory(RegisterEnum.ESI_XMM6, 0x150), new Register(RegisterEnum.ECX_XMM1))
                .Label(returnLabel)
                .Ret();
            return shellcode;
        }

        private static Dictionary<string, List<PositionTeleport>> _coordinates = new Dictionary<string, List<PositionTeleport>>()
        {
            { "dome", new List<PositionTeleport>() // Dome
                {
                    new PositionTeleport(new Position(-650f, 600f, -290f), new Position(-600f, 650f, -250f), new Position(-1024f, 1193f, -150f)),
                    new PositionTeleport(new Position(-1203, 430f, -260f), new Position(-1150f, 500f, -240f), new Position(157f, 997f, -310f))
                }
            },
            { "mogadishu", new List<PositionTeleport>() // Bakaara
                {
                    new PositionTeleport(new Position(-940f, 1219f, -40f), new Position(-850f, 1350f, -25f), new Position(-1169f, 1283f, 224f)),
                    new PositionTeleport(new Position(-900f, 1800f, 210f), new Position(-800f, 1950f, 250f), new Position(305f, 1224f, -30f))
                }
            },
            { "village", new List<PositionTeleport>() // Village
                {
                    new PositionTeleport(new Position(481f, -1400f, 250f), new Position(570f, -1300f, 300f), new Position(-272f, -2210f, 910f)),
                    new PositionTeleport(new Position(-666f, -2700f, 900f), new Position(-550f, -2500f, 970f), new Position(950f, -1116f, 300f))
                }
            },
            { "terminal_cls", new List<PositionTeleport>() // Terminal
                {
                    new PositionTeleport(new Position(2850f, 4459f, 185f), new Position(2890f, 4620f, 200f), new Position(3753f, 3687f, 200f)),
                    new PositionTeleport(new Position(2950f, 5000f, 180f), new Position(3000f, 5100f, 200f), new Position(1089f, 4933f, 200f))
                }
            }
        };

        private static Code GetPositionCode()
        {            
            var shellcode = new Code();
            foreach (var posTeleport in _coordinates.Last().Value)
            {
                var subPosCode = GetPositionCode(posTeleport);
                var codebytes = subPosCode.GetBytes();
                var injectAddress = MemoryManager.AllocSpace(_handle, (uint)codebytes.Count).ToInt32();
                MemoryManager.WriteByteArrayFromMemory(_handle, injectAddress, codebytes.ToArray());
                shellcode.Call(new FourBytesConst(injectAddress));
            }
            shellcode
                .Mov(new Register(RegisterEnum.ECX_XMM1), new EightBitConstant(2))
                .Ret();
            return shellcode;
        }

        private static Code GetPositionCode(PositionTeleport posTeleport)
        {
            var xMinAddress = MemoryManager.AllocSpace(_handle, 4).ToInt32();
            MemoryManager.WriteByteArrayFromMemory(_handle, xMinAddress, new FloatConst(posTeleport.MinPos.X).GetBytes().ToArray());
            var xMaxAddress = MemoryManager.AllocSpace(_handle, 4).ToInt32();
            MemoryManager.WriteByteArrayFromMemory(_handle, xMaxAddress, new FloatConst(posTeleport.MaxPos.X).GetBytes().ToArray());
            var yMinAddress = MemoryManager.AllocSpace(_handle, 4).ToInt32();
            MemoryManager.WriteByteArrayFromMemory(_handle, yMinAddress, new FloatConst(posTeleport.MinPos.Y).GetBytes().ToArray());
            var yMaxAddress = MemoryManager.AllocSpace(_handle, 4).ToInt32();
            MemoryManager.WriteByteArrayFromMemory(_handle, yMaxAddress, new FloatConst(posTeleport.MaxPos.Y).GetBytes().ToArray());
            var zMinAddress = MemoryManager.AllocSpace(_handle, 4).ToInt32();
            MemoryManager.WriteByteArrayFromMemory(_handle, zMinAddress, new FloatConst(posTeleport.MinPos.Z).GetBytes().ToArray());
            var zMaxAddress = MemoryManager.AllocSpace(_handle, 4).ToInt32();
            MemoryManager.WriteByteArrayFromMemory(_handle, zMaxAddress, new FloatConst(posTeleport.MaxPos.Z).GetBytes().ToArray());

            var shellcode = new Code();
            var secondPlayerCode = new Label("secondPlayerCode", shellcode);
            var returnLabel = new Label("return", shellcode);
            shellcode.Cmp(new Register(RegisterEnum.EBP_DS32_XMM5), new FourBytesConst(Constants.HOSTPLAYER_ID))
                .Jne(secondPlayerCode)
                .Cmp(new Register(RegisterEnum.EBP_DS32_XMM5), new FourBytesConst(Constants.HOSTPLAYER_ID))
                .Movss(new Register(RegisterEnum.EAX_XMM0), new RegisterMemory(RegisterEnum.EBX_XMM3, 0x1C))
                .Movss(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(xMinAddress))
                .Comiss(new Register(RegisterEnum.EAX_XMM0), new Register(RegisterEnum.ECX_XMM1))
                .Jb(secondPlayerCode)
                .Movss(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(xMaxAddress))
                .Comiss(new Register(RegisterEnum.EAX_XMM0), new Register(RegisterEnum.ECX_XMM1))
                .Jnb(secondPlayerCode)
                .Movss(new Register(RegisterEnum.EAX_XMM0), new RegisterMemory(RegisterEnum.EBX_XMM3, 0x20))
                .Movss(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(yMinAddress))
                .Comiss(new Register(RegisterEnum.EAX_XMM0), new Register(RegisterEnum.ECX_XMM1))
                .Jb(secondPlayerCode)
                .Movss(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(yMaxAddress))
                .Comiss(new Register(RegisterEnum.EAX_XMM0), new Register(RegisterEnum.ECX_XMM1))
                .Jnb(secondPlayerCode)
                .Movss(new Register(RegisterEnum.EAX_XMM0), new RegisterMemory(RegisterEnum.EBX_XMM3, 0x24))
                .Movss(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(zMinAddress))
                .Comiss(new Register(RegisterEnum.EAX_XMM0), new Register(RegisterEnum.ECX_XMM1))
                .Jb(secondPlayerCode)
                .Movss(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(zMaxAddress))
                .Comiss(new Register(RegisterEnum.EAX_XMM0), new Register(RegisterEnum.ECX_XMM1))
                .Jnb(secondPlayerCode)
                .Mov(new RegisterMemory(RegisterEnum.EBX_XMM3, 0x1C), new FloatConst(posTeleport.TargetPos.X))
                .Mov(new RegisterMemory(RegisterEnum.EBX_XMM3, 0x20), new FloatConst(posTeleport.TargetPos.Y))
                .Mov(new RegisterMemory(RegisterEnum.EBX_XMM3, 0x24), new FloatConst(posTeleport.TargetPos.Z))
                .Label(secondPlayerCode)
                .Cmp(new Register(RegisterEnum.EBP_DS32_XMM5), new FourBytesConst(Constants.SECONDPLAYER_ID))
                .Jne(returnLabel)
                .Cmp(new Register(RegisterEnum.EBP_DS32_XMM5), new FourBytesConst(Constants.HOSTPLAYER_ID))
                .Movss(new Register(RegisterEnum.EAX_XMM0), new RegisterMemory(RegisterEnum.EBX_XMM3, 0x1C))
                .Movss(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(xMinAddress))
                .Comiss(new Register(RegisterEnum.EAX_XMM0), new Register(RegisterEnum.ECX_XMM1))
                .Jb(returnLabel)
                .Movss(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(xMaxAddress))
                .Comiss(new Register(RegisterEnum.EAX_XMM0), new Register(RegisterEnum.ECX_XMM1))
                .Jnb(returnLabel)
                .Movss(new Register(RegisterEnum.EAX_XMM0), new RegisterMemory(RegisterEnum.EBX_XMM3, 0x20))
                .Movss(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(yMinAddress))
                .Comiss(new Register(RegisterEnum.EAX_XMM0), new Register(RegisterEnum.ECX_XMM1))
                .Jb(returnLabel)
                .Movss(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(yMaxAddress))
                .Comiss(new Register(RegisterEnum.EAX_XMM0), new Register(RegisterEnum.ECX_XMM1))
                .Jnb(returnLabel)
                .Movss(new Register(RegisterEnum.EAX_XMM0), new RegisterMemory(RegisterEnum.EBX_XMM3, 0x24))
                .Movss(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(zMinAddress))
                .Comiss(new Register(RegisterEnum.EAX_XMM0), new Register(RegisterEnum.ECX_XMM1))
                .Jb(returnLabel)
                .Movss(new Register(RegisterEnum.ECX_XMM1), new FourBytesConst(zMaxAddress))
                .Comiss(new Register(RegisterEnum.EAX_XMM0), new Register(RegisterEnum.ECX_XMM1))
                .Jnb(returnLabel)
                .Mov(new RegisterMemory(RegisterEnum.EBX_XMM3, 0x1C), new FloatConst(posTeleport.TargetPos.X + 50f))
                .Mov(new RegisterMemory(RegisterEnum.EBX_XMM3, 0x20), new FloatConst(posTeleport.TargetPos.Y + 50f))
                .Mov(new RegisterMemory(RegisterEnum.EBX_XMM3, 0x24), new FloatConst(posTeleport.TargetPos.Z))
                .Label(returnLabel)
                .Ret();
            return shellcode;
        }

        private static Code GetSentryAndBothPlayersCode()
        {
            var shellcode = new Code();
            var returnLabel = new Label("return", shellcode);
            shellcode.Cmp(new RegisterMemory(RegisterEnum.ESI_XMM6, 0x150), new FourBytesConst(Constants.HEALTH_SENTRY_VALUE))
                .Je(returnLabel)
                .Cmp(new Register(RegisterEnum.ESI_XMM6), new FourBytesConst(Constants.HOSTPLAYER_ID))
                .Je(returnLabel)
                .Cmp(new Register(RegisterEnum.ESI_XMM6), new FourBytesConst(Constants.SECONDPLAYER_ID))
                .Je(returnLabel)
                .Sub(new Register(RegisterEnum.ECX_XMM1), new Register(RegisterEnum.EBP_DS32_XMM5))
                .Mov(new RegisterMemory(RegisterEnum.ESI_XMM6, 0x150), new Register(RegisterEnum.ECX_XMM1))
                .Label(returnLabel)
                .Ret();
            return shellcode;
        }

    }
}
