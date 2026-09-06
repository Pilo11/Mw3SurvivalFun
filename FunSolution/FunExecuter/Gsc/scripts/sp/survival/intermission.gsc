// FunExecuter Survival GSC: 60-second intermission between waves.
// Original MW3 does not load this from IWD. FunExecuter injects it into the
// Survival wave ScriptFile in patch_survival.ff (same method as hiho_christmas.gsc).
// The skip-to-start-next-wave button still works.

fun_intermission_seconds()
{
	return 60;
}
