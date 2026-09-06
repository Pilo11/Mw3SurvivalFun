// FunExecuter survival test script.
// Injected into a Survival ScriptFile in patch_survival.ff.
// Do not define init()/main() here — host scripts such as 1571 already use those names.

fun_hiho_christmas()
{
    level endon( "special_op_terminated" );

    for ( ;; )
    {
        level waittill( "wave_started", wave );

        if ( isdefined( wave ) && wave != 1 )
            continue;

        iprintlnbold( "HIHO christmas!" );
        return;
    }
}
