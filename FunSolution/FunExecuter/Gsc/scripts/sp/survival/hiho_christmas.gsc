// FunExecuter survival test script.
// Loaded automatically on IW5-Mod when Survival starts (scripts/sp/survival).

init()
{
    if ( isdefined( level.fun_hiho_christmas ) )
        return;

    level.fun_hiho_christmas = 1;
    thread fun_hiho_christmas();
}

main()
{
    init();
}

fun_hiho_christmas()
{
    level endon( "special_op_terminated" );

    for ( ;; )
    {
        level waittill( "wave_started", wave );

        if ( isdefined( wave ) && wave != 1 )
            continue;

        wait 10;
        iprintlnbold( "HIHO christmas!" );
        return;
    }
}
