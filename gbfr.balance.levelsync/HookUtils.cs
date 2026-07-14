using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gbfr.balance.levelsync;

public static class HookUtils
{
    public const string PushCallerRegistersX64 = "push rcx\npush rdx\npush r8\npush r9";
    public const string PopCallerRegistersX64 = "pop r9\npop r8\npop rdx\npop rcx";


    public static unsafe nint GetAbsoluteAddressFromRelative(nint relAddress) => *(int*)relAddress + relAddress + 4;
}
